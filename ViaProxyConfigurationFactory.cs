using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Logging;

namespace ViaProxy
{
    public class ViaProxyConfigurationFactory : IConfigurationFactory
    {
        public static string Key => "viaproxy";
        private readonly ILogger _logger;

        public ViaProxyConfigurationFactory(ILogger logger)
        {
            _logger = logger;
        }

        public IEnumerable<ConfigurationStore> GetConfigurations() => new ConfigurationStore[1]
        {
            new ViaProxyConfigurationStore(_logger)            
        };
    }
}
