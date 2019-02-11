using ActivityWatchVS.VO;
using IniWrapper.Wrapper;
using System;
using System.IO;

namespace ActivityWatchVS.Services
{
    internal class AWConfigurationService
    {
        #region Fields

        private AWConfigurationVO _awConfig = null;
        private volatile object _lock = new object();
        private AWPackage _package;

        public AWConfigurationVO AwConfig
        {
            get
            {
                //if (_awConfig == null)
                //{
                lock (_lock)
                {
                    _awConfig = init();
                }
                //}
                return _awConfig;
            }
        }

        #endregion Fields

        #region Constructors

        public AWConfigurationService(AWPackage package)
        {
            _package = package;
        }

        private AWConfigurationVO init()
        {
            var iniPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"activitywatch\activitywatch\aw-server\aw-server.ini");
            if (!File.Exists(iniPath))
            {
                //Defaults
                return new AWConfigurationVO() { ConfigFileExists = false };
            }

            // read from INI
            var iniWrapperFactory = new IniWrapperFactory();
            var iniWrapper = iniWrapperFactory.CreateWithDefaultIniParser(
                new IniWrapper.Settings.IniSettings()
                {
                    IniFilePath = iniPath,
                    MissingFileWhenLoadingHandling = IniWrapper.Settings.MissingFileWhenLoadingHandling.DoNotLoad
                });

            var config = iniWrapper.LoadConfiguration<VO.AWConfigurationVO>();
            config.ConfigFileExists = true;
            return config;
        }

        #endregion Constructors
    }
}