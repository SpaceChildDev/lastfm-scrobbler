# Roadmap

## v1.0.0 — Current Release

- [x] Apple Music (Windows Store) scrobbling via SMTC API
- [x] Album name resolution via Last.fm `track.getInfo`
- [x] Configurable scrobble threshold (% and max seconds)
- [x] Track normalization with regex rules (Deluxe Edition, Remastered, etc.)
- [x] Dark UI with sidebar navigation (Monitor, Account, Scrobbling, Normalization)
- [x] System tray integration
- [x] Scrobble history (local SQLite)
- [x] Optional edit-before-scrobble dialog
- [x] Windows startup option

## v1.1.0 — Polish

- [ ] Album art display in the monitor window
- [ ] Windows 11 toast notifications with album art
- [ ] Scrobble history browser (searchable list of past scrobbles)
- [ ] Retry queue for failed scrobbles (offline support)
- [ ] Tray icon changes to reflect current state (playing, idle, error)

## v1.2.0 — Multi-player support

- [ ] iTunes / Apple Music (legacy) support
- [ ] Spotify support via SMTC
- [ ] Any SMTC-compatible player (generic mode)
- [ ] Per-player normalization rules

## v1.3.0 — Power features

- [ ] Love/unlove track from tray menu
- [ ] Tag-based normalization (not just regex)
- [ ] Import/export normalization rules
- [ ] Manual scrobble entry

## v2.0.0 — Distribution

- [ ] MSIX packaging for Microsoft Store distribution
- [ ] Auto-updater
- [ ] Code signing

## Longer term / Ideas

- Scrobble from local media players (VLC, foobar2000) via plugin
- Last.fm friends' listening activity in the tray
- macOS companion app (using MusicKit / MediaPlayer APIs)
