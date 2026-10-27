import 'package:fluent_ui/fluent_ui.dart';

class AboutView extends StatelessWidget {
  const AboutView({super.key});

  @override
  Widget build(BuildContext context) {
    final theme = FluentTheme.of(context);

    return ScaffoldPage.scrollable(
      padding: const EdgeInsets.symmetric(horizontal: 28, vertical: 24),
      header: const PageHeader(
        title: Text('About View621', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 26)),
      ),
      children: [
        // Brand Card
        Card(
          padding: const EdgeInsets.all(20),
          child: Row(
            children: [
              Image.asset(
                'Assets/Square44x44Logo.targetsize-48_altform-lightunplated.png',
                width: 56,
                height: 56,
                errorBuilder: (_, _, _) => const Icon(FluentIcons.photo2, size: 48),
              ),
              const SizedBox(width: 18),
              Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text('View621', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 24)),
                  const SizedBox(height: 2),
                  Text(
                    'An e621.net desktop client built with Flutter & Fluent UI',
                    style: TextStyle(fontSize: 14, color: theme.resources.textFillColorSecondary),
                  ),
                ],
              ),
            ],
          ),
        ),

        const SizedBox(height: 20),

        Text(
          'How to Use View621',
          style: TextStyle(fontWeight: FontWeight.bold, fontSize: 18, color: theme.resources.textFillColorPrimary),
        ),

        const SizedBox(height: 12),

        // Multi-Tab Navigation Card
        _buildGuideCard(
          context,
          icon: FluentIcons.tab_center,
          title: 'Browsing & Multi-Tab Navigation',
          bullets: [
            'Click the + button on the tab bar to open a clean, independent search tab.',
            'Close tabs at any time using the close button on each tab or middle-clicking the tab header.',
            'Each search tab operates with its own distinct search memory and query state.',
          ],
        ),

        const SizedBox(height: 12),

        // Search & Filters Card
        _buildGuideCard(
          context,
          icon: FluentIcons.search,
          title: 'Searching & Filtering',
          bullets: [
            'Type tags in the search bar (e.g. canine scenery) to see real-time autocomplete suggestions.',
            'Click the Search button or press Enter to query e621.net.',
            'Use the Rating filter in the sidebar to view Safe (rating:s), Questionable (safe & questionable), or Explicit (all ratings).',
            'Use the Show gender filter to isolate Male, Female, or All.',
            'Access your recent searches from the resizable Previous searches list in the sidebar. Drag the top bar to adjust height.',
          ],
        ),

        const SizedBox(height: 12),

        // Post View Card
        _buildGuideCard(
          context,
          icon: FluentIcons.photo2,
          title: 'Post View & Media Inspection',
          bullets: [
            'Click any thumbnail in search results to open its detailed post view.',
            'Navigate between posts instantly using the Prev / Next buttons or the Left and Right keyboard arrow keys.',
            'Click Switch to full view to view the uncompressed original resolution, or switch back to sample view.',
            'Zoom and pan with mouse drag and scroll wheel, or toggle Fit to view / Actual size.',
            'Native video playback with looping and custom transport controls.',
          ],
        ),

        const SizedBox(height: 12),

        // Right-Click Reference Card
        _buildGuideCard(
          context,
          icon: FluentIcons.context_menu,
          title: 'Right-Click Quick Reference',
          bullets: [
            'Search Results Card: Open in new tab, Copy image link, Pin post.',
            'Post View Tag Chips: Click to search tag in new tab.',
            'Image Viewport: Copy URL to clipboard.',
            'Middle Click on post cards: Opens the post directly in a new tab.',
          ],
        ),
      ],
    );
  }

  Widget _buildGuideCard(
    BuildContext context, {
    required IconData icon,
    required String title,
    required List<String> bullets,
  }) {
    final theme = FluentTheme.of(context);

    return Card(
      padding: const EdgeInsets.all(18),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(icon, size: 16, color: theme.accentColor),
              const SizedBox(width: 8),
              Text(title, style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 15)),
            ],
          ),
          const SizedBox(height: 8),
          for (final bullet in bullets)
            Padding(
              padding: const EdgeInsets.only(bottom: 4),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('• ', style: TextStyle(color: theme.resources.textFillColorSecondary)),
                  Expanded(
                    child: Text(
                      bullet,
                      style: TextStyle(fontSize: 13, color: theme.resources.textFillColorSecondary),
                    ),
                  ),
                ],
              ),
            ),
        ],
      ),
    );
  }
}
