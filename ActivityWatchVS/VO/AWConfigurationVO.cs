using IniWrapper.Attribute;

namespace ActivityWatchVS.VO
{
    public class AWConfigurationVO
    {
        #region Properties

        [IniIgnore]
        public bool ConfigFileExists { get; set; } = true;

        [IniOptions(Section = "server", Key = "cors_origins")]
        public string CorsOrigins { get; set; }

        [IniOptions(Section = "server-testing", Key = "cors_origins")]
        public string CorsOriginsTesting { get; set; }

        [IniOptions(Section = "server", Key = "host")]
        public string Host { get; set; } = "localhost";

        [IniOptions(Section = "server-testing", Key = "host")]
        public string HostTesting { get; set; } = "localhost";

        [IniOptions(Section = "server", Key = "port")]
        public int Port { get; set; } = 5600;

        [IniOptions(Section = "server-testing", Key = "port")]
        public int PortTesting { get; set; } = 5666;

        [IniOptions(Section = "server", Key = "storage")]
        public string Storage { get; set; } = "peewee";

        [IniOptions(Section = "server-testing", Key = "storage")]
        public string StorageTesting { get; set; } = "peewee";

        #endregion Properties
    }
}