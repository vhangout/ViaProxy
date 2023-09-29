using System.Diagnostics;
using System.Runtime.CompilerServices;

using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using HttpRequestOptions = MediaBrowser.Common.Net.HttpRequestOptions;
using Emby.Server.Implementations.HttpClientManager;
using System.Reflection;
using System.Collections.Concurrent;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Querying;
using System.Net;
using MediaBrowser.Common.Plugins;

namespace ViaProxy
{
    public class EntryPoint : IServerEntryPoint, IDisposable
    {
        private const string harmony_lib = "0Harmony.dll";
        public static EntryPoint Current;

        public readonly string accessToken;

        private readonly ILogger _logger;
        private readonly IConfigurationManager _config;
        public readonly IServerConfigurationManager ServerConfiguration;
        private readonly IServerApplicationHost _applicationHost;      


        private readonly IHttpClient _httpClient;
        private readonly HttpClientInfo[] _clientInfo;
        public ViaProxyConfiguration options;


        public EntryPoint(
            ILogger logger,
            IConfigurationManager config,
            IHttpClient httpClient,
            IServerConfigurationManager serverConfiguration,
            IServerApplicationHost applicationHost,
            IAuthenticationRepository authRepo)
        {            
            _logger = logger;
            _config = config;
            ServerConfiguration = serverConfiguration;
            _httpClient = httpClient;

            _config.NamedConfigurationUpdated += new EventHandler<ConfigurationUpdateEventArgs>(ConfigWasUpdated);

            _applicationHost = applicationHost;

            QueryResult<AuthenticationInfo> queryResult = authRepo.Get(new AuthenticationInfoQuery()
            {
                IsActive = new bool?(true)
            });
            accessToken = queryResult.Items.Length != 0 ? queryResult.Items[0].AccessToken : null;

            _clientInfo = new HttpClientInfo[]
            {
                (HttpClientInfo)_httpClient.GetConnectionContext(new HttpRequestOptions { Url = "https://image.tmdb.org" }),
                (HttpClientInfo)_httpClient.GetConnectionContext(new HttpRequestOptions { Url = "https://api.themoviedb.org" }),
            };

            options = (ViaProxyConfiguration)_config.GetConfiguration(ViaProxyConfigurationFactory.Key);


            Current = this;
        }

 
        public void Run()
        {
            UpdateClients();
            var self_plugin = _applicationHost.Plugins.Where(p => Plugin.StaticName.Equals(p.Name)).FirstOrDefault();
            var harmony_path = Path.Combine(Path.GetDirectoryName(self_plugin.AssemblyFilePath), "viaproxylib", harmony_lib);
            Log($"CHECKING HARMONY LIB PATH {harmony_path}");
            if (File.Exists(harmony_path)) {
                Log($"FOUND HARMONY LIB AT PATH {harmony_path}");
            }
            else
            {
                Log($"NOT FOUND HARMONY LIB AT PATH {harmony_path}");
                Log($"USE DEFAULT BY FILENAME {harmony_lib}");
                harmony_path = null;
            }
            var harmony_assembly = Assembly.LoadFrom(harmony_path != null ? harmony_path : harmony_lib);
            if (harmony_assembly != null)
            {
                UseIt(harmony_assembly);
            }
            else
            {
                Log($"NOT FOUND {harmony_lib}");
            }
        }

        private void ConfigWasUpdated(object sender, ConfigurationUpdateEventArgs e)
        {
            UpdateClients();
        }

        private void UpdateClients()
        {
            LogCall();
            options = (ViaProxyConfiguration)_config.GetConfiguration(ViaProxyConfigurationFactory.Key);
            foreach (HttpClientInfo context in _clientInfo)
            {
                Log($"UPDATE FOR URL {context.HttpClient.BaseAddress}");
                context.HttpClient = null;
                var handler = new HttpClientHandler();

                if (options.Enable.GetValueOrDefault(false))
                {
                    Log($"USE PROXY {options.ProxyType.ToLower()}://{options.ProxyUrl}:{options.ProxyPort}");
                    handler.Proxy = new WebProxy
                    {
                        Address = new Uri($"{options.ProxyType.ToLower()}://{options.ProxyUrl}:{options.ProxyPort}"),
                        BypassProxyOnLocal = false,
                        UseDefaultCredentials = false
                    };
                    if (options.EnableCredentials.GetValueOrDefault(false))
                    {
                        Log("Setup credential");
                        handler.Proxy.Credentials = new NetworkCredential(options.Login, options.Password);
                    }
                }

                context.HttpClient = new HttpClient(handler);
            }
        }

