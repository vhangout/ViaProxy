using ViaProxy;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Querying;
using SharpCifs.Util.DbsHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class InjectorImp
{
    private static Regex _rgx => new Regex(@"[\?&](([^&=]+)=([^&=#]*))");
    public static void ParseMovieSearchResultPostfix(ref RemoteSearchResult __result)
    {
        if (EntryPoint.Current.options.Enable.GetValueOrDefault(false))
        {
            if (__result.ImageUrl != null)
                __result.ImageUrl = $"/emby/Items/RemoteSearch/Image?imageUrl={__result.ImageUrl}&ProviderName=TheMovieDb&api_key={EntryPoint.Current.accessToken}";
        }
        else
        {
            EntryPoint.Current.Log("******* PROXY DISABLED");
        }
    }

    public static void GetImageResponsePrefix(ref string url, CancellationToken cancellationToken)
    {
        if (EntryPoint.Current.options.Enable.GetValueOrDefault(false))
        { 
            if (url.StartsWith("/emby")) 
            url = _rgx.Match(url).Groups[3].Value;
        }
        else
        {
            EntryPoint.Current.Log("******* PROXY DISABLED");
        }
    }

}

