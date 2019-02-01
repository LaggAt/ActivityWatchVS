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

        private const double MIN_HEARTBEAT_SECONDS = 60;
        private const double WAIT_FOR_EVENT_SECONDS = 20;
        private const double AFK_SECONDS = 60 * 15;

        #endregion Constants

        #region Fields

        private ConcurrentQueue<ActivityWatch.API.V1.Event> _events = new ConcurrentQueue<ActivityWatch.API.V1.Event>();
        private ManualResetEvent _newEventAvailable = new ManualResetEvent(false);
        private Task _thread = null;
        private AWPackage package;
        private Client _client;
        private bool _isBucketSent = false;
        private Dictionary<string, ActivityWatch.API.V1.Event> bucketIdPartLastEvent = new Dictionary<string, Event>();

        #endregion Fields

        #region Constructors

        internal EventService(AWPackage package_)
        {
            package = package_;
        }

        private EventService()
        {
        }

        #endregion Constructors

        #region Methods

        internal void AddEvent(ActivityWatch.API.V1.Event ev)
        {
            _events.Enqueue(ev);
            _newEventAvailable.Set();
            startThread();
        }

        private bool mergeEventsAndOutFinishedEvent(out ActivityWatch.API.V1.Event logEvent)
        {
            logEvent = null;
            if (_events.TryDequeue(out ActivityWatch.API.V1.Event newEvent))
            {
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
                // nothing to dequeue, create stop events
                foreach (var stopEvent in bucketIdPartLastEvent.Values.Where(e => e.Timestamp.AddSeconds((int)e.Duration) < DateTimeOffset.UtcNow.AddSeconds(-AFK_SECONDS)))
                {
                    // write event, user is afk
                    stopEvent.Duration = AFK_SECONDS;
                    bucketIdPartLastEvent.Remove(stopEvent.Data.BucketIDCustomPart);
                    logEvent = stopEvent;
                    return true;
                }
            }
            return false;
        }

        private async Task postBucketIfNeededAsync(string bucket_id, string bucketType)
        {
            if (!_isBucketSent)
            {
                _client = new ActivityWatch.API.V1.Client();
                _client.BaseUrl = @"http://localhost:5600/api";
//#if DEBUG
//                _client.BaseUrl = @"http://ipv4.fiddler:5666/api";
//#endif
                var awBucket = new ActivityWatch.API.V1.CreateBucket()
                {
                    Client = AWPackage.CLIENT_NAME,
                    //Id = AWPackage.PackageGuidString,
                    Hostname = Environment.MachineName,
                    //Created = DateTime.UtcNow,
                    Type = bucketType
                };
                try
                {
                    var bucketResult = await _client.BucketsIdPostAsync(awBucket, bucket_id);
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

        private async Task pushEventsThreadAsync()
        {
            for (; ; )
            {
                ActivityWatch.API.V1.Event ev = null;
                try
                {
                    if (package.Features.IsShutdownPending)
                    {
                        // I never get here
                        return;
                    }

                    bool hasFired = _newEventAvailable.WaitOne((int)(WAIT_FOR_EVENT_SECONDS * 1000));
                    while (mergeEventsAndOutFinishedEvent(out ActivityWatch.API.V1.Event logEvent))
                    {
                        var bucket_id = $"{logEvent.Data.BucketIDCustomPart}_{Environment.MachineName}";

                        bool ok = false;
                        do
                        {
                            try
                            {
                                await postBucketIfNeededAsync(bucket_id, logEvent.Data.TypeName);
                                await _client.BucketsIdHeartbeatAsync(logEvent, "2.0", bucket_id);
                                ok = true;
                            }
                            catch (Exception ex)
                            {
                                // some error, retry regularly
                                ; //TODO: LOG
                                Thread.Sleep(5000);
                            }
                        } while (!ok);
                    }
                    _newEventAvailable.Reset();
                }
                catch (Exception ex)
                {
                    ; // TODO: LOG
                }
            }
        }

        private void startThread()
        {
            if (_thread == null)
            {
                lock (this)
                {
                    if (_thread == null)
                    {
                        _thread = new Task(() => pushEventsThreadAsync());
                        _thread.Start();
                    }
                }
            }
        }

        #endregion Methods
    }
}