namespace LastFmScrobbler.Models;

public class Track
{
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public string? AlbumArtist { get; set; }
    public string RawAlbum { get; set; } = string.Empty;  // SMTC'den gelen ham veri (debug için)
    public int? DurationSeconds { get; set; }
    public int? TrackNumber { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public string? SourceApp { get; set; }

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Title) &&
        !string.IsNullOrWhiteSpace(Artist);

    public Track Clone() => new()
    {
        Title = Title,
        Artist = Artist,
        Album = Album,
        AlbumArtist = AlbumArtist,
        RawAlbum = RawAlbum,
        DurationSeconds = DurationSeconds,
        TrackNumber = TrackNumber,
        DetectedAt = DetectedAt,
        SourceApp = SourceApp
    };

    public bool IsSameTrack(Track? other)
    {
        if (other is null) return false;
        return string.Equals(Title, other.Title, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Artist, other.Artist, StringComparison.OrdinalIgnoreCase);
    }

    public override string ToString() => $"{Artist} - {Title}";
}

public class ScrobbleRecord
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public DateTime ScrobbledAt { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class PendingScrobble
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public DateTime PlayedAt { get; set; }
    public DateTime QueuedAt { get; set; }
}
