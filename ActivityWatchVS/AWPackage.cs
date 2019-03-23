using ActivityWatchVS.Listeners;
using ActivityWatchVS.Services;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace ActivityWatchVS
{
    [PackageRegistration(UseManagedResourcesOnly = false, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0")] // Info on this package for Help/About
    [Guid(AWPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideOptionPage(typeof(UI.AWOptionUIElementDialogPage), "Activity Watch VS", "General", 0, 0, true)]
    public sealed class AWPackage : AsyncPackage
    {
        #region Constants

        public const string NAME_ACTIVITY_WATCHER = "aw-watcher-vs";
        public const string NAME_CS_PLUGIN = "ActivityWatchVS";

        /// <summary>
        /// AWPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "ccccb132-8911-489f-ae88-b0de87d77f49";

        #endregion Constants

        #region Fields

        private AwBinaryService _awBinaryService;
        private AWOptionService _awOptions;
        private Listeners.DTE2EventListener _dte2EventListener = null;
        private DTE2 _dte2Service = null;
        private EventService _eventService;
        private bool _isReady;
        private LogService _logService;
        private IProgress<ServiceProgressData> _progress;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AWPackage"/> class.
        /// </summary>
        public AWPackage()
        {
        }

        #endregion Constructors

        #region Properties

        public bool IsReady { get => _isReady; }
        internal AwBinaryService AwBinaryService => _awBinaryService;
        internal AWOptionService AwOptions { get => _awOptions; }
        internal DTE2 DTE2Service => _dte2Service;
        internal EventService EventService => _eventService;

        internal LogService LogService => _logService;

        #endregion Properties

        #region Package Members

        public void Dispose()
        {
            shutdown();
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            _progress = progress;
            _progress.Report(new ServiceProgressData("ActivityWatchVS starting"));
            this.DisposalToken.Register(() => shutdown());

            // background thread
            await BackgroundThreadInitialization();
            // main thread
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await MainThreadInitialization();

            _isReady = true;
            _progress.Report(new ServiceProgressData("ActivityWatchVS running"));
        }

        private async Task BackgroundThreadInitialization()
        {
            // Logger
            _logService = new Services.LogService(this, await GetServiceAsync(typeof(SVsGeneralOutputWindowPane)) as IVsOutputWindowPane);

            try
            {
                // ... Services VS
                _dte2Service = await GetServiceAsync(typeof(DTE)) as DTE2;

                // ... our Services
                _awBinaryService = new Services.AwBinaryService(this);
                _eventService = new Services.EventService(this);

                // we are ready to send events

                // ... Listeners
                _dte2EventListener = new Listeners.DTE2EventListener(this);
            }
            catch (Exception ex)
            {
                _logService.Log(ex, "ActivityWatchVS: This is a bug, please report it!", activate: true);
            }
        }

        private async Task MainThreadInitialization()
        {
            try { 
                // single class for AW ini file and our own settings
                _awOptions = await Services.AWOptionService.InitializeAsync(this);
            }
            catch (Exception ex)
            {
                _logService.Log(ex, "ActivityWatchVS: This is a bug, please report it!", activate: true);
            }
        }

        private void shutdown()
        {
            EventService?.Shutdown();

            // tell the world we 're done
            LogService?.Log(NAME_CS_PLUGIN + " finished.", LogService.EErrorLevel.Info);
            _progress.Report(new ServiceProgressData("ActivityWatchVS ended"));
        }

        #endregion Package Members
    }
}