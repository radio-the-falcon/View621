# View621

**View621** is a native, unpackaged Windows 10 & 11 desktop client designed for browsing, searching, and managing content from **e621**. Built with **WinUI 3** and **.NET 10**, it provides a fluent, modern user experience utilising Windows Mica and Acrylic materials, multi-tab browsing, interactive panning and zooming, native media streaming, local offline caching, Windows DPAPI credential protection, and account integration for voting and favourites.

---

## Key Features

### 🗂️ Multi-Tab Browsing (`TabView`)
- **Full Tab Management**: Open multiple simultaneous searches and post details in the top tab bar.
- **New Tab Shortcuts**: Click the `+` button in the tab bar, middle-click any post thumbnail card, or right-click a card and choose **Open in new tab**.
- **Close & Restore**: Close tabs individually using the `×` button. Closing all tabs automatically opens a clean search tab.
- **State Preservation**: Tabs preserve active playback, stream buffering, navigation history, and pan/zoom positions when switching tabs.

### 🔍 Search & Tag Autocompletion
- **Keyboard-Friendly Tag Autocomplete**:
  - As you type tags in the search bar, suggestion popups appear dynamically with post counts.
  - Navigate suggestions smoothly using `Up` and `Down` arrow keys without prematurely overwriting your query text.
  - Press `Enter` to commit the selected suggestion, or `Escape` to dismiss the autocomplete dropdown.
  - Multi-tag queries separated by space delimiters.
- **Search History**: Sidebar dropdown displays recent queries with one-click reload and history clearing.
- **Saved Searches**: Bookmark favourite tag searches into a dedicated sidebar view with real-time badges and quick execution.
- **Integrated Sorting**: Built-in dropdown menu with clear direction indicators:
  - Default (Newest ID first) / Oldest ID first
  - Date created (Newest first) / Date created (Oldest first)
  - Score (Highest first) / Score (Lowest first)
  - Favourites (Most first) / Favourites (Fewest first)
  - Reverse order toggle (Ascending)
- **Quick Filter Chips**: One-click tags for frequent searches (`order:score`, `rating:s`, `scenery`, etc.).
- **Global Content Filters**:
  - **Gender filter**: Customise results via the sidebar (`All`, `Male` [-female], or `Female` [-male]).
  - **Rating filter**: Restrict content across searches (`Safe`, `Questionable`, `Explicit`).
- **Responsive Thumbnail Grid**:
  - Dynamic `UniformGridLayout` with `ItemsStretch="Fill"` evenly distributes cards across window sizes.
  - Thumbnail badges indicate format (`Video`, `GIF`, `Image`), parent/child variations, and comic series/pool memberships.

### 🖼️ Media Viewing, Playback & Offline Cache
- **Interactive Image Viewport**:
  - Smooth click-and-drag panning.
  - Bounded mouse wheel zoom centred at the cursor, clamped to prevent images shrinking smaller than the viewport.
  - Responsive sample view automatically resizing to fit the window during resize events.
  - Floating **Fit to view** / **Actual size** toggle for full-resolution view.
- **Native Video & GIF Playback**:
  - Native `MediaPlayerElement` playback for `webm` and `mp4` videos with custom transport controls docked at the bottom of the viewing pane.
  - Continuous background streaming and caching to disk while playing or paused.
  - Animated GIF rendering using `BitmapImage` with viewport-constrained sizing that maintains active frame loops.
  - Seamless tab switching: Pauses playback on switch, maintains playback position and buffer state, and restores responsive transport controls on return.
- **Offline Sample Image Cache**:
  - Automatically caches sample previews locally for seamless browsing.
  - If a post was previously viewed, it loads instantly from offline storage even without an active internet connection.
- **Navigation & Relations**:
  - Sequential `« Prev` and `Next »` post navigation with keyboard arrow shortcuts (`←` / `→`).
  - Related posts drawer displaying parent, children, and comic pool relationships with direct navigation.
  - Context menu options to copy raw image data or copy source links to clipboard.

### ⭐ Account Integration & Favourites
- **Windows DPAPI Credential Security**:
  - Account API keys are encrypted at rest using native Windows Data Protection API (`CryptProtectData`/`CryptUnprotectData` via `crypt32.dll`).
  - Plain-text credentials are never stored on disk.
- **In-App Login Verification**:
  - Direct credential test button in Settings with live validation against e621.
- **Live Favourites View**:
  - Dedicated **Favourites** sidebar view pulling your account's favourited posts in real time.
  - Live numerical badge counter displayed directly on the sidebar menu item.
  - One-click removal from favourites directly from thumbnail cards or context menus.
- **Post Actions Panel**:
  - Compact icon-only action buttons on `PostView` situated below post metadata:
    - **Upvote** (`+1`) with vote cancellation toggle.
    - **Downvote** (`-1`) with vote cancellation toggle.
    - **Favourite** / **Unfavourite** with immediate count and badge synchronisation.

### 📌 Pinned Posts (Bookmarks)
- **Local Collections**: Pin posts to local offline bookmarks for quick access.
- **Dedicated Pinned View**: Access all pinned posts from the sidebar navigation pane with live numerical count badges.

### ⚙️ Persistent Configuration & Data Storage
All application data is cleanly isolated in `~/.View621` (`C:\Users\<User>\.View621`):
- `settings.json`: Theme, materials, opacity, DPAPI encrypted credentials, and preferences.
- `bookmarks.json`: Locally pinned posts catalogue.
- `saved_searches.json`: Persisted saved search queries.
- `search_history.json`: Recent search queries.
- `cache/images/`: Local disk cache of viewed sample media and videos.
- `cache/posts.json`: Cached post metadata index.
- `app.log`: Diagnostics and error logs.

