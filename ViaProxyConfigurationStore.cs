using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Logging;
using System.ComponentModel.DataAnnotations;

namespace ViaProxy
{
    internal class ViaProxyConfigurationStore : ConfigurationStore

    {
        public const string ConfigurationKey = "viaproxy";        
        private readonly ILogger _logger;

        public ViaProxyConfigurationStore(ILogger logger)
        {
            ConfigurationType = typeof(ViaProxyConfiguration);
            Key = ConfigurationKey;
            _logger = logger;
        }        
    }
}
