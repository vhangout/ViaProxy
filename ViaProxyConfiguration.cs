using MediaBrowser.Model.Plugins;

namespace ViaProxy
{
    public class ViaProxyConfiguration : BasePluginConfiguration
    {
        public bool? Enable { get; set; }
        public string? ProxyType { get; set; }
        public string? ProxyUrl { get; set; }
        public int? ProxyPort { get; set; }
        public bool? EnableCredentials { get; set; }
        public string? Login { get; set; }
        public string? Password { get; set; }
    }
}
