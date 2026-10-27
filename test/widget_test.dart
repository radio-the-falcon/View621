import 'package:fluent_ui/fluent_ui.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:view621/models/image_post.dart';
import 'package:view621/services/storage_locations.dart';
import 'package:view621/viewmodels/main_viewmodel.dart';
import 'package:view621/viewmodels/search_viewmodel.dart';
import 'package:view621/views/main_view.dart';

void main() {
  test('ImagePost model serialization and format detection', () {
    final post = ImagePost(
      id: 12345,
      title: 'Test Post',
      artist: 'FoxArtist',
      fileExtension: 'webm',
      score: 42,
      favCount: 10,
    );

    expect(post.id, equals(12345));
    expect(post.isVideo, isTrue);
    expect(post.isAnimatedGif, isFalse);
    expect(post.mediaTypeLabel, equals('Video'));

    final json = post.toJson();
    final restored = ImagePost.fromJson(json);

    expect(restored.id, equals(12345));
    expect(restored.title, equals('Test Post'));
    expect(restored.artist, equals('FoxArtist'));
    expect(restored.isVideo, isTrue);
  });

  test('StorageLocations data paths', () {
    expect(StorageLocations.dataDirectory, contains('.View621'));
    expect(StorageLocations.bookmarksFilePath, endsWith('bookmarks.json'));
    expect(StorageLocations.settingsFilePath, endsWith('settings.json'));
    expect(StorageLocations.searchHistoryFilePath, endsWith('search_history.json'));
  });

  testWidgets('MainView builds without assertion errors', (tester) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(
      FluentApp(
        home: ChangeNotifierProvider<MainViewModel>(
          create: (_) => MainViewModel(),
          child: const MainView(),
        ),
      ),
    );

    expect(find.byType(MainView), findsOneWidget);
    expect(find.byType(NavigationView), findsOneWidget);
  });

  test('SearchViewModel disposal safety during pending async operations', () async {
    final vm = SearchViewModel(initialQuery: 'canine');
    // Dispose immediately while async search is in flight
    vm.dispose();
    expect(vm.isDisposed, isTrue);
    // Calling notifyListeners when disposed should be a safe no-op
    expect(() => vm.notifyListeners(), returnsNormally);
  });
}
