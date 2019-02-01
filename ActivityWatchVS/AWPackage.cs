using ActivityWatchVS.Services;
using ActivityWatchVS.VO;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace ActivityWatchVS
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio is to
    /// implement the IVsPackage interface and register itself with the shell. This package uses the
    /// helper classes defined inside the Managed Package Framework (MPF) to do it: it derives from
    /// the Package class that provides the implementation of the IVsPackage interface and uses the
    /// registration attributes defined in the framework to register itself and its components with
    /// the shell. These attributes tell the pkgdef creation utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset
    /// Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(AWPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class AWPackage : AsyncPackage
    {
        #region Fields

        public const string CLIENT_NAME = "aw-watcher-vs";

        /// <summary>
        /// AWPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "ccccb132-8911-489f-ae88-b0de87d77f49";

        private DTE2 _dte2Service = null;
        private EventService _eventService;
        private FeaturesVO _featuresVO = new VO.FeaturesVO();
        private Listeners.TextEditorEventListener _textEditorEventListener = null;

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

        internal DTE2 DTE2Service
        {
            get
            {
                return _dte2Service;
            }
        }

        internal EventService EventService
        {
            get
            {
                return _eventService;
            }
        }

        internal FeaturesVO Features
        {
            get
            {
                return _featuresVO;
            }
        }

        #endregion Properties

        #region Package Members

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            cancellationToken.Register(() => Features.Shutdown());
            progress.Report(new ServiceProgressData("ActivityWatchVS starting"));

            // ... Services VS
            _dte2Service = await GetServiceAsync(typeof(DTE)) as DTE2;

            // ... our Services
            _eventService = new Services.EventService(this);

            // we are ready to send events
            Features.ServicesAreUp();
            // ... Listeners
            _textEditorEventListener = new Listeners.TextEditorEventListener(this);

            // init on UI thread:
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            // ... UI
            await UI.EnableDisableAWWatcherCommand.InitializeAsync(this);

            progress.Report(new ServiceProgressData("ActivityWatchVS running"));
        }

        #endregion Package Members
    }
}