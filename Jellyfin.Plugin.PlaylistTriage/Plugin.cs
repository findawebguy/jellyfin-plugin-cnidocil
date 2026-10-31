using System;
using System.Collections.Generic;
using System.IO;
using Jellyfin.Plugin.PlaylistTriage.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.PlaylistTriage;

/// <summary>
/// The main plugin class for Playlist Triage and Library Pruning.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of <see cref="IApplicationPaths"/>.</param>
    /// <param name="xmlSerializer">Instance of <see cref="IXmlSerializer"/>.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        TryInjectClientScriptIntoWebIndex(applicationPaths?.WebPath);
    }

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public override string Name => "Cnidocil";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("b14f8263-5e92-4f6b-871d-5a9e7019c4b2");

    /// <inheritdoc />
    public override string Description =>
        "Cnidocil (jellyfin-plugin-cnidocil) - Quick playlist triage and media library pruning with hybrid hotkey + OSD marking, custom playbar overlay, and bulk disk management.";

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = "PlaylistTriage",
                DisplayName = "Cnidocil",
                EmbeddedResourcePath = GetType().Namespace + ".Web.triage.html",
                EnableInMainMenu = true,
                MenuSection = "library",
                MenuIcon = "playlist_play"
            }
        };
    }

    private static void TryInjectClientScriptIntoWebIndex(string? webPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(webPath))
            {
                return;
            }

            var indexPath = Path.Combine(webPath, "index.html");
            if (!File.Exists(indexPath))
            {
                return;
            }

            var html = File.ReadAllText(indexPath);
            var modified = false;

            // 1. Inject Chromium 129+ ScrollOptions.behavior strict validation safeguard (wrapped in try/catch to never crash)
            const string scrollPolyfill = "<script id=\"pt-scroll-fix\">!function(){function s(a){try{if(a&&1===a.length&&a[0]&&\"object\"==typeof a[0]&&null!==a[0]){var o=a[0];if(null===o.behavior||void 0===o.behavior||\"auto\"!==o.behavior&&\"smooth\"!==o.behavior&&\"instant\"!==o.behavior){var c=Object.assign({},o);return delete c.behavior,[c]}}}catch(e){}return a}if(\"undefined\"!=typeof Element&&Element.prototype.scrollTo){var e=Element.prototype.scrollTo;Element.prototype.scrollTo=function(){try{return e.apply(this,s(arguments))}catch(t){}}}if(\"undefined\"!=typeof window&&window.scrollTo){var w=window.scrollTo;window.scrollTo=function(){try{return w.apply(this,s(arguments))}catch(t){}}}}();</script>";
            if (!html.Contains("pt-scroll-fix", StringComparison.OrdinalIgnoreCase))
            {
                if (html.Contains("<head>", StringComparison.OrdinalIgnoreCase))
                {
                    html = html.Replace("<head>", "<head>" + scrollPolyfill, StringComparison.OrdinalIgnoreCase);
                    modified = true;
                }
            }
            else
            {
                // Ensure latest robust polyfill
                var start = html.IndexOf("<script id=\"pt-scroll-fix\">", StringComparison.OrdinalIgnoreCase);
                if (start != -1)
                {
                    var end = html.IndexOf("</script>", start, StringComparison.OrdinalIgnoreCase);
                    if (end != -1)
                    {
                        html = html.Substring(0, start) + scrollPolyfill + html.Substring(end + 9);
                        modified = true;
                    }
                }
            }

            // 2. Inject ClientScript before </body>
            const string scriptTag = "<script src=\"/Plugins/PlaylistTriage/ClientScript\" defer></script>";
            if (!html.Contains("/Plugins/PlaylistTriage/ClientScript", StringComparison.OrdinalIgnoreCase))
            {
                if (html.Contains("</body>", StringComparison.OrdinalIgnoreCase))
                {
                    html = html.Replace("</body>", scriptTag + "</body>", StringComparison.OrdinalIgnoreCase);
                    modified = true;
                }
            }

            if (modified)
            {
                File.WriteAllText(indexPath, html);
            }
        }
        catch
        {
            // Ignore if web directory is read-only; dashboard page also loads client script dynamically.
        }
    }
}
