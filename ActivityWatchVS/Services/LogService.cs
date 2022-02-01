using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace ActivityWatchVS.Services
{
    internal class LogService
    {
        #region Fields

        private readonly AWPackage _package;
        private readonly IVsOutputWindowPane _vsOutputWindowPane;
        private readonly ConcurrentQueue<string> _logQueue;
        private volatile bool _activate;

        #endregion Fields

        #region Constructors

        public LogService(AWPackage package, IVsOutputWindowPane vsOutputWindowPane)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _logQueue = new ConcurrentQueue<string>();
            _package = package;
            _vsOutputWindowPane = vsOutputWindowPane;

            _vsOutputWindowPane.SetName(AWPackage.NAME_CS_PLUGIN);
#if DEBUG
            LogLevel = EErrorLevel.Debug;
            _activate = true;
#endif
            // say hello
            Log(AWPackage.NAME_CS_PLUGIN + " " + getCurrentVersion() + " running", EErrorLevel.Info);
        }

        #endregion Constructors

        #region Enums

        public enum EErrorLevel
        {
            Debug = 1,
            Info = 2,
            Warning = 3,
            Error = 4
        }

        #endregion Enums

        #region Methods

        internal void Log(Exception ex, string additionalInfo = "", EErrorLevel errorLevel = EErrorLevel.Error, bool activate = true)
        {
            Log(
                ex.GetType() + (string.IsNullOrWhiteSpace(additionalInfo) ? "" : " (" + additionalInfo + ") ") + ":\r\n" + ex.ToString(),
                errorLevel,
                activate);
        }

        internal void Log(string msg, EErrorLevel errorLevel = EErrorLevel.Debug, bool activate = false)
        {
            if (errorLevel < LogLevel)
            {
                return;
            }

            _logQueue.Enqueue(msg + "\r\n");

            _activate = _activate || activate;

            _ = _package.JoinableTaskFactory.StartOnIdle(FlushLogsToOutputWindowPane, VsTaskRunContext.UIThreadBackgroundPriority);
        }

        private void FlushLogsToOutputWindowPane()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if(_activate)
            {
                _vsOutputWindowPane.Activate();
                _activate = false;
            }

            while (_logQueue.Count > 0 && _logQueue.TryDequeue(out string msg))
            {
                _vsOutputWindowPane.OutputStringThreadSafe(msg);
            }
        }

        private string getCurrentVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                return $"v{fvi.ProductMajorPart}.{fvi.ProductMinorPart}.{fvi.ProductBuildPart}.{fvi.ProductPrivatePart}";
            }
            catch
            {
                return ""; // no version info
            }
        }

        #endregion Methods

        #region Properties

        public EErrorLevel LogLevel { get; set; } = EErrorLevel.Info;

        #endregion Properties
    }
}