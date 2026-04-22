namespace LastFmScrobbler.Models;

public enum RuleField
{
    Title,
    Artist,
    Album
}

public class NormalizationRule
{
    public int Id { get; set; }
    public RuleField Field { get; set; }
    public string Pattern { get; set; } = string.Empty;   // regex
    public string Replacement { get; set; } = string.Empty; // "" = delete match
    public bool IsEnabled { get; set; } = true;
    public string Description { get; set; } = string.Empty;
    public bool IsBuiltIn { get; set; } = false; // built-in rules can be disabled but not deleted

    public static List<NormalizationRule> GetDefaults() =>
    [
        // Album cleanup
        new() { Field = RuleField.Album, Pattern = @"\s*[\(\[](Deluxe Edition|Deluxe Version|Super Deluxe|Super Deluxe Edition|Super Deluxe Version)[\)\]]", Replacement = "", Description = "Deluxe Edition", IsBuiltIn = true },
        new() { Field = RuleField.Album, Pattern = @"\s*[\(\[](Expanded Edition|Expanded Version)[\)\]]", Replacement = "", Description = "Expanded Edition", IsBuiltIn = true },
        new() { Field = RuleField.Album, Pattern = @"\s*[\(\[](Anniversary Edition|[0-9]+th Anniversary Edition)[\)\]]", Replacement = "", Description = "Anniversary Edition", IsBuiltIn = true },
        new() { Field = RuleField.Album, Pattern = @"\s*[\(\[](Remastered [0-9]{4}|[0-9]{4} Remaster|Remaster)[\)\]]", Replacement = "", Description = "Remastered (album)", IsBuiltIn = true },
        new() { Field = RuleField.Album, Pattern = @"\s*[\(\[](Bonus Track Version|Standard Edition|Original Recording Remastered)[\)\]]", Replacement = "", Description = "Bonus Track/Standard", IsBuiltIn = true },
        new() { Field = RuleField.Album, Pattern = @"\s*[\(\[](Complete Edition|Collector's Edition|Platinum Edition)[\)\]]", Replacement = "", Description = "Collector editions", IsBuiltIn = true },

        // Title cleanup
        new() { Field = RuleField.Title, Pattern = @"\s*-\s*Remastered\s*[0-9]*", Replacement = "", Description = "Remastered (track)", IsBuiltIn = true },
        new() { Field = RuleField.Title, Pattern = @"\s*[\(\[](Remastered [0-9]{4}|[0-9]{4} Remaster|Remaster|Remastered)[\)\]]", Replacement = "", Description = "Remastered brackets (track)", IsBuiltIn = true },
        new() { Field = RuleField.Title, Pattern = @"\s*[\(\[](Explicit|Clean Version|Radio Edit|Radio Version|Single Version)[\)\]]", Replacement = "", Description = "Explicit/Radio/Single", IsBuiltIn = true },

        // Artist cleanup
        new() { Field = RuleField.Artist, Pattern = @"\s+(feat\.|ft\.|Feat\.|Ft\.|featuring|Featuring)\s+", Replacement = " feat. ", Description = "Normalize feat. format", IsBuiltIn = true },
    ];
}
