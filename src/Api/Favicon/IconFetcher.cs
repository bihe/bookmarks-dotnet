using System;
using System.Net;
using System.Threading.Tasks;
using Flurl.Http;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Api.Favicon
{
    public class IconFetcher
    {
        const string SiteFavicon = "favicon.ico";
        readonly ILogger<IconFetcher> _logger;

        public IconFetcher(ILogger<IconFetcher> logger)
        {
            _logger = logger;
        }

        public async Task<(string filename, byte[] payload)> GetFaviconFromUrl(string url)
        {
            string fileName = "";
            byte[] payload = new byte[0];
            try
            {
                var baseUrl = BaseUrl(url);

                // 1) get the HTML page of the URL and parse the icon, shortcut icon locations
                var result = await ParseHTMLForFavicon(url);
                if (result.ok)
                {
                    var uri = new Uri(baseUrl);
                    var iconUrl = result.url;

                    // we have parsed the favicon from the html
                    // now ensure that the parsed url is downloadable html pages use some kind of tricks:
                    // a) missing base-url href=/assets/abc/favicon.png
                    // b) missing scheme //cdn.com/abc/favicon.png
                    if (iconUrl.StartsWith("//"))
                    {
                        // missing scheme
                        iconUrl = uri.Scheme + ":" + iconUrl;
                    }
                    else if (iconUrl.StartsWith("/"))
                    {
                        // missing base-url
                        iconUrl = baseUrl + iconUrl;
                    }
                    else if (iconUrl.StartsWith("./"))
                    {
                        // relative path
                        iconUrl = iconUrl.Replace("./", "/");
                        iconUrl = baseUrl + iconUrl;
                    }

                    payload = await DownloadFavicon(iconUrl);
                    fileName = result.filename;
                }
                else
                {
                    // 2) we could not parse the favicon from the provided HTML - fallback to fetch
                    // the favicon from the baseurl
                    var faviconBaseUrlLocation = PlainIconSiteUrl(url);
                    payload = await DownloadFavicon(faviconBaseUrlLocation);
                    fileName = SiteFavicon;
                }
            }
            catch(Exception EX)
            {
                _logger.LogError($"Could not get favicon from url '{url}' because of error: {EX.Message}");
            }
            return (fileName, payload);
        }

        async Task<byte[]> DownloadFavicon(string url)
        {
            _logger.LogDebug($"Will try to fetch favicon using url '{url}'.");
            return await url.GetBytesAsync();
        }

        async Task<(string url, string filename, bool ok)> ParseHTMLForFavicon(string domain)
        {
            var baseUrl = BaseUrl(domain);
            var response = await baseUrl.GetAsync();
            var ok = false;
            if (response.StatusCode == (int)HttpStatusCode.OK)
            {
                var content = await response.GetStringAsync();
                if (!string.IsNullOrEmpty(content))
                {
                    HtmlDocument page = new HtmlDocument();
                    page.LoadHtml(content);
                    var filename = "";
                    var favicon = TryFaviconDefinitions(page);

                    if (!string.IsNullOrEmpty(favicon))
                    {
                        _logger.LogInformation($"Got favicon '{favicon}'");
                        var parts = favicon.Split("/", StringSplitOptions.RemoveEmptyEntries);
                        if (parts != null && parts.Length > 0)
                        {
                            filename = parts[parts.Length-1];
                            ok = true;
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"Could not get favicon");
                    }
                    return (favicon, filename, ok);
                }
                else
                {
                    _logger.LogWarning($"Could not get content of domain '{domain}'");
                }
            }
            else
            {
                _logger.LogWarning($"No result from url '{domain}'");
            }
            return ("", "", ok);
        }


        string PlainIconSiteUrl(string url)
        {
            return $"{BaseUrl(url)}/{SiteFavicon}";
        }

        // parse a given url and only return the base-url scheme + hostname
        string BaseUrl(string url)
        {
            var baseUrl = new Uri(url);
            return $"{baseUrl.Scheme}://{baseUrl.Host}";
        }

        string TryFaviconDefinitions(HtmlDocument document)
        {
            var favicon = ParseFaviconFromHtml(document, "icon");
            if (string.IsNullOrEmpty(favicon))
                favicon = ParseFaviconFromHtml(document, "shortcut icon");
            return favicon;
        }

        string ParseFaviconFromHtml(HtmlDocument document, string faviconDefinition)
        {
            var el = document.DocumentNode.SelectSingleNode($"/html/head/link[@rel='{faviconDefinition}' and @href]");
            if (el != null)
            {
                return el.Attributes["href"].Value;
            }
            return "";
        }
    }
}
