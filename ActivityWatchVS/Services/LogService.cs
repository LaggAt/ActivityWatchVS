using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;
using System.Reflection;

namespace ActivityWatchVS.Services
{
    internal class LogService
    {
        #region Fields

        private AWPackage _package;
        private IVsOutputWindowPane _vsOutputWindowPane;

        #endregion Fields

        #region Constructors

        public LogService(AWPackage package, IVsOutputWindowPane vsOutputWindowPane)
        {
            _package = package;
            _vsOutputWindowPane = vsOutputWindowPane;

            _vsOutputWindowPane.SetName(AWPackage.NAME_CS_PLUGIN);
            bool activate = false;
#if DEBUG
            LogLevel = EErrorLevel.Debug;
            activate = true;
#endif
            // say hello
            Log(AWPackage.NAME_CS_PLUGIN + " " + getCurrentVersion() + " running", EErrorLevel.Info, activate);
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
            try
            {
                _vsOutputWindowPane.OutputString(msg + "\r\n");
                if (activate)
                {
                    _vsOutputWindowPane.Activate();
                }
            }
            catch
            { }
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