        private void UseIt(Assembly asm)
        {
            Type? Harmony = asm.GetType("HarmonyLib.Harmony");
            Log($"FOUND HARMONY CLASS {Harmony.FullName}");

            MethodInfo? Patch = Harmony.GetMethod("Patch", BindingFlags.Public | BindingFlags.Instance);
            Log($"FOUND HARMONY METHOD {Patch.Name}");

            Type? HarmonyMethod = asm.GetType("HarmonyLib.HarmonyMethod");
            Log($"FOUND HARMONY CLASS {HarmonyMethod.FullName}");

            var harmony = Activator.CreateInstance(Harmony, "moviedb.patcher");

            var dbMoviePlugin = _applicationHost.Plugins.Where(p => "MovieDb".Equals(p.Name)).FirstOrDefault();
            if (dbMoviePlugin == null)
            {
                Log($"PLUGIN NOT FOUND");
            }
            else
            {
                Log($"FOUND PLUGIN {dbMoviePlugin.AssemblyFilePath}");
                Log($"CHECK VERSION");
                var mdb_version = new Version(1, 7, 3, 0);
                if (dbMoviePlugin.Version < mdb_version)
                {
                    Log($"FOUND OLD VERSION {dbMoviePlugin.Version} LESS THEN {mdb_version}");
                    Log($"PATH IMPOSIBLE. IMAGES IN SEARCH BLOCK DO NOT SHOW");
                    return;
                }
                Log($"PLUGIN VERSION {dbMoviePlugin.Version}");

                var plugin_assembly = dbMoviePlugin.GetType().Assembly;
                Log($"GET PLUGIN ASSEMBLY {plugin_assembly.FullName}");

                PatchMethod(harmony, Patch, 
                            plugin_assembly, "MovieDb.MovieDbSearch", "ParseMovieSearchResult", BindingFlags.Instance | BindingFlags.NonPublic,
                            Activator.CreateInstance(HarmonyMethod, typeof(InjectorImp).GetMethod("ParseMovieSearchResultPostfix", BindingFlags.Static | BindingFlags.Public)),
                            false);

                PatchMethod(harmony, Patch, 
                            plugin_assembly, "MovieDb.MovieDbProviderBase", "GetImageResponse", BindingFlags.Instance | BindingFlags.Public,
                            Activator.CreateInstance(HarmonyMethod, typeof(InjectorImp).GetMethod("GetImageResponsePrefix", BindingFlags.Static | BindingFlags.Public)),
                            true);
            }
        }

        private void PatchMethod(object harmony, MethodInfo Patch, Assembly movieDB, string className, string methodName, BindingFlags flags, 
            object harmonyMethod, bool isPrefix)
        {
            var sourceClass = movieDB.GetType(className);
            if (sourceClass != null)
            {
                Log($"FOUND CLASS {sourceClass.FullName}");
                var sourceMethod = sourceClass.GetMethod(methodName, flags);
                if (sourceMethod != null)
                {
                    Log($"FOUND METHOD {sourceMethod.Name}");
                    Patch?.Invoke(harmony, new[] { sourceMethod, isPrefix ? harmonyMethod : null, isPrefix ? null : harmonyMethod, null, null });
                    var style = isPrefix ? "PREFIX" : "POSTFIX";
                    Log($"PATCHED METHOD {sourceMethod.Name} WITH {style}");
                }
            }
        }

        public void Dispose()
        {
            Current = null;
        }

        public void Log(string message)
        {
            _logger.Info($"******* {message}");
        }

        public void LogStack()
        {            
            Log($"LOG STACK {new StackTrace(true)}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogCall()
        {
            var st = new StackTrace(true);
            var sf = st.GetFrame(1);
            Log($"CALLING {sf.GetMethod().ReflectedType.Name}:{sf.GetMethod().Name}");
        }

        public void LogDump(object obj)
        {
            Log("DUMP");
            _logger.Info($"{ObjectDumper.Dump(obj, DumpStyle.CSharp)}");            
        }

    }
}
