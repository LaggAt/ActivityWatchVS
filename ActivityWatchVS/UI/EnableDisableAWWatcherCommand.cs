using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;

namespace ActivityWatchVS.UI
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class EnableDisableAWWatcherCommand
    {
        #region Fields

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("d36069ab-530f-46fb-bfa4-9a0de649fd4a");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AWPackage package;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EnableDisableAWWatcherCommand"/> class. Adds
        /// our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private EnableDisableAWWatcherCommand(AWPackage package_, OleMenuCommandService commandService)
        {
            this.package = package_ ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            menuItem.Checked = package.Features.IsSendingEventsEnabled;
            commandService.AddCommand(menuItem);
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static EnableDisableAWWatcherCommand Instance
        {
            get;
            private set;
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AWPackage package)
        {
            // Switch to the main thread - the call to AddCommand in EnableDisableAWWatcherCommand's
            // constructor requires the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new EnableDisableAWWatcherCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            //string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            //string title = "EnableDisableAWWatcherCommand";

            //// Show a message box to prove we were here
            //VsShellUtilities.ShowMessageBox(
            //    this.package,
            //    message,
            //    title,
            //    OLEMSGICON.OLEMSGICON_INFO,
            //    OLEMSGBUTTON.OLEMSGBUTTON_OK,
            //    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            package.Features.IsSendingEventsEnabled = !package.Features.IsSendingEventsEnabled;

            var command = sender as MenuCommand;
            command.Checked = package.Features.IsSendingEventsEnabled;
        }

        #endregion Methods
    }
}