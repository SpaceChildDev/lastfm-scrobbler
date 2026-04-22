using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using LastFmScrobbler.Models;
using Newtonsoft.Json.Linq;

namespace LastFmScrobbler.Core;

public class LastFmClient
{
    private const string ApiBase = "https://ws.audioscrobbler.com/2.0/";
    private const string AuthUrl = "https://www.last.fm/api/auth/";

    private readonly HttpClient _http = new();
    private string _apiKey = string.Empty;
    private string _apiSecret = string.Empty;
    private string? _sessionKey;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_sessionKey);

    public void Configure(string apiKey, string apiSecret, string? sessionKey)
    {
        _apiKey = apiKey;
        _apiSecret = apiSecret;
        _sessionKey = sessionKey;
    }

    // ── Auth Flow ────────────────────────────────────────────────────────────

    /// <summary>Step 1: Get a token and return the URL for the user to authorize.</summary>
    public async Task<(string token, string authUrl)> GetAuthUrlAsync()
    {
        var result = await CallAsync(new Dictionary<string, string>
        {
            ["method"] = "auth.getToken",
            ["api_key"] = _apiKey
        }, signed: false);

        var token = result["token"]?.ToString()
            ?? throw new Exception("No token returned from Last.fm");

        var url = $"{AuthUrl}?api_key={_apiKey}&token={token}";
        return (token, url);
    }

    /// <summary>Step 2: Exchange the authorized token for a session key.</summary>
    public async Task<(string sessionKey, string username)> GetSessionAsync(string token)
    {
        var result = await CallAsync(new Dictionary<string, string>
        {
            ["method"] = "auth.getSession",
            ["api_key"] = _apiKey,
            ["token"] = token
        }, signed: true);

        var session = result["session"]
            ?? throw new Exception("No session in response");

        var sk = session["key"]?.ToString()
            ?? throw new Exception("No session key");
        var name = session["name"]?.ToString() ?? string.Empty;

        _sessionKey = sk;
        return (sk, name);
    }

    // ── Track Info ───────────────────────────────────────────────────────────

    /// <summary>
    /// Looks up the canonical album name from Last.fm for a given artist + track.
    /// Returns null if not found or on error.
    /// </summary>
    public async Task<string?> GetAlbumNameAsync(string artist, string title)
    {
        try
        {
            var result = await CallAsync(new Dictionary<string, string>
            {
                ["method"]  = "track.getInfo",
                ["api_key"] = _apiKey,
                ["artist"]  = artist,
                ["track"]   = title,
                ["autocorrect"] = "1",
            }, signed: false);

            return result.SelectToken("track.album.title")?.ToString();
        }
        catch
        {
            return null;
        }
    }

    // ── Love / Unlove ────────────────────────────────────────────────────────

    public async Task LoveTrackAsync(string artist, string title)
    {
        if (!IsAuthenticated) return;
        await CallAsync(new Dictionary<string, string>
        {
            ["method"]  = "track.love",
            ["api_key"] = _apiKey,
            ["sk"]      = _sessionKey!,
            ["artist"]  = artist,
            ["track"]   = title,
        }, signed: true);
    }

    public async Task UnloveTrackAsync(string artist, string title)
    {
        if (!IsAuthenticated) return;
        await CallAsync(new Dictionary<string, string>
        {
            ["method"]  = "track.unlove",
            ["api_key"] = _apiKey,
            ["sk"]      = _sessionKey!,
            ["artist"]  = artist,
            ["track"]   = title,
        }, signed: true);
    }

    // ── Scrobbling ───────────────────────────────────────────────────────────

    public async Task UpdateNowPlayingAsync(Track track)
    {
        if (!IsAuthenticated) return;

        var p = new Dictionary<string, string>
        {
            ["method"] = "track.updateNowPlaying",
            ["api_key"] = _apiKey,
            ["sk"] = _sessionKey!,
            ["artist"] = track.Artist,
            ["track"] = track.Title,
        };
        if (!string.IsNullOrEmpty(track.Album)) p["album"] = track.Album;
        if (track.DurationSeconds.HasValue) p["duration"] = track.DurationSeconds.Value.ToString();

        await CallAsync(p, signed: true);
    }

    public async Task<bool> ScrobbleAsync(Track track, DateTime playedAt)
    {
        if (!IsAuthenticated) return false;

        var p = new Dictionary<string, string>
        {
            ["method"] = "track.scrobble",
            ["api_key"] = _apiKey,
            ["sk"] = _sessionKey!,
            ["artist[0]"] = track.Artist,
            ["track[0]"] = track.Title,
            ["timestamp[0]"] = ((DateTimeOffset)playedAt.ToUniversalTime()).ToUnixTimeSeconds().ToString(),
        };
        if (!string.IsNullOrEmpty(track.Album)) p["album[0]"] = track.Album;
        if (track.DurationSeconds.HasValue) p["duration[0]"] = track.DurationSeconds.Value.ToString();

        var result = await CallAsync(p, signed: true);
        var accepted = result.SelectToken("scrobbles.@attr.accepted");
        return accepted?.Value<int>() == 1;
    }

    /// <summary>Scrobble up to 50 tracks in one API call. Returns the number accepted.</summary>
    public async Task<int> ScrobbleBatchAsync(List<(Track track, DateTime playedAt)> items)
    {
        if (!IsAuthenticated || items.Count == 0) return 0;

        var p = new Dictionary<string, string>
        {
            ["method"]  = "track.scrobble",
            ["api_key"] = _apiKey,
            ["sk"]      = _sessionKey!,
        };

        for (int i = 0; i < items.Count; i++)
        {
            var (track, playedAt) = items[i];
            p[$"artist[{i}]"]    = track.Artist;
            p[$"track[{i}]"]     = track.Title;
            p[$"timestamp[{i}]"] = ((DateTimeOffset)playedAt.ToUniversalTime()).ToUnixTimeSeconds().ToString();
            if (!string.IsNullOrEmpty(track.Album)) p[$"album[{i}]"] = track.Album;
        }

        var result   = await CallAsync(p, signed: true);
        var accepted = result.SelectToken("scrobbles.@attr.accepted");
        return accepted?.Value<int>() ?? 0;
    }

    // ── HTTP / Signature ─────────────────────────────────────────────────────

    private async Task<JObject> CallAsync(Dictionary<string, string> parameters, bool signed)
    {
        if (signed)
        {
            var sig = BuildSignature(parameters);
            parameters["api_sig"] = sig;
        }
        parameters["format"] = "json";

        var content = new FormUrlEncodedContent(parameters.Select(p =>
            new KeyValuePair<string, string>(p.Key, p.Value)));

        var response = await _http.PostAsync(ApiBase, content);
        var json = await response.Content.ReadAsStringAsync();
        var obj = JObject.Parse(json);

        if (obj["error"] is JToken err)
            throw new Exception($"Last.fm error {err}: {obj["message"]}");

        return obj;
    }

    private string BuildSignature(Dictionary<string, string> parameters)
    {
        // Signature = MD5( sorted_key+value pairs concatenated + secret )
        var sorted = parameters
            .Where(p => p.Key != "format" && p.Key != "callback")
            .OrderBy(p => p.Key, StringComparer.Ordinal);

        var sb = new StringBuilder();
        foreach (var (key, value) in sorted)
        {
            sb.Append(key);
            sb.Append(value);
        }
        sb.Append(_apiSecret);

        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
