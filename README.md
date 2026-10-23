# View621

**View621** is a modern, cross-platform desktop client designed for browsing, searching, and viewing content from **e621** and **e926**. Built with **Uno Platform**, **WinUI 3**, and **.NET 10**, it provides a consistent, responsive user experience across **Windows 10 & 11**, **Linux (Desktop Skia)**, and **macOS (Mac Catalyst)**. Features include multi-tab browsing, interactive panning and zooming, media streaming, local offline caching, and bookmarks management.

---

## Key Features

### 🗂️ Multi-Tab Browsing (`TabView`)
- **Full Tab Management**: Open multiple simultaneous searches, bookmark collections, and post details in the top tab bar.
- **New Tab Shortcuts**: Click the `+` button in the tab bar, middle-click any post thumbnail card, or right-click a card and choose **Open in new tab**.
- **Close & Restore**: Close tabs individually using the `×` button. Closing all tabs automatically opens a clean search tab.
- **State Preservation**: Tabs preserve active playback, stream buffering, navigation history, and pan/zoom positions when switching tabs.

### 🔍 Search & Tag Autocompletion
- **Keyboard-Friendly Tag Autocomplete**:
  - Dynamic suggestion popups appear as you type tags in the search bar, complete with post counts.
  - Navigate suggestions smoothly using `Up` and `Down` arrow keys without prematurely overwriting query text.
  - Press `Enter` to commit the selected suggestion, or `Escape` to dismiss the autocomplete dropdown.
  - Space-delimited multi-tag queries.
- **Search History & Saved Searches**: Dedicated views for recent search queries and pinned searches with one-click reload.
- **Integrated Sorting**: Built-in dropdown menu with clear direction indicators:
  - Default (Newest ID first) / Oldest ID first
  - Date created (Newest first) / Date created (Oldest first)
  - Score (Highest first) / Score (Lowest first)
  - Favourites (Most first) / Favourites (Fewest first)
  - Reverse order toggle (Ascending)
- **Quick Filter Chips**: One-click tags for frequent searches (`order:score`, `rating:s`, `scenery`, etc.).
- **Global Content Filters**:
  - **Gender filter**: Customise results via the sidebar (`All`, `Male` [-female], or `Female` [-male]).
  - **Rating filter**: Restrict content across searches (`General`, `Sensitive`, `Questionable`, `Explicit`).
- **Responsive Thumbnail Grid**:
  - Dynamic `UniformGridLayout` with `ItemsStretch="Fill"` evenly distributes cards across window sizes.
  - Thumbnail badges indicate format (`Video`, `GIF`, `Image`), parent/child relationships, and pool memberships.

### 🖼️ Media Viewing, Playback & Offline Cache
- **Interactive Image Viewport**:
  - Smooth click-and-drag panning.
  - Bounded mouse wheel zoom centred at the cursor, clamped to prevent images shrinking smaller than the viewport.
  - Responsive sample view automatically resizing to fit the window during resize events.
  - Floating **Fit to view** / **Actual size** toggle for full-resolution view.
- **Native Video & GIF Playback**:
  - `MediaPlayerElement` playback for `webm` and `mp4` videos with custom transport controls docked at the bottom of the viewing pane.
  - Continuous background streaming and caching to disk while playing or paused.
  - Animated GIF rendering using `BitmapImage` with viewport-constrained sizing that maintains active frame loops.
  - Seamless tab switching: Pauses playback on switch, maintains playback position and buffer state, and restores responsive transport controls on return.
- **Offline Sample Image Cache**:
  - Automatically caches sample previews locally for seamless browsing.
  - If a post was previously viewed, it loads instantly from offline storage even without an active internet connection.
- **Navigation & Context Menu**:
  - Sequential `« Prev` and `Next »` post navigation with keyboard arrow shortcuts (`←` / `→`).
  - Context menu options to copy raw image data or copy source links to clipboard.

### 🔖 Bookmarks & Favourites
- **Local Collections**: Save posts to local offline bookmarks with custom tagging and notes.
- **Dedicated Bookmarks View**: Access all bookmarked items directly from the sidebar navigation pane.

### ⚙️ Persistent Configuration & Data Storage
All application data is cleanly isolated in `~/.view621` (`C:\Users\<User>\.view621` on Windows, `~/.view621` on Linux/macOS):
- `settings.json`: Theme, materials, opacity, and interface configuration.
- `bookmarks.json`: Saved post bookmarks.
- `search_history.json`: Recent search queries.
- `saved_searches.json`: Pinned and favourite search queries.
- `cache/images/`: Local disk cache of viewed sample media and videos.
- `cache/posts.json`: Cached post metadata index.
- `app.log`: Diagnostics and error logs.

**Direct Folder Access**: The Settings page includes a dedicated **Open folder** button under **Data & Storage** to instantly open `~/.view621` in your operating system's native file manager (`explorer`, `xdg-open`, or `open`).

---

## Technical Stack & Architecture

