# View621

**View621** is a native, unpackaged Windows 10 & 11 desktop client designed for browsing, searching, and viewing content from **e621** and **e926**. Built with **WinUI 3** and **.NET 10**, it provides a fluent, modern user experience utilising Windows Mica and Acrylic materials, multi-tab browsing, interactive panning and zooming, native media streaming, local offline caching, and bookmarks management.

---

## Key Features

### 🗂️ Multi-Tab Browsing (`TabView`)
- **Full Tab Management**: Open multiple simultaneous searches, bookmark collections, and post details in the top tab bar.
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
- **Navigation & Context Menu**:
  - Sequential `« Prev` and `Next »` post navigation with keyboard arrow shortcuts (`←` / `→`).
  - Context menu options to copy raw image data or copy source links to clipboard.

### 🔖 Bookmarks & Favourites
- **Local Collections**: Save posts to local offline bookmarks with custom tagging and notes.
- **Dedicated Bookmarks View**: Access all bookmarked items directly from the sidebar navigation pane.

### ⚙️ Persistent Configuration & Data Storage
All application data is cleanly isolated in `~/.View621` (`C:\Users\<User>\.View621`):
- `settings.json`: Theme, materials, opacity, credentials, and API configuration.
- `bookmarks.json`: Saved post bookmarks.
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
- **Single-File Architecture**: Self-contained with embedded MSIX tooling and resource indexation (`<EnableMsixTooling>true</EnableMsixTooling>`)
- **Styling**: Windows 11 Fluent Design System with Mica and Desktop Acrylic backdrops
- **Architectural Pattern**: MVVM using CommunityToolkit MVVM (`ObservableObject`, `RelayCommand`, service abstractions)

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

### Self-Contained Single-File Release
To generate a standalone `.exe` that runs anywhere without requiring external dependencies, pre-installed runtimes, or neighbouring DLLs:

```powershell
# For x64 / x86 (generates View621_x86.exe)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true

# For ARM64 (generates View621_arm.exe)
dotnet publish -c Release -r win-arm64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

Output binaries:
- `bin\Release\net10.0-windows10.0.26100.0\win-x64\publish\View621_x86.exe`
- `bin\Release\net10.0-windows10.0.26100.0\win-arm64\publish\View621_arm.exe`

---

## Project Structure

```
View621/
├── Assets/                      # Icons, logos, and splash screen graphics
├── Models/
│   ├── E621Dto.cs               # Strongly-typed JSON DTOs for e621/e926 API responses
│   ├── ImagePost.cs             # Post domain model with computed bindings and format detection
│   └── PostNavigationContext.cs # Carries sequential navigation state across tabs
├── Pages/
│   ├── SearchPage.xaml(.cs)     # Main post grid, tag bar, sort selector, and filter chips
│   ├── ImageDetailPage.xaml(.cs)# Pan/zoom viewer, video playback, tag chips, and metadata sidebar
│   ├── BookmarksPage.xaml(.cs)  # Saved bookmark management and offline viewer
│   ├── SettingsPage.xaml(.cs)   # Theme, backdrop, API credentials, and data location launcher
│   └── AboutPage.xaml           # Application information and build details
├── Services/
│   ├── AppSettings.cs           # Settings persistence manager (~/.View621/settings.json)
│   ├── BookmarkService.cs       # Bookmark storage and cached post catalogue manager
│   ├── E621ApiClient.cs         # Async REST client for search and autocompletion
│   ├── IImageService.cs         # Service contract for API interactions
│   ├── ITabService.cs           # Multi-tab broker contract
│   ├── StorageLocations.cs      # Centralised paths, directory helpers, and logging (~/.View621)
│   ├── TabService.cs            # Coordinates tabs and navigation across views
│   └── ThemeService.cs          # Theme and window backdrop helper
├── ViewModels/
│   └── SearchViewModel.cs       # Search view model managing queries, pagination, and tags
├── MainWindow.xaml(.cs)         # Root window hosting TitleBar, TabView, and navigation pane
├── App.xaml(.cs)                # Application entry point, first-run initialization, and exception handlers
├── app.manifest                 # Windows application manifest (DPI awareness, OS compatibility)
└── View621.csproj               # Project and single-file build configuration
```

---

## License

This project is open-source under the MIT License.
