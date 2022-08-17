using ActivityWatch.API.V1;
using ActivityWatchVS.Tools;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ActivityWatchVS.Services
{
    internal class EventService
    {
        #region Constants

        private const double AFK_SECONDS = 60 * 15;
        private const double MIN_HEARTBEAT_SECONDS = 60;
        private const int WAIT_FOR_EVENT_SECONDS = 20;

        #endregion Constants

        #region Fields

        private Client _client;
        private bool _isBucketSent = false;
        private Task _currentTask = Task.CompletedTask;

        private readonly object _currentTaskLock = new object();
        private readonly AWPackage _package;
        private readonly ConcurrentQueue<Event> _events = new ConcurrentQueue<Event>();
        private readonly Dictionary<string, Event> _bucketIdPartLastEvent = new Dictionary<string, Event>();
        private readonly Semaphore _semaphore = new Semaphore(1, 1, typeof(EventService).FullName);

        #endregion Fields

        #region Constructors

        internal EventService(AWPackage package)
        {
            _package = package;
        }

        #endregion Constructors

        #region Methods

        public void Reset()
        {
            _isBucketSent = false;
        }

        internal void AddEvent(Event ev)
        {
            if (_package.DisposalToken.IsCancellationRequested)
                return;

            _events.Enqueue(ev);

            lock(_currentTaskLock)
            {
                if (_currentTask.IsCompleted)
                {
                    _currentTask = this.ScheduleBackgroundTaskAsync(() => this.ProcessEventQueueAsync(_package.DisposalToken));
                }
            }
        }

        internal void Shutdown()
        {
            _package.JoinableTaskFactory.Run(ProcessEventsAsync);
        }

        private static string GetBucketId(Event ev)
        {
            return $"{ev.Data.BucketIDCustomPart}_{Environment.MachineName}";
        }

        private bool MergeEventsAndOutFinishedEvent(out Event logEvent)
        {
            logEvent = null;
            if (_events.TryDequeue(out Event newEvent))
            {
                _package.LogService.Log($"new Event for {GetBucketId(newEvent)}: " + newEvent.ToJson(), LogService.EErrorLevel.Debug);

                Event oldEvent = null;

                // do we have an old event?
                if (_bucketIdPartLastEvent.ContainsKey(newEvent.Data.BucketIDCustomPart))
                {
                    // compare old to new event
                    oldEvent = _bucketIdPartLastEvent[newEvent.Data.BucketIDCustomPart];
                    var duration = (newEvent.Timestamp - oldEvent.Timestamp).TotalSeconds;
                    oldEvent.Duration = duration;

                    if (oldEvent.Equals(newEvent) && duration < MIN_HEARTBEAT_SECONDS)
                    {
                        // same event - keep old event, throw away new event
                    }
                    else if (!oldEvent.Equals(newEvent))
                    {
                        // different event, log and keep the new one
                        _bucketIdPartLastEvent.Remove(oldEvent.Data.BucketIDCustomPart);
                        _bucketIdPartLastEvent[newEvent.Data.BucketIDCustomPart] = newEvent;
                        logEvent = oldEvent;
                        return true;
                    }
                    else
                    {
                        // same event, but older than minimum hearbeat interval different event, push
                        // old event with new duration, and save old
                        _bucketIdPartLastEvent[newEvent.Data.BucketIDCustomPart] = newEvent;
                    }
                }
                else
                {
                    // no old event, keep the record
                    _bucketIdPartLastEvent[newEvent.Data.BucketIDCustomPart] = newEvent;
                }
            }
            else
            {
                if (_package.DisposalToken.IsCancellationRequested)
                {
                    // we are shutting down, create stop events
                    var stopEvent = _bucketIdPartLastEvent.Values.FirstOrDefault();
                    if (stopEvent != null)
                    {
                        // write event, we are about to stop
                        _bucketIdPartLastEvent.Remove(stopEvent.Data.BucketIDCustomPart);
                        stopEvent.Duration = (DateTimeOffset.UtcNow - stopEvent.Timestamp).TotalSeconds;
                        logEvent = stopEvent;
                        return true;
                    }
                }
                else
                {
                    // nothing to dequeue, create stop events (AFK)
                    var stopEvent = _bucketIdPartLastEvent.Values.FirstOrDefault(e => e.Timestamp.AddSeconds((int)e.Duration) < DateTimeOffset.UtcNow.AddSeconds(-AFK_SECONDS));
                    if (stopEvent != null)
                    {
                        // write event, user is afk
                        _bucketIdPartLastEvent.Remove(stopEvent.Data.BucketIDCustomPart);
                        stopEvent.Duration = AFK_SECONDS;
                        logEvent = stopEvent;
                        return true;
                    }
                }
            }
            return false;
        }

        private async Task<bool> PostCreateBucketAsync(string bucket_id, string bucketType, CancellationToken cancellationToken)
        {
            if (!_isBucketSent || _package.AwOptions.ActivityWatchBaseURL != _client?.BaseUrl)
            {
                _client = new Client();
                _client.BaseUrl = _package.AwOptions.ActivityWatchBaseURL;
 
                var awBucket = new CreateBucket()
                {
                    Client = AWPackage.NAME_ACTIVITY_WATCHER,
                    Hostname = Environment.MachineName,
                    Type = bucketType
                };

                try
                {
                    var bucketResult = await _client.BucketsPostAsync(awBucket, bucket_id, cancellationToken).ConfigureAwait(false);
                }
                catch (AWApiException ex)
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

                return true;
            }

            return false;
        }

        private async Task PushEventAsync(Event logEvent, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await TaskScheduler.Default;

            try
            {
                var bucket_id = GetBucketId(logEvent);

                bool ok = false;
                do
                {
                    try
                    {
                        if (!_isBucketSent)
                        {
                            _isBucketSent = await PostCreateBucketAsync(bucket_id, logEvent.Data.TypeName, cancellationToken);
                        }

                        await _client.BucketsIdEventsPostAsync(logEvent, bucket_id, cancellationToken);

                        _package.LogService.Log($"Sent event for {bucket_id}: " + logEvent.ToJson(), LogService.EErrorLevel.Debug);
                        ok = true;
                    }
                    catch (Exception ex)
                    {
                        var sockEx = ex.GetInnerst<SocketException>();
                        if (sockEx != null)
                        {
                            if (sockEx.SocketErrorCode == SocketError.ConnectionRefused)
                            {
                                //aw_service is not running, try to find and start it
                                _package.AwBinaryService.TryStartAwServer();
                            }
                        }

                        // some error, retry regularly
                        _package.LogService.Log(ex, logEvent.ToJson());
                        Reset();
                        // don't ddos
                        await Task.Delay(5000);
                    }
                } while (!ok);
            }
            catch (Exception ex)
            {
                _package.LogService.Log(ex);
            }
        }

        private async Task ScheduleBackgroundTaskAsync(Func<Task> action)
        {
            try
            {
                await _package.JoinableTaskFactory.StartOnIdle(action);
            }
            catch (Exception ex)
            {
                _package.LogService.Log(ex);
            }
        }

        private Task ProcessEventsAsync() => ProcessEventQueueAsync(CancellationToken.None);

        private async Task ProcessEventQueueAsync(CancellationToken token)
        {
            await TaskScheduler.Default;

            if (!_semaphore.WaitOne(WAIT_FOR_EVENT_SECONDS * 1000))
            {
                return;
            }

            try
            {
                while (MergeEventsAndOutFinishedEvent(out Event logEvent))
                {
                    await PushEventAsync(logEvent, token);
                }
            }
            finally
            {
                _semaphore.Release();
            }

            _package.LogService.Log("EventService loop ended", LogService.EErrorLevel.Debug);
        }

        #endregion Methods
    }
}