**Direct Folder Access**: The Settings page includes a dedicated **Open folder** button under **Data & Storage** to instantly launch `~/.View621` in Windows File Explorer.

---

## Technical Stack & Architecture

- **Framework**: Windows App SDK (WinUI 3)
- **Runtime**: .NET 10 (`net10.0-windows10.0.26100.0`)
- **Package Style**: Unpackaged application (`<WindowsPackageType>None</WindowsPackageType>`)
- **Single-File Architecture**: Self-contained standalone executable with native library self-extraction (`<PublishSingleFile>true</PublishSingleFile>`)
- **Styling**: Windows 11 Fluent Design System with Mica and Desktop Acrylic backdrops
- **Architectural Pattern**: MVVM using CommunityToolkit MVVM (`ObservableObject`, `RelayCommand`, service abstractions)
- **Security**: Native Windows DPAPI (`crypt32.dll`) P/Invoke

---

## Building and Running

### Prerequisites
1. **Windows 10 (version 1809+) or Windows 11**
2. **.NET 10 SDK** (or later)

### Development (Debug)
From PowerShell in the repository root:

```powershell
# Build the project
dotnet build -c Debug

# Run the application
dotnet run --no-build -c Debug
```

### Self-Contained Standalone Single-File Publish
To generate a single standalone `.exe` per architecture containing all runtimes and dependencies in a single file with zero external requirements:

```powershell
# For 64-bit Windows (outputs View621_x64.exe)
dotnet publish View621.csproj -c Release -r win-x64 -p:Platform=x64 -o ./publish/x64

# For ARM64 Windows (outputs View621_arm.exe)
dotnet publish View621.csproj -c Release -r win-arm64 -p:Platform=ARM64 -o ./publish/arm64
```

The compiled standalone executable files will be located at:
- `publish\x64\View621_x64.exe`
- `publish\arm64\View621_arm.exe`

---

## Project Structure

```
View621/
├── .github/workflows/           # CI/CD workflows for building and publishing x64/ARM64 releases
├── Assets/                      # Icons, logos, and splash screen graphics
├── Helpers/
│   ├── DpapiHelper.cs           # Native Windows DPAPI encryption/decryption (crypt32.dll)
│   └── WindowChromeHelper.cs    # Windows 11 title bar styling, backdrops, and theme synchronization
├── Models/
│   ├── E621Dto.cs               # Strongly-typed JSON DTOs for e621 API responses
│   ├── ImagePost.cs             # Post domain model with computed bindings and format detection
│   ├── PostNavigationContext.cs # Carries sequential navigation state across tabs
│   └── SavedSearchEntry.cs      # Saved search query persistence model
├── Properties/
│   ├── PublishProfiles/         # Profiles for win-x64 and win-arm64 single-file publishing
│   └── launchSettings.json      # Debug configuration
├── Services/
│   ├── AppSettings.cs           # Settings persistence manager with DPAPI encryption
│   ├── BookmarkService.cs       # Pinned posts catalogue manager
│   ├── E621ApiClient.cs         # Async REST client for posts, pools, tags, voting, and favourites
│   ├── FavoritesService.cs      # Real-time account favourites tracking and badge synchronization
│   ├── IImageService.cs         # Service contract for API interactions
│   ├── ISavedSearchesService.cs # Contract for saved search query operations
│   ├── ITabService.cs           # Multi-tab broker contract
│   ├── ImageCacheService.cs     # Disk caching for post media
│   ├── PostCacheService.cs      # Metadata caching for offline queries
│   ├── SavedSearchesService.cs  # Saved searches storage and management
│   ├── SearchCacheService.cs    # In-memory and persisted search result cache
│   ├── SearchHistoryService.cs  # Recent search terms persistence manager
│   ├── StorageLocations.cs      # Centralised paths, directory helpers, and logging (~/.View621)
│   ├── TabService.cs            # Coordinates tabs and navigation across views
│   └── ThemeService.cs          # Theme and window backdrop helper
├── ViewModels/
│   ├── PinViewModel.cs          # Pinned posts management and collection filtering
│   ├── PostViewModel.cs         # Post detail view model
│   ├── SavedSearchesViewModel.cs# Saved searches view model
│   └── SearchViewModel.cs       # Search view model managing queries, pagination, and tags
├── Views/
│   ├── AboutView.xaml(.cs)      # Application information, version, and links
│   ├── FavouritesView.xaml(.cs) # Live e621 account favourited posts grid
│   ├── PinView.xaml(.cs)        # Locally pinned posts viewer and manager
│   ├── PostView.xaml(.cs)       # Pan/zoom viewer, video playback, voting, tags, and relations
│   ├── SavedSearchesView.xaml(.cs) # Saved search queries management view
│   ├── SearchView.xaml(.cs)     # Main post grid, tag bar, sort selector, and filter chips
│   └── SettingsView.xaml(.cs)   # Theme, backdrop, DPAPI credentials, and data location launcher
├── MainWindow.xaml(.cs)         # Root window hosting TitleBar, TabView, and navigation pane
├── App.xaml(.cs)                # Application entry point, first-run initialization, and exception handlers
├── app.manifest                 # Windows application manifest (DPI awareness, OS compatibility)
└── View621.csproj               # Project and single-file build configuration
```

---

## License

This project is open-source under the MIT License.
