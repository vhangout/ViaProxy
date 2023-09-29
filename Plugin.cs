using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace ViaProxy
{
    public class Plugin : BasePlugin<ViaProxyConfiguration>, IHasThumbImage, IHasWebPages

    {

        public static Plugin Instance { get; set; }        

        public override string Name => StaticName;
        public static string StaticName => "ViaProxy";
        public static string ProviderName => "Via Proxy";

        private Guid _id = new Guid(g: "F8481A81-3227-4720-875B-20A25918ACE9");

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public override Guid Id => _id;

        public override string Description => "MovieDB via proxy patcher";

        public ImageFormat ThumbImageFormat => ImageFormat.Png;

        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream(name: $"{type.Namespace}.thumb.png");
        }

        public IEnumerable<PluginPageInfo> GetPages() => new[]
        {
            new PluginPageInfo()
            {
                Name = "viaproxy",
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.ViaProxyPage.html",
            },
            new PluginPageInfo()
            {
                Name = "viaproxyjs",
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.ViaProxyPage.js",
            },
        };

    }
}