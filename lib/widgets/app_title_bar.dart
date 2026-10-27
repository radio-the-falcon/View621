import 'dart:io';
import 'package:fluent_ui/fluent_ui.dart';
import 'package:window_manager/window_manager.dart';

class AppTitleBar extends StatefulWidget {
  final VoidCallback? onTogglePane;

  const AppTitleBar({super.key, this.onTogglePane});

  @override
  State<AppTitleBar> createState() => _AppTitleBarState();
}

class _AppTitleBarState extends State<AppTitleBar> with WindowListener {
  bool _isMaximized = false;

  @override
  void initState() {
    super.initState();
    if (Platform.isWindows) {
      windowManager.addListener(this);
      _checkMaximized();
    }
  }

  @override
  void dispose() {
    if (Platform.isWindows) {
      windowManager.removeListener(this);
    }
    super.dispose();
  }

  Future<void> _checkMaximized() async {
    if (!Platform.isWindows) return;
    final max = await windowManager.isMaximized();
    if (mounted) {
      setState(() => _isMaximized = max);
    }
  }

  @override
  void onWindowMaximize() {
    setState(() => _isMaximized = true);
  }

  @override
  void onWindowUnmaximize() {
    setState(() => _isMaximized = false);
  }

  @override
  Widget build(BuildContext context) {
    final theme = FluentTheme.of(context);
    final isDark = theme.brightness == Brightness.dark;
    final titleBarColor = isDark ? const Color(0xFF202020) : const Color(0xFFF3F3F3);

    return Container(
      height: 40,
      color: titleBarColor,
      child: Row(
        children: [
          if (widget.onTogglePane != null)
            IconButton(
              icon: const Icon(FluentIcons.global_nav_button, size: 16),
              onPressed: widget.onTogglePane,
            ),
          const SizedBox(width: 8),
          Image.asset(
            'Assets/Square44x44Logo.targetsize-24_altform-unplated.png',
            width: 16,
            height: 16,
            errorBuilder: (_, _, _) => const Icon(FluentIcons.photo2, size: 16),
          ),
          const SizedBox(width: 10),
          const Text(
            'View621',
            style: TextStyle(fontSize: 12, fontWeight: FontWeight.w500),
          ),
          const Expanded(
            child: DragToMoveArea(
              child: SizedBox(height: double.infinity),
            ),
          ),
          if (Platform.isWindows) ...[
            _CaptionButton(
              icon: FluentIcons.chrome_minimize,
              onPressed: () => windowManager.minimize(),
            ),
            _CaptionButton(
              icon: _isMaximized ? FluentIcons.chrome_restore : FluentIcons.chrome_full_screen,
              onPressed: () async {
                if (_isMaximized) {
                  await windowManager.unmaximize();
                } else {
                  await windowManager.maximize();
                }
              },
            ),
            _CaptionButton(
              icon: FluentIcons.chrome_close,
              isClose: true,
              onPressed: () => windowManager.close(),
            ),
          ],
        ],
      ),
    );
  }
}

class _CaptionButton extends StatelessWidget {
  final IconData icon;
  final VoidCallback onPressed;
  final bool isClose;

  const _CaptionButton({
    required this.icon,
    required this.onPressed,
    this.isClose = false,
  });

  @override
  Widget build(BuildContext context) {
    return HoverButton(
      onPressed: onPressed,
      builder: (context, states) {
        Color bg = Colors.transparent;
        Color fg = FluentTheme.of(context).resources.textFillColorPrimary;

        if (states.contains(WidgetState.hovered)) {
          bg = isClose ? const Color(0xFFC42B1C) : FluentTheme.of(context).resources.subtleFillColorSecondary;
          if (isClose) fg = Colors.white;
        }

        return Container(
          width: 46,
          height: 40,
          color: bg,
          child: Center(
            child: Icon(icon, size: 10, color: fg),
          ),
        );
      },
    );
  }
}
