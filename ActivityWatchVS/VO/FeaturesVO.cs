using System;

namespace ActivityWatchVS.VO
{
    internal class FeaturesVO
    {
        #region Fields

        private bool _areServicesUp = false;
        private bool _isShutdownPending = false;

        public event EventHandler ShutdownEvent;

        #endregion Fields

        #region Properties

        public bool DoSendEvents
        {
            get
            {
                return _areServicesUp && IsSendingEventsEnabled;
            }
        }

        public bool IsShutdownPending
        {
            get
            {
                return _isShutdownPending;
            }
        }

        internal bool IsSendingEventsEnabled
        {
            get
            {
                return GeneralSettings.Default.EnableWatcher;
            }
            set
            {
                if (GeneralSettings.Default.EnableWatcher == value)
                {
                    return;
                }
                GeneralSettings.Default.EnableWatcher = value;
                GeneralSettings.Default.Save();
            }
        }

        internal void ServicesAreUp()
        {
            _areServicesUp = true;
        }

        internal void Shutdown()
        {
            _isShutdownPending = true;
            OnShutdown(new EventArgs());
        }

        private void OnShutdown(EventArgs e)
        {
            EventHandler handler = ShutdownEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        #endregion Properties
    }
}