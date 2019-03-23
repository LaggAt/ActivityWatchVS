using EnvDTE;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace ActivityWatchVS.Listeners
{
    internal sealed class DTE2EventListener : IDisposable
    {
        #region Fields

        private BuildEvents _buildEv;
        private DocumentEvents _documentEv;
        private AWPackage _package;
        private SelectionEvents _selectionEv;
        private SolutionEvents _solutionEv;
        private WindowEvents _windowEv;

        #endregion Fields

        #region Constructors

        public DTE2EventListener(AWPackage package)
        {
            this._package = package;
            var dte2Events = this._package.DTE2Service.Events;

            _buildEv = dte2Events.BuildEvents;
            _documentEv = dte2Events.DocumentEvents;
            _selectionEv = dte2Events.SelectionEvents;
            _solutionEv = dte2Events.SolutionEvents;
            _windowEv = dte2Events.WindowEvents;

            _buildEv.OnBuildBegin += buildEvents_OnBuildBegin;
            _buildEv.OnBuildDone += buildEvents_OnBuildDone;
            _documentEv.DocumentOpened += documentEv_DocumentOpened;
            _documentEv.DocumentSaved += documentEv_DocumentSaved;
            _documentEv.DocumentClosing += documentEv_DocumentClosing;
            _selectionEv.OnChange += selectionEv_OnChange;
            _solutionEv.Opened += solutionEvents_Opened;
            _solutionEv.BeforeClosing += solutionEv_BeforeClosing;
            _windowEv.WindowActivated += windowEv_WindowActivated;
            _windowEv.WindowClosing += windowEv_WindowClosing;
            _windowEv.WindowCreated += windowEv_WindowCreated;
            _windowEv.WindowMoved += windowEv_WindowMoved;
        }

        private DTE2EventListener()
        { }

        private void windowEv_WindowActivated(Window GotFocus, Window LostFocus)
        {
            produceEvent();
        }

        private void windowEv_WindowClosing(Window Window)
        {
            produceEvent();
        }

        private void windowEv_WindowCreated(Window Window)
        {
            produceEvent();
        }

        private void windowEv_WindowMoved(Window Window, int Top, int Left, int Width, int Height)
        {
            produceEvent();
        }

        #endregion Constructors

        #region Methods

        public void textEditorEvents_LineChanged(TextPoint StartPoint, TextPoint EndPoint, int Hint)
        {
            produceEvent();
        }

        private void buildEvents_OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
        {
            produceEvent();
        }

        private void buildEvents_OnBuildDone(vsBuildScope Scope, vsBuildAction Action)
        {
            produceEvent();
        }

        private void documentEv_DocumentClosing(Document Document)
        {
            produceEvent();
        }

        private void documentEv_DocumentOpened(Document Document)
        {
            produceEvent();
        }

        private void documentEv_DocumentSaved(Document Document)
        {
            produceEvent();
        }

        private void produceEvent([CallerMemberName] string caller = null)
        {
            if (!_package.IsReady || !_package.AwOptions.IsEnabled)
            {
                return;
            }

            try
            {
                string solution = _package.DTE2Service.Solution?.FullName;
                string activeDocument = _package.DTE2Service.ActiveDocument?.FullName;
                if (!string.IsNullOrWhiteSpace(solution) && !string.IsNullOrWhiteSpace(activeDocument))
                {
                    string solutionDir = Path.GetDirectoryName(solution);
                    if (activeDocument.StartsWith(solutionDir, StringComparison.Ordinal))
                    {
                        activeDocument = "." + activeDocument.Substring(solutionDir.Length);
                    }
                }
                string language = null;
                if (!string.IsNullOrWhiteSpace(activeDocument))
                {
                    // we keep that for later, for now I find it more useful to use the file extension as language
                    //language = _package.DTE2Service.ActiveDocument?.Language;
                    //if (string.IsNullOrWhiteSpace(language))
                    language = Path.GetExtension(activeDocument).TrimStart(".".ToCharArray());
                }

                var data = new ActivityWatch.API.V1.EventDataAppEditorActivity()
                {
                    Project = string.IsNullOrWhiteSpace(solution) ? "-" : solution,
                    File = string.IsNullOrWhiteSpace(activeDocument) ? "-" : activeDocument,
                    Language = string.IsNullOrWhiteSpace(language) ? "-" : language,
                };
                if (_package.LogService.LogLevel == Services.LogService.EErrorLevel.Debug)
                {
                    data.Caller = caller;
                }
                var awEvent = new ActivityWatch.API.V1.Event()
                {
                    Timestamp = DateTime.UtcNow,
                    Duration = 0,
                    Data = data
                };

                _package.EventService.AddEvent(awEvent);
            }
            catch (Exception ex)
            {
                _package.LogService.Log(ex);
            }
        }

        private void selectionEv_OnChange()
        {
            produceEvent();
        }

        private void solutionEv_BeforeClosing()
        {
            produceEvent();
        }

        private void solutionEvents_Opened()
        {
            produceEvent();
        }

        #region IDisposable

        public void Dispose()
        {
            _buildEv.OnBuildBegin -= buildEvents_OnBuildBegin;
            _buildEv.OnBuildDone -= buildEvents_OnBuildDone;
            _documentEv.DocumentOpened -= documentEv_DocumentOpened;
            _documentEv.DocumentSaved -= documentEv_DocumentSaved;
            _documentEv.DocumentClosing -= documentEv_DocumentClosing;
            _selectionEv.OnChange -= selectionEv_OnChange;
            _solutionEv.Opened -= solutionEvents_Opened;
            _solutionEv.BeforeClosing -= solutionEv_BeforeClosing;
            _windowEv.WindowActivated -= windowEv_WindowActivated;
            _windowEv.WindowClosing -= windowEv_WindowClosing;
            _windowEv.WindowCreated -= windowEv_WindowCreated;
            _windowEv.WindowMoved -= windowEv_WindowMoved;
        }

        #endregion IDisposable

        #endregion Methods
    }
}