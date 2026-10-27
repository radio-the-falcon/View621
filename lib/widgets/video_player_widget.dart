import 'package:fluent_ui/fluent_ui.dart';
import 'package:media_kit/media_kit.dart';
import 'package:media_kit_video/media_kit_video.dart';

class VideoPlayerWidget extends StatefulWidget {
  final String videoUrl;

  const VideoPlayerWidget({super.key, required this.videoUrl});

  @override
  State<VideoPlayerWidget> createState() => _VideoPlayerWidgetState();
}

class _VideoPlayerWidgetState extends State<VideoPlayerWidget> {
  late final Player _player;
  late final VideoController _controller;
  bool _isPlaying = true;
  Duration _position = Duration.zero;
  Duration _duration = Duration.zero;

  @override
  void initState() {
    super.initState();
    _player = Player();
    _controller = VideoController(_player);

    _player.stream.playing.listen((playing) {
      if (mounted) setState(() => _isPlaying = playing);
    });

    _player.stream.position.listen((pos) {
      if (mounted) setState(() => _position = pos);
    });

    _player.stream.duration.listen((dur) {
      if (mounted) setState(() => _duration = dur);
    });

    _player.setPlaylistMode(PlaylistMode.loop);
    _player.open(Media(widget.videoUrl));
  }

  @override
  void didUpdateWidget(covariant VideoPlayerWidget oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.videoUrl != widget.videoUrl) {
      _player.open(Media(widget.videoUrl));
    }
  }

  @override
  void dispose() {
    _player.dispose();
    super.dispose();
  }

  String _formatDuration(Duration d) {
    final minutes = d.inMinutes.remainder(60).toString().padLeft(2, '0');
    final seconds = d.inSeconds.remainder(60).toString().padLeft(2, '0');
    return '$minutes:$seconds';
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      color: Colors.black,
      child: Stack(
        fit: StackFit.expand,
        children: [
          Center(
            child: Video(
              controller: _controller,
              controls: NoVideoControls,
            ),
          ),

          // Docked Transport Controls at bottom
          Positioned(
            left: 0,
            right: 0,
            bottom: 0,
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  begin: Alignment.bottomCenter,
                  end: Alignment.topCenter,
                  colors: [
                    Colors.black.withValues(alpha: 0.8),
                    Colors.black.withValues(alpha: 0.0),
                  ],
                ),
              ),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  // Progress Slider
                  Slider(
                    value: _duration.inMilliseconds > 0
                        ? (_position.inMilliseconds / _duration.inMilliseconds).clamp(0.0, 1.0) * 100.0
                        : 0.0,
                    min: 0.0,
                    max: 100.0,
                    onChanged: (val) {
                      final targetMs = (val / 100.0 * _duration.inMilliseconds).round();
                      _player.seek(Duration(milliseconds: targetMs));
                    },
                  ),
                  Row(
                    children: [
                      IconButton(
                        icon: Icon(
                          _isPlaying ? FluentIcons.pause : FluentIcons.play,
                          size: 16,
                          color: Colors.white,
                        ),
                        onPressed: () {
                          _player.playOrPause();
                        },
                      ),
                      const SizedBox(width: 8),
                      Text(
                        '${_formatDuration(_position)} / ${_formatDuration(_duration)}',
                        style: const TextStyle(color: Colors.white, fontSize: 12),
                      ),
                      const Spacer(),
                      IconButton(
                        icon: const Icon(FluentIcons.repeat_all, size: 14, color: Colors.white),
                        onPressed: () {
                          _player.seek(Duration.zero);
                          _player.play();
                        },
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
