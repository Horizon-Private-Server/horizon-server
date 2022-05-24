using Server.Common.Logging;

namespace Server.NAT.Config
{
    #region NAT Server Config
    public class ServerSettings
    {
        /// <summary>
        /// Default Port of the NAT server.
        /// </summary>
        public int Port { get; set; } = 10070;

        /// <summary>
        /// When set, all nat ip requests will be receive the server's ip and this port.
        /// </summary>
        public int? OverridePort { get; set; } = null;

        /// <summary>
        /// Logging settings.
        /// </summary>
        public NATLogSettings Logging { get; set; } = new NATLogSettings();
    }
    #endregion
}