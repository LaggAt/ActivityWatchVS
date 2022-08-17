using System.Threading.Tasks;

namespace ActivityWatchVS.Services
{
    internal class AWOptionService
    {
        #region Fields

        private AWConfigurationService _awINI;
        private UI.AWOptionUIElementDialogPage _awOptions;
        private AWPackage _package;
        #endregion Fields

        #region Constructors

        private AWOptionService(AWPackage package)
        {
            _package = package;
            _awOptions = (UI.AWOptionUIElementDialogPage)_package.GetDialogPage(typeof(UI.AWOptionUIElementDialogPage));
            _awINI = new Services.AWConfigurationService(_package);
            setAWUrl();
        }


        #endregion Constructors

        #region Properties

        //public UI.AWOptionUIElementDialogPage Options { get => _awOptions; }
        public string ActivityWatchBaseURL
        {
            get
            {
                return _awOptions.ActivityWatchBaseURL;
            }
        }

        public string DefaultURL
        {
            get
            {
                if (IsProductive)
                {
                    return $@"http://{_awINI.AwConfig.Host}:{_awINI.AwConfig.Port}/api";
                }
                return $@"http://{_awINI.AwConfig.HostTesting}:{_awINI.AwConfig.PortTesting}/api";
            }
        }

        /// <summary>
        /// Is sending of events enabled
        /// </summary>
        public bool IsEnabled { get => _awOptions.IsEnabled; }

        /// <summary>
        /// Productive or Testing Server?
        /// </summary>
        public bool IsProductive
        {
            get
            {
#if DEBUG
                return false;
#else
                return true;
#endif
            }
        }
        #endregion Properties

        #region Methods

        internal static AWOptionService Initialize(AWPackage package)
        {
            return new AWOptionService(package);
        }

        private void setAWUrl()
        {
            if (string.IsNullOrWhiteSpace(_awOptions.ActivityWatchBaseURL))
            {
                _awOptions.ActivityWatchBaseURL = DefaultURL;
            }
        }

        #endregion Methods
    }
}