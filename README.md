# Last.fm Scrobbler for Apple Music on Windows

> Automatic Last.fm scrobbling for the **Apple Music Windows app** — because the official Last.fm app doesn't support it.

[![Release](https://img.shields.io/github/v/release/SpaceChildDev/lastfm-scrobbler)](https://github.com/SpaceChildDev/lastfm-scrobbler/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)](https://github.com/SpaceChildDev/lastfm-scrobbler)

---

## The Problem

Apple Music arrived on Windows as a Microsoft Store app in 2023. However, **the official Last.fm desktop app does not detect Apple Music on Windows** — it was never updated to support the new Store version. Audioscrobbler-based solutions like the legacy Last.fm app only work with iTunes.

This app fills that gap using the **Windows System Media Transport Controls (SMTC) API**, the same interface Windows uses for the media overlay (Win+K), which Apple Music properly integrates with.

This app fills that gap and works with **Apple Music for Windows**, **iTunes**, and any other media player that registers with SMTC — including Spotify, VLC, and others.

---

## Features

### Core Scrobbling
- Automatic scrobbling from **Apple Music (Microsoft Store app)** on Windows 10/11
- Resolves correct **album names** from the Last.fm API (Apple Music's SMTC metadata is incomplete)
- "Now Playing" updates sent to Last.fm in real time
- Configurable scrobble threshold (% of track played or max seconds)
- **System tray app** — runs silently in the background, dark modern UI

### Offline Queue
- If a scrobble fails due to a network error, it is saved to a local queue
- Queue is flushed automatically on startup and every 5 minutes
- Batches up to 50 queued scrobbles in a single API call

### Duplicate Protection
- Configurable window (default 5 min) skips re-scrobbling the same track after pausing/resuming
- Prevents double-scrobbles without affecting normal play

### Love / Unlove
- ♥ button in the Monitor view — love or unlove the currently playing track directly in Last.fm

### Album Art
- Album artwork fetched from SMTC and shown in the Monitor card

### Scrobble History & Stats
- Full local history in SQLite — last 200 scrobbles with artist, track, album, time, status
- Stats bar: total, today, this week, pending queue count

### Manual Scrobble
- Add any scrobble by hand (artist, track, album, played-at time) from the History page

### Track Normalization
- Regex rules strip edition tags from album/track names: `(Deluxe Edition)`, `- Remastered 2011`, etc.
- Built-in rules can be toggled; custom rules can be added and deleted

### Customization
- **Accent color picker** — change the UI highlight color
- **Windows startup** — optional registry run key
- **Now Playing balloon tip** — optional tray notification on track change
- **Edit before scrobble** — review metadata before it is sent

---

## Installation

### Option A — Portable (recommended)

1. Go to the [**Releases**](https://github.com/SpaceChildDev/lastfm-scrobbler/releases) page
2. Download `LastFmScrobbler.exe`
3. Run it — no installation needed
4. The app stores its data in the same folder as the exe

### Option B — Build from source

See [Building from Source](#building-from-source) below.

**Requirements:** Windows 10 version 19041 (20H1) or later, Windows 11 recommended.  
No .NET runtime installation needed — the app is self-contained.

---

## Getting Your Last.fm API Key

This app requires a Last.fm API key to authenticate and submit scrobbles. The key is free and takes about 2 minutes to obtain.

1. Log in to [last.fm](https://www.last.fm) with your account
2. Go to [last.fm/api/account/create](https://www.last.fm/api/account/create)
3. Fill in the form:
   - **Application name:** anything, e.g. `My Scrobbler`
   - **Application description:** anything
   - **Callback URL:** leave blank
4. Submit — you'll get an **API Key** and a **Shared Secret**
5. Open Last.fm Scrobbler → **Hesap** tab → paste both values → click **Last.fm ile Giriş**

Your API key and session token are stored **only on your local machine** in the app's SQLite database. They are never sent anywhere except Last.fm's own servers.

---

## Usage

1. Start the app — it appears as an icon in the system tray (bottom-right, notification area)
2. Right-click the tray icon to access **Monitor**, **Settings**, or **Exit**
3. Double-click the tray icon to open the Monitor window
4. Play a track in Apple Music — the monitor shows the current track and a progress bar toward the scrobble threshold
5. Once the threshold is reached, the track is automatically scrobbled to Last.fm

### Scrobble threshold

By default, a track is scrobbled after **50% of its duration** OR **240 seconds**, whichever comes first — matching Last.fm's own guidelines. The minimum is always 30 seconds. Both values are configurable in the **Scrobbling** settings page.

### Settings reference

| Setting | Default | Description |
|---|---|---|
| Scrobble threshold | 50% | Percentage of track duration before scrobbling |
| Maximum | 240 s | Hard cap — scrobbles after this many seconds regardless of percentage |
| Duplicate window | 5 min | Skip repeat scrobbles of the same track within this window (0 = off) |
| Filter Apple Music | On | Only scrobble from Apple Music (off = use whatever SMTC reports as active) |
| Edit before scrobble | Off | Show an edit dialog before each scrobble |
| Now Playing notification | Off | Balloon tip from the tray icon on track change |
| Start with Windows | Off | Adds/removes a `HKCU\Run` registry key |
| Auto Normalize | On | Apply normalization rules before scrobbling |
| Accent color | #BA0000 | UI highlight color |

---

## Building from Source

**Requirements:**
- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)

```bash
git clone https://github.com/SpaceChildDev/lastfm-scrobbler.git
cd lastfm-scrobbler

# Build
dotnet build

# Publish self-contained single-file exe
dotnet publish -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:EnableCompressionInSingleFile=true \
  -c Release -o publish/
```

The published executable will be at `publish/LastFmScrobbler.exe`.

> **Note:** No API keys are hardcoded. All credentials are entered by the user at runtime and stored in the local SQLite database (`lastfm_scrobbler.db`). The `.db` file is excluded via `.gitignore`.

---

## How it works

Apple Music on Windows registers itself with the Windows SMTC (System Media Transport Controls) API. This app listens to SMTC events to detect track changes.

One quirk: Apple Music's SMTC implementation combines the artist and album title into a single field using an em dash separator (`Artist — Album`). This app parses that field correctly and additionally queries the Last.fm `track.getInfo` API to resolve the canonical album name.

---

## Contributing

Pull requests are welcome. For major changes, please open an issue first.

Areas where help is appreciated:
- Spotify and other player support via SMTC
- UI improvements
- Translations

---

## License

[MIT](LICENSE) — free to use, modify, and distribute.
