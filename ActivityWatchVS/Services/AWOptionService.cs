using System.Threading.Tasks;

namespace ActivityWatchVS.Services
{
    internal class AWOptionService
    {
        #region Fields

        private AWPackage _package;
        private UI.AWOptionUIElementDialogPage _awOptions;
        private AWConfigurationService _awINI;

        #endregion Fields

        #region Constructors

        private AWOptionService(AWPackage package)
        {
            _package = package;
            _awOptions = (UI.AWOptionUIElementDialogPage)_package.GetDialogPage(typeof(UI.AWOptionUIElementDialogPage));
            _awINI = new Services.AWConfigurationService(_package);
        }

        #endregion Constructors

        #region Properties

        //public UI.AWOptionUIElementDialogPage Options { get => _awOptions; }
        public string ActivityWatchBaseURL
        {
            get
            {
                string url = _awOptions.ActivityWatchBaseURL;
                if (string.IsNullOrWhiteSpace(url))
                {
#if DEBUG
                    url = $@"http://{_awINI.AwConfig.HostTesting}:{_awINI.AwConfig.PortTesting}/api";
#else
                    url = $@"http://{_awINI.AwConfig.Host}:{_awINI.AwConfig.Port}/api";
#endif
                }
                return url;
            }
        }

        /// <summary>
        /// Is sending of events enabled
        /// </summary>
        public bool IsEnabled { get => _awOptions.IsEnabled; }

        #endregion Properties

        internal static async Task<AWOptionService> InitializeAsync(AWPackage package)
        {
            return new AWOptionService(package);
        }
    }
}