- **UI Framework**: [Uno Platform](https://platform.uno/) with WinUI 3 (Windows App SDK on Windows, Skia Desktop on Linux, and Mac Catalyst on macOS)
- **Runtime**: .NET 10
  - Windows: `net10.0-windows10.0.26100.0` (x64, ARM64)
  - Linux & macOS: `net10.0-desktop` (Skia Desktop for Linux x64 and macOS ARM64 / x64)
- **Architectural Pattern**: MVVM using CommunityToolkit MVVM (`ObservableObject`, `RelayCommand`, service abstractions)
- **Styling**: Windows 11 Fluent Design System with Mica/Acrylic backdrops on Windows, and Uno Cupertino Apple-inspired design styling
- **API Client**: Anonymous e621 / e926 REST API integration with automated pagination and search suggestion debouncing

---

## Building and Running

### Prerequisites
- **.NET 10 SDK** (or later)

### Windows (Debug)
From PowerShell in the repository root:

```powershell
# Build for Windows App SDK
dotnet build E6Viewer.csproj -f net10.0-windows10.0.26100.0

# Run the application
dotnet run --project E6Viewer.csproj -f net10.0-windows10.0.26100.0
```

### Self-Contained Release Builds

#### Windows (x64 and ARM64)
```powershell
# Windows x64 (generates View621_x64.exe)
dotnet publish E6Viewer.csproj -c Release -r win-x64 -f net10.0-windows10.0.26100.0 --self-contained -o ./artifacts/windows-x64

# Windows ARM64 (generates View621_arm.exe)
dotnet publish E6Viewer.csproj -c Release -r win-arm64 -f net10.0-windows10.0.26100.0 --self-contained -o ./artifacts/windows-ARM64
```

#### Linux (Desktop Skia x64)
```bash
# Build and publish Linux self-contained executable
dotnet publish E6Viewer.csproj -c Release -r linux-x64 -f net10.0-desktop --self-contained -o ./artifacts/linux-x64
```

#### macOS (Desktop Skia ARM64 & x64)
```bash
# Build and publish macOS Apple Silicon (ARM64)
dotnet publish E6Viewer.csproj -c Release -r osx-arm64 -f net10.0-desktop --self-contained -o ./artifacts/macos-arm64

# Build and publish macOS Intel (x64)
dotnet publish E6Viewer.csproj -c Release -r osx-x64 -f net10.0-desktop --self-contained -o ./artifacts/macos-x64
```

---

## Project Structure

```
e6_viewer/
├── Assets/                      # Icons, logos, and splash screen graphics
├── Helpers/
│   ├── CrossPlatformLauncher.cs # Shell launcher abstraction (explorer, xdg-open, open)
│   └── WindowChromeHelper.cs    # Windows 11 title bar and backdrop styling
├── Models/
│   ├── E621Dto.cs               # Strongly-typed JSON DTOs for e621/e926 API responses
│   ├── ImagePost.cs             # Post domain model with computed bindings and format detection
│   ├── PostNavigationContext.cs # Carries sequential navigation state across tabs
│   └── SavedSearchEntry.cs      # Pinned search query model
├── Platforms/
│   └── Desktop/
│       └── Program.cs           # Skia Desktop entry point (Linux / macOS / X11)
├── Services/
│   ├── AppSettings.cs           # Settings persistence manager (~/.view621/settings.json)
│   ├── BookmarkService.cs       # Bookmark storage and cached post catalogue manager
│   ├── E621ApiClient.cs         # Async REST client for search and autocompletion
│   ├── ImageCacheService.cs     # Disk caching for post previews and media
│   ├── PostCacheService.cs      # Metadata caching for browsed posts
│   ├── SavedSearchesService.cs  # Pinned query storage
│   ├── SearchHistoryService.cs  # Query history management
│   ├── StorageLocations.cs      # Centralised paths, directory helpers, and migration (~/.view621)
│   ├── TabService.cs            # Coordinates tabs and navigation across views
│   └── ThemeService.cs          # Theme and window backdrop helper
├── ViewModels/
│   └── SearchViewModel.cs       # Search view model managing queries, pagination, and tags
├── Views/
│   ├── AboutView.xaml(.cs)      # Application information, license, and brand cards
│   ├── PinView.xaml(.cs)        # Pinned search quick navigation view
│   ├── PostView.xaml(.cs)       # Pan/zoom viewer, video playback, tag chips, and details sidebar
│   ├── SavedSearchesView.xaml(.cs)# Saved and favourite search manager
│   ├── SearchView.xaml(.cs)     # Main post thumbnail grid, tag bar, and filter chips
│   └── SettingsView.xaml(.cs)   # Theme, backdrop, cache management, and storage launcher
├── MainWindow.xaml(.cs)         # Root window hosting TitleBar, TabView, and navigation pane
├── App.xaml(.cs)                # Application entry point, first-run initialisation, and exception handlers
├── Directory.Build.props        # Global cross-platform build properties
├── global.json                  # Uno.Sdk version pinning
├── app.manifest                 # Windows application manifest (DPI awareness, OS compatibility)
└── E6Viewer.csproj              # Cross-platform multi-targeting project configuration
```

---

## License

This project is open-source under the MIT License.
