using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.TeamFoundation;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ActivityWatchVS.Listeners
{
    public class VersionControlListener
    {
        #region Fields

        private CancellationTokenSource _cancellationTokenSource;
        private Dictionary<WorkspaceInfo, VersionControlServer> _knownWorkspaceInfosAndVersionControlServers = new Dictionary<WorkspaceInfo, VersionControlServer>();
        private AWPackage _package;
        private Task _thread;
        private VersionControlServer _versionControlServer;

        #endregion Fields

        #region Constructors

        public VersionControlListener(AWPackage package)
        {
            _package = package;

            _cancellationTokenSource = new CancellationTokenSource();
            _thread = Task.Run(() => watchWorkspacesThread(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// Raised after updating work items with a check-in.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void versionControlServer_AfterWorkItemsUpdated(object sender, WorkItemsUpdateEventArgs e)
        {
            ;
        }

        /// <summary>
        /// Raised on the commit of a new check-in.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void versionControlServer_CommitCheckin(object sender, CommitCheckinEventArgs e)
        {
            ;
        }

        /// <summary>
        /// Raised on the creation of a new Shelveset.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void versionControlServer_CommitShelveset(object sender, CommitShelvesetEventArgs e)
        {
            ;
        }

        /// <summary>
        /// This event is fired when a committed branch is created.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void versionControlServer_CommittedBranchCreated(object sender, CommittedBranchCreatedEventArgs e)
        {
            ;
        }

        /// <summary>
        /// Raised on the creation of a new PendingChange.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void versionControlServer_NewPendingChange(object sender, PendingChangeEventArgs e)
        {
            ;
        }

        /// <summary>
        /// Raised when a workspace's set of pending changes is modified.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void versionControlServer_PendingChangesChanged(object sender, WorkspaceEventArgs e)
        {
            ;
        }

        private void watchWorkspacesThread(CancellationToken token)
        {
            //for (; ; )
            //{
            //    // look for new WorkspaceInfo's every 30s
            //    try
            //    {
            //        Workstation workstation = Workstation.Current;
            //        foreach (WorkspaceInfo workspaceInfo in workstation.GetAllLocalWorkspaceInfo())
            //        {
            //            if (_knownWorkspaceInfosAndVersionControlServers.ContainsKey(workspaceInfo))
            //            {
            //                continue;
            //            }

            try
            {
                //TfsTeamProjectCollection tfsTeamProjectCollection = new TfsTeamProjectCollection(workspaceInfo.ServerUri);
                //if (tfsTeamProjectCollection == null)
                //{
                //    _package.LogService.Log($"TfsTeamProjectCollection for '{workspaceInfo.ServerUri}' is null.", Services.LogService.EErrorLevel.Warning);
                //    continue;
                //}
                //VersionControlServer versionControlServer = tfsTeamProjectCollection.GetService<VersionControlServer>();
                //if (versionControlServer == null)
                //{
                //    _package.LogService.Log($"versionControlServer for '{tfsTeamProjectCollection.DisplayName} is null.", Services.LogService.EErrorLevel.Warning);
                //    continue;
                //}

                Thread.Sleep(20000);

                var _applicationObject = _package.DTE2Service;
                //+     _applicationObject.GetObject("Microsoft.VisualStudio.TeamFoundation.TeamFoundationServerExt")   {Microsoft.VisualStudio.TeamFoundation.TeamFoundationServerExt} dynamic {Microsoft.VisualStudio.TeamFoundation.TeamFoundationServerExt}
                var tfsExt = (TeamFoundationServerExt)_applicationObject.GetObject("Microsoft.VisualStudio.TeamFoundation.TeamFoundationServerExt");
                var tfsUrl = tfsExt.ActiveProjectContext.DomainUri;

                //if ((tfsExt == null) || (tfsExt.ActiveProjectContext == null) || (tfsExt.ActiveProjectContext.DomainUri == null) || (tfsExt.ActiveProjectContext.ProjectUri == null)) { MessageBox.Show("Please Connect to TFS first and select a Team Project"); }
                //else { MessageBox.Show("Connected to:" + tfsExt.ActiveProjectContext.ProjectName); }

                //var vsExt = _applicationObject.GetObject("Microsoft.VisualStudio.TeamFoundation.VersionControl.VersionControlExt") as VersionControlExt;

                //var versionControlServer = vsExt.Explorer.Workspace.VersionControlServer;

                // Get a reference to our Team Foundation Server.
                var tpc = new TfsTeamProjectCollection(new Uri(tfsUrl));

                // Get a reference to Version Control.
                _versionControlServer = tpc.GetService<VersionControlServer>();

                //vcs = vsExt.Explorer.Workspace.VersionControlServer;
                //var versionControlServer = vsExt.SolutionWorkspace.VersionControlServer;

                // events
                _versionControlServer.PendingChangesChanged += versionControlServer_PendingChangesChanged;
                _versionControlServer.CommitCheckin += versionControlServer_CommitCheckin;
                _versionControlServer.CommitShelveset += versionControlServer_CommitShelveset;
                _versionControlServer.CommittedBranchCreated += versionControlServer_CommittedBranchCreated;
                _versionControlServer.NewPendingChange += versionControlServer_NewPendingChange;
                _versionControlServer.AfterWorkItemsUpdated += versionControlServer_AfterWorkItemsUpdated;

                //var gitRepoService = tpc.GetService<GitRepositoryService>();

                //// if successful add to known list
                //_knownWorkspaceInfosAndVersionControlServers[workspaceInfo] = versionControlServer;
            }
            catch (Exception ex)
            {
                _package.LogService.Log(ex);
            }
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        _package.LogService.Log(ex, "Exception in watchWorkspacesThread. This is a bug, please report it.", activate: true);
            //    }
            //    Thread.Sleep(30000);
            //}
        }

        #endregion Constructors
    }
}