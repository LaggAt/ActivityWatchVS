using ActivityWatch.API.V1;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ActivityWatchVS.Services
{
    internal class EventService
    {
        #region Constants

        private const double AFK_SECONDS = 60 * 15;
        private const double MIN_HEARTBEAT_SECONDS = 60;
        private const int THREAD_WAIT_TIMEOUT_SECONDS = 60;
        private const int WAIT_FOR_EVENT_SECONDS = 20;

        #endregion Constants

        #region Fields

        private CancellationTokenSource _cancellationTokenSource;
        private Client _client;
        private ManualResetEvent _continueLoopManualResetEvent = new ManualResetEvent(false);
        private bool _doShutdown = false;
        private ConcurrentQueue<ActivityWatch.API.V1.Event> _events = new ConcurrentQueue<ActivityWatch.API.V1.Event>();
        private bool _isBucketSent = false;
        private AWPackage _package;
        private Task _thread = null;
        private Dictionary<string, ActivityWatch.API.V1.Event> bucketIdPartLastEvent = new Dictionary<string, Event>();

        #endregion Fields

        #region Constructors

        internal EventService(AWPackage package)
        {
            _package = package;
        }

        private EventService()
        {
        }

        #endregion Constructors

        #region Methods

        public void Reset()
        {
            _isBucketSent = false;
        }

        internal void AddEvent(ActivityWatch.API.V1.Event ev)
        {
            if (_doShutdown) //(_cancellationTokenSource?.IsCancellationRequested ?? false)
            {
                return;
            }
            _events.Enqueue(ev);
            _continueLoopManualResetEvent.Set();
            startThread();
        }

        internal void Shutdown()
        {
            //_cancellationTokenSource?.Cancel();
            _doShutdown = true;
            _continueLoopManualResetEvent.Set();

            //if (_thread != null)
            //{
            //    Task.WaitAll(new Task[] { _thread }, THREAD_WAIT_TIMEOUT_SECONDS * 1000);
            //}
            _thread?.Wait(THREAD_WAIT_TIMEOUT_SECONDS * 1000);
        }

        private static string getBucketId(Event ev)
        {
            return $"{ev.Data.BucketIDCustomPart}_{Environment.MachineName}";
        }

        private bool mergeEventsAndOutFinishedEvent(out ActivityWatch.API.V1.Event logEvent)
        {
            logEvent = null;
            if (_events.TryDequeue(out ActivityWatch.API.V1.Event newEvent))
            {
                _package.LogService.Log($"new Event for {getBucketId(newEvent)}: " + newEvent.ToJson(), LogService.EErrorLevel.Debug);

                ActivityWatch.API.V1.Event oldEvent = null;
                // do we have an old event?
                if (bucketIdPartLastEvent.ContainsKey(newEvent.Data.BucketIDCustomPart))
                {
                    // compare old to new event
                    oldEvent = bucketIdPartLastEvent[newEvent.Data.BucketIDCustomPart];
                    var duration = (newEvent.Timestamp - oldEvent.Timestamp).TotalSeconds;
                    oldEvent.Duration = duration;

                    if (oldEvent.Equals(newEvent) && duration < MIN_HEARTBEAT_SECONDS)
                    {
                        // same event - keep old event, throw away new event
                    }
                    else if (!oldEvent.Equals(newEvent))
                    {
                        // different event, log and keep the new one
                        bucketIdPartLastEvent.Remove(oldEvent.Data.BucketIDCustomPart);
                        bucketIdPartLastEvent[newEvent.Data.BucketIDCustomPart] = newEvent;
                        logEvent = oldEvent;
                        return true;
                    }
                    else
                    {
                        // same event, but older than minimum hearbeat interval different event, push
                        // old event with new duration, and save old
                        bucketIdPartLastEvent[newEvent.Data.BucketIDCustomPart] = newEvent;
                    }
                }
                else
                {
                    // no old event, keep the record
                    bucketIdPartLastEvent[newEvent.Data.BucketIDCustomPart] = newEvent;
                }
            }
            else
            {
                if (_doShutdown) //(_cancellationTokenSource.IsCancellationRequested)
                {
                    // we are shutting down, create stop events
                    var stopEvent = bucketIdPartLastEvent.Values.FirstOrDefault();
                    if (stopEvent != null)
                    {
                        // write event, we are about to stop
                        bucketIdPartLastEvent.Remove(stopEvent.Data.BucketIDCustomPart);
                        stopEvent.Duration = (DateTimeOffset.UtcNow - stopEvent.Timestamp).TotalSeconds;
                        logEvent = stopEvent;
                        return true;
                    }
                }
                else
                {
                    // nothing to dequeue, create stop events (AFK)
                    var stopEvent = bucketIdPartLastEvent.Values.FirstOrDefault(e => e.Timestamp.AddSeconds((int)e.Duration) < DateTimeOffset.UtcNow.AddSeconds(-AFK_SECONDS));
                    if (stopEvent != null)
                    {
                        // write event, user is afk
                        bucketIdPartLastEvent.Remove(stopEvent.Data.BucketIDCustomPart);
                        stopEvent.Duration = AFK_SECONDS;
                        logEvent = stopEvent;
                        return true;
                    }
                }
            }
            return false;
        }

        private async Task postBucketIfNeededAsync(string bucket_id, string bucketType, CancellationToken cancellationToken)
        {
            if (!_isBucketSent || _package.AwOptions.ActivityWatchBaseURL != _client?.BaseUrl)
            {
                _client = new ActivityWatch.API.V1.Client();
                _client.BaseUrl = _package.AwOptions.ActivityWatchBaseURL;
                var awBucket = new ActivityWatch.API.V1.CreateBucket()
                {
                    Client = AWPackage.NAME_ACTIVITY_WATCHER,
                    Hostname = Environment.MachineName,
                    Type = bucketType
                };
                try
                {
                    var bucketResult = await _client.BucketsPostAsync(awBucket, bucket_id, cancellationToken).ConfigureAwait(false);
                }
                catch (ActivityWatch.API.V1.AWApiException ex)
                {
                    if (ex.StatusCode == 304)
                    {
                        // bucket already exists, go on.
                    }
                    else
                    {
                        throw;
                    }
                }
                _isBucketSent = true;
            }
        }

        private void pushEventsThread(CancellationToken cancellationToken)
        {
            for (; ; )
            {
                try
                {
                    bool hasFired = _continueLoopManualResetEvent.WaitOne(WAIT_FOR_EVENT_SECONDS * 1000);
                    while (mergeEventsAndOutFinishedEvent(out ActivityWatch.API.V1.Event logEvent))
                    {
                        var bucket_id = getBucketId(logEvent);

                        bool ok = false;
                        do
                        {
                            try
                            {
                                //await
                                postBucketIfNeededAsync(bucket_id, logEvent.Data.TypeName, _cancellationTokenSource.Token)
                                    .GetAwaiter().GetResult();
                                //await
                                _client.BucketsIdEventsPostAsync(logEvent, bucket_id, _cancellationTokenSource.Token)
                                    .GetAwaiter().GetResult();
                                _package.LogService.Log($"Sent event for {bucket_id}: " + logEvent.ToJson(), LogService.EErrorLevel.Debug);
                                ok = true;
                            }
                            catch (Exception ex)
                            {
                                // some error, retry regularly
                                _package.LogService.Log(ex, logEvent.ToJson());
                                if (!_doShutdown) //(!cancellationToken.IsCancellationRequested)
                                {
                                    Reset();
                                    // don't ddos
                                    Thread.Sleep(5000);
                                }
                            }
                        } while (!ok && !_doShutdown); //cancellationToken.IsCancellationRequested); // don't retry if we do shut down
                    }
                    _continueLoopManualResetEvent.Reset();
                    if (_doShutdown && _events.IsEmpty) //_events.IsEmpty needed?
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _package.LogService.Log(ex);
                }
            }
            _package.LogService.Log("EventService loop ended", LogService.EErrorLevel.Debug);
        }

        private void startThread()
        {
            if (_thread == null)
            {
                lock (this)
                {
                    if (_thread == null)
                    {
                        _cancellationTokenSource = new CancellationTokenSource();
                        _thread = Task.Run(() => pushEventsThread(_cancellationTokenSource.Token));
                    }
                }
            }
        }

        #endregion Methods
    }
}