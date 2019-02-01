using EnvDTE;
using System;
using System.IO;

namespace ActivityWatchVS.Listeners
{
    internal sealed class TextEditorEventListener
    {
        #region Fields

        private static TextEditorEventListener _instance = null;
        private static volatile object _lock = new object();
        private TextEditorEvents _textEditorEvents;
        private AWPackage package;

        #endregion Fields

        #region Constructors

        public TextEditorEventListener(AWPackage package_)
        {
            this.package = package_;
            _textEditorEvents = this.package.DTE2Service.Events.TextEditorEvents;
            _textEditorEvents.LineChanged += _textEditorEvents_LineChanged;
        }

        private TextEditorEventListener()
        { }

        //public object TextEditorEvents { get; private set; }

        #endregion Constructors

        #region Methods

        public void _textEditorEvents_LineChanged(TextPoint StartPoint, TextPoint EndPoint, int Hint)
        {
            if (!package.Features.DoSendEvents)
            {
                return;
            }

            string activeDocument = package.DTE2Service.ActiveDocument.FullName;
            string solution = package.DTE2Service.Solution.FullName;
            string language = Path.GetExtension(activeDocument).TrimStart(".".ToCharArray());

            var awEvent = new ActivityWatch.API.V1.Event()
            {
                Timestamp = DateTime.UtcNow,
                Duration = 0,
                Data = new ActivityWatch.API.V1.EventDataAppEditorActivity()
                {
                    File = activeDocument,
                    Project = solution,
                    Language = language
                }
            };

            package.EventService.AddEvent(awEvent);
        }

        #endregion Methods
    }
}