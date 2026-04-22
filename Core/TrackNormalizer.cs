using System.Text.RegularExpressions;
using LastFmScrobbler.Models;

namespace LastFmScrobbler.Core;

public class TrackNormalizer
{
    private List<NormalizationRule> _rules = [];

    public void UpdateRules(List<NormalizationRule> rules)
    {
        _rules = rules.Where(r => r.IsEnabled).ToList();
    }

    public Track Normalize(Track original)
    {
        var track = original.Clone();

        foreach (var rule in _rules)
        {
            switch (rule.Field)
            {
                case RuleField.Title:
                    track.Title = Apply(track.Title, rule);
                    break;
                case RuleField.Artist:
                    track.Artist = Apply(track.Artist, rule);
                    break;
                case RuleField.Album:
                    track.Album = Apply(track.Album, rule);
                    if (track.AlbumArtist != null)
                        track.AlbumArtist = Apply(track.AlbumArtist, rule);
                    break;
            }
        }

        // Trim trailing/leading whitespace after rules
        track.Title = track.Title.Trim();
        track.Artist = track.Artist.Trim();
        track.Album = track.Album.Trim();

        return track;
    }

    private static string Apply(string input, NormalizationRule rule)
    {
        try
        {
            return Regex.Replace(input, rule.Pattern, rule.Replacement,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(100));
        }
        catch (RegexMatchTimeoutException)
        {
            return input;
        }
        catch (ArgumentException)
        {
            // Invalid regex — skip rule
            return input;
        }
    }
}
