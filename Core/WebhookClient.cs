using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace LastFmScrobbler.Core;

public static class WebhookClient
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

    public static void Post(string url, string eventName, Models.Track? track, bool? success = null)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        _ = PostAsync(url, eventName, track, success);
    }

    private static async Task PostAsync(string url, string eventName, Models.Track? track, bool? success)
    {
        try
        {
            var payload = new
            {
                @event    = eventName,
                track     = track is null ? null : new
                {
                    title            = track.Title,
                    artist           = track.Artist,
                    album            = track.Album,
                    duration_seconds = track.DurationSeconds,
                    source           = track.SourceApp,
                },
                success   = success,
                timestamp = DateTime.UtcNow.ToString("o"),
            };

            var json    = JsonConvert.SerializeObject(payload, Formatting.None);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _http.PostAsync(url, content);
        }
        catch { /* best-effort, never affects scrobbling */ }
    }
}
