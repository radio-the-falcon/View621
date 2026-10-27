# View621

**View621** is a native Windows desktop client designed for browsing, searching, and managing content from **e621**. Built with **Flutter** and **fluent_ui**, it faithfully replicates the Windows 11 WinUI look and feel, providing a fluent modern experience utilising Windows Mica and Acrylic materials, multi-tab browsing, interactive panning and zooming, hardware-accelerated media streaming, local offline caching, Windows DPAPI credential protection, and account integration for voting and favourites.

---

## Key Features

### 🗂️ Multi-Tab Browsing (`TabView`)
- **Full Tab Management**: Open multiple simultaneous searches and post details in the top tab bar.
- **New Tab Shortcuts**: Click the `+` button in the tab bar, middle-click any post thumbnail card, or right-click a card and choose **Open in new tab**.
- **Close & Restore**: Close tabs individually using the `×` button. Closing all tabs automatically opens a clean search tab.
- **State Preservation**: Tabs preserve active playback, navigation history, and pan/zoom positions when switching tabs.

### 🔍 Search & Tag Autocompletion
- **Keyboard-Friendly Tag Autocomplete**:
  - As you type tags in the search bar, suggestion popups appear dynamically with post counts.
  - Multi-tag queries separated by space delimiters.
- **Search History**: Sidebar list displays recent queries with one-click reload and history clearing, complete with a draggable height resize handle.
- **Saved Searches**: Bookmark favourite tag searches into a dedicated sidebar view with real-time badges, post previews, and quick execution.
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
  - Dynamic responsive grid evenly distributes cards across window sizes.
  - Thumbnail badges indicate format (`Video`, `GIF`, `Image`), parent/child variations, and comic series/pool memberships.

### 🖼️ Media Viewing, Playback & Offline Cache
- **Interactive Image Viewport**:
  - Smooth click-and-drag panning.
  - Bounded mouse wheel zoom centred at the cursor, clamped to prevent images shrinking smaller than the viewport.
  - Responsive sample view automatically resizing to fit the window during resize events.
  - Floating **Fit to view** / **Actual size** toggle for full-resolution view.
- **Native Video & GIF Playback**:
  - High-performance `media_kit` video playback for `webm` and `mp4` videos with custom transport controls docked at the bottom of the viewing pane.
  - Looping enabled by default for short video loops and webms.
  - Animated GIF rendering with viewport-constrained sizing that maintains active frame loops.
- **Offline Sample Image Cache**:
  - Automatically caches sample previews locally in `~/.View621/cache/images` for seamless browsing.
  - If a post was previously viewed, it loads instantly from offline storage even without an active internet connection.
- **Navigation & Relations**:
  - Sequential `« Prev` and `Next »` post navigation with keyboard arrow shortcuts (`←` / `→`).
  - Related posts drawer displaying parent, children, and comic pool relationships with direct navigation.
  - Context menu options to copy raw image data or copy source links to clipboard.

### ⭐ Account Integration & Favourites
- **Windows DPAPI Credential Security**:
  - Account API keys are encrypted at rest using native Windows Data Protection API (`CryptProtectData`/`CryptUnprotectData` via `crypt32.dll` and `dart:ffi`).
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

- **Framework**: Flutter Desktop for Windows
- **Design System**: `fluent_ui` (reproducing the Windows 11 WinUI 3 experience)
- **Window Management**: `window_manager`
- **Media Engine**: `media_kit` & `media_kit_video`
- **Architectural Pattern**: MVVM using `ChangeNotifier` and `Provider`
- **Security**: Native Windows DPAPI (`crypt32.dll`) via `dart:ffi`

---

## Building and Running

### Prerequisites
1. **Windows 10 (version 1809+) or Windows 11**
2. **Flutter SDK** (3.47+ recommended)
3. **Visual Studio 2022 / 2026** with C++ desktop development workload

### Development (Debug)
From PowerShell in the repository root:

```powershell
# Get dependencies
flutter pub get

# Run the application
flutter run -d windows
```

### Release Build
To generate a standalone release build:

```powershell
flutter build windows --release
```

The compiled release executable and binaries will be located at:
- `build\windows\x64\runner\Release\`

---

## Project Structure

```
e6_viewer/
├── Assets/                      # Icons, logos, and splash screen graphics
├── lib/
│   ├── helpers/
│   │   └── dpapi_helper.dart    # Native Windows DPAPI encryption via dart:ffi
│   ├── models/
│   │   ├── e621_dto.dart        # Strongly-typed JSON DTOs for e621 API responses
│   │   ├── image_post.dart      # Post domain model with computed bindings
│   │   ├── post_navigation_context.dart # Navigation context across tabs
│   │   └── saved_search_entry.dart # Saved search persistence model
│   ├── services/
│   │   ├── app_settings.dart    # Settings manager with DPAPI encryption
│   │   ├── bookmark_service.dart# Pinned posts manager
│   │   ├── e621_api_client.dart # Async REST client for posts, pools, tags, and votes
│   │   ├── favorites_service.dart# Account favourites badge and synchronisation
│   │   ├── image_cache_service.dart # Local disk caching for post media
│   │   ├── post_cache_service.dart  # Metadata caching for offline queries
│   │   ├── saved_searches_service.dart # Saved searches storage
│   │   ├── search_history_service.dart # Search history manager
│   │   ├── storage_locations.dart # Centralised ~/.View621 directories and logging
│   │   └── tab_service.dart     # Multi-tab broker and navigation controller
│   ├── viewmodels/
│   │   ├── favourites_viewmodel.dart # Favourites view model
│   │   ├── main_viewmodel.dart  # Root navigation, tabs, and sidebar badges
│   │   ├── pin_viewmodel.dart   # Pinned posts view model
│   │   ├── post_viewmodel.dart  # Post detail view model
│   │   ├── saved_searches_viewmodel.dart # Saved searches view model
│   │   └── search_viewmodel.dart# Search view model managing queries and filters
│   ├── views/
│   │   ├── about_view.dart      # About page with user manual and guide
│   │   ├── favourites_view.dart # Account favourites grid view
│   │   ├── main_view.dart       # NavigationView, titlebar, TabView, and sidebar
│   │   ├── pin_view.dart        # Locally pinned posts gallery view
│   │   ├── post_view.dart       # Pan/zoom viewer, video playback, and metadata
│   │   ├── saved_searches_view.dart # Saved searches manager view
│   │   └── settings_view.dart   # Preferences, credentials, and data location
│   ├── widgets/
│   │   ├── app_title_bar.dart   # Windows caption controls and drag region
│   │   ├── post_card.dart       # Post card with preview, badges, and context menu
│   │   └── video_player_widget.dart # Hardware-accelerated media player
│   └── main.dart                # Application entry point and Fluent theme setup
├── test/
│   ├── dpapi_test.dart          # Windows DPAPI encryption/decryption verification
│   └── widget_test.dart         # Unit tests for models and storage locations
├── windows/                     # Native Windows C++ desktop runner and CMake build
├── pubspec.yaml                 # Flutter package specification and dependencies
└── README.md                    # Documentation
```

---

## Licence

This project is open-source under the MIT Licence.
