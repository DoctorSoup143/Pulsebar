using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Pulsebar
{
    /// <summary>
    /// Checks GitHub Releases for a newer version of Pulsebar. Never downloads or installs
    /// anything itself - it only reports whether a newer tagged release exists, and where to
    /// find it, so the caller can point the user at it.
    /// </summary>
    public static class UpdateChecker
    {
        private const string LatestReleaseApiUrl = "https://api.github.com/repos/DoctorSoup143/Pulsebar/releases/latest";

        public sealed class UpdateInfo
        {
            public Version Version { get; set; }

            public string TagName { get; set; }

            public string HtmlUrl { get; set; }
        }

        public static async Task<UpdateInfo> CheckForUpdateAsync(Version currentVersion)
        {
            try
            {
                using (HttpClient _client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
                {
                    _client.DefaultRequestHeaders.UserAgent.ParseAdd("Pulsebar-UpdateChecker");
                    _client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

                    string _json = await _client.GetStringAsync(LatestReleaseApiUrl).ConfigureAwait(false);

                    JObject _release = JObject.Parse(_json);

                    string _tagName = (string)_release["tag_name"];
                    string _htmlUrl = (string)_release["html_url"];

                    if (string.IsNullOrEmpty(_tagName) || string.IsNullOrEmpty(_htmlUrl))
                    {
                        return null;
                    }

                    string _versionText = _tagName.TrimStart('v', 'V');

                    if (!Version.TryParse(_versionText, out Version _latestVersion))
                    {
                        return null;
                    }

                    // Compare on Major.Minor.Build only - tags are "vX.Y.Z" (3 parts) while the
                    // assembly version carries a 4th Revision component that would otherwise
                    // skew the comparison (an unset Revision is -1, not 0).
                    Version _normalizedCurrent = new Version(currentVersion.Major, currentVersion.Minor, Math.Max(currentVersion.Build, 0));

                    if (_latestVersion <= _normalizedCurrent)
                    {
                        return null;
                    }

                    return new UpdateInfo
                    {
                        Version = _latestVersion,
                        TagName = _tagName,
                        HtmlUrl = _htmlUrl
                    };
                }
            }
            catch
            {
                // Best-effort only: offline, repo not public yet, rate-limited, malformed
                // response, whatever - an update check must never interrupt or crash the app.
                return null;
            }
        }
    }
}
