import 'dart:io';
import 'package:fluent_ui/fluent_ui.dart';
import 'package:url_launcher/url_launcher.dart';
import '../services/app_settings.dart';
import '../services/e621_api_client.dart';
import '../services/storage_locations.dart';

class SettingsView extends StatefulWidget {
  const SettingsView({super.key});

  @override
  State<SettingsView> createState() => _SettingsViewState();
}

class _SettingsViewState extends State<SettingsView> {
  late final TextEditingController _usernameController;
  late final TextEditingController _apiKeyController;

  bool _isVerifying = false;
  String? _verifyMessage;
  bool _verifySuccess = false;

  @override
  void initState() {
    super.initState();
    final settings = AppSettings.instance;
    _usernameController = TextEditingController(text: settings.username);
    _apiKeyController = TextEditingController(text: settings.apiKey);
  }

  @override
  void dispose() {
    _usernameController.dispose();
    _apiKeyController.dispose();
    super.dispose();
  }

  Future<void> _verifyLogin() async {
    final user = _usernameController.text.trim();
    final key = _apiKeyController.text.trim();

    setState(() {
      _isVerifying = true;
      _verifyMessage = null;
    });

    final client = E621ApiClient();
    final result = await client.verifyLogin(user, key);

    if (mounted) {
      setState(() {
        _isVerifying = false;
        _verifySuccess = result.success;
        _verifyMessage = result.message;
      });

      if (result.success) {
        AppSettings.instance.username = user;
        AppSettings.instance.apiKey = key;
      }
    }
  }

  void _openDataFolder() {
    final path = StorageLocations.dataDirectory;
    if (Platform.isWindows) {
      Process.run('explorer.exe', [path]);
    } else {
      launchUrl(Uri.directory(path));
    }
  }

  @override
  Widget build(BuildContext context) {
    final settings = AppSettings.instance;
    final theme = FluentTheme.of(context);

    return ScaffoldPage.scrollable(
      padding: const EdgeInsets.symmetric(horizontal: 28, vertical: 24),
      header: const PageHeader(
        title: Text('Settings', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 26)),
      ),
      children: [
        // Appearance Section
        _buildSectionHeader('Appearance'),
        const SizedBox(height: 8),
        Card(
          child: Column(
            children: [
              _buildSettingRow(
                icon: FluentIcons.color_solid,
                title: 'App Theme',
                description: 'Select which app theme to display',
                control: ComboBox<String>(
                  value: settings.themeMode,
                  items: const [
                    ComboBoxItem(value: 'Default', child: Text('Use system setting')),
                    ComboBoxItem(value: 'Light', child: Text('Light')),
                    ComboBoxItem(value: 'Dark', child: Text('Dark')),
                  ],
                  onChanged: (val) {
                    if (val != null) {
                      setState(() => settings.themeMode = val);
                    }
                  },
                ),
              ),
              const Divider(),
              _buildSettingRow(
                icon: FluentIcons.photo2,
                title: 'Window Material',
                description: 'Choose between Mica and Desktop Acrylic backdrop',
                control: ComboBox<String>(
                  value: settings.backdropKind,
                  items: const [
                    ComboBoxItem(value: 'Mica', child: Text('Mica')),
                    ComboBoxItem(value: 'Acrylic', child: Text('Desktop Acrylic')),
                  ],
                  onChanged: (val) {
                    if (val != null) {
                      setState(() => settings.backdropKind = val);
                    }
                  },
                ),
              ),
              const Divider(),
              _buildSettingRow(
                icon: FluentIcons.brightness,
                title: 'Translucency',
                description: 'Adjust the level of window transparency',
                control: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    SizedBox(
                      width: 150,
                      child: Slider(
                        value: (settings.backdropOpacity * 100).roundToDouble(),
                        min: 10,
                        max: 100,
                        onChanged: (val) {
                          setState(() => settings.backdropOpacity = val / 100.0);
                        },
                      ),
                    ),
                    const SizedBox(width: 8),
                    Text('${(settings.backdropOpacity * 100).round()}%'),
                  ],
                ),
              ),
            ],
          ),
        ),

        const SizedBox(height: 24),

        // Search & API Section
        _buildSectionHeader('Search & API'),
        const SizedBox(height: 8),
        Card(
          child: _buildSettingRow(
            icon: FluentIcons.page_list,
            title: 'Images per page',
            description: 'Number of images to fetch per page',
            control: ComboBox<int>(
              value: settings.pageSize,
              items: const [
                ComboBoxItem(value: 24, child: Text('24 images')),
                ComboBoxItem(value: 40, child: Text('40 images')),
                ComboBoxItem(value: 75, child: Text('75 images')),
              ],
              onChanged: (val) {
                if (val != null) {
                  setState(() => settings.pageSize = val);
                }
              },
            ),
          ),
        ),

        const SizedBox(height: 24),

        // Authentication Section
        _buildSectionHeader('Account & API Credentials (Optional)'),
        const SizedBox(height: 4),
        Text(
          'Credentials are encrypted securely on disk using Windows DPAPI.',
          style: TextStyle(fontSize: 12, color: theme.resources.textFillColorSecondary),
        ),
        const SizedBox(height: 8),
        Card(
          child: Column(
            children: [
              _buildSettingRow(
                icon: FluentIcons.contact,
                title: 'Username',
                description: 'Your username on e621',
                control: SizedBox(
                  width: 220,
                  child: TextBox(
                    controller: _usernameController,
                    placeholder: 'Username',
                    onChanged: (val) => settings.username = val.trim(),
                  ),
                ),
              ),
              const Divider(),
              _buildSettingRow(
                icon: FluentIcons.password_field,
                title: 'API Key',
                description: 'Generated from your e621 account settings page',
                control: SizedBox(
                  width: 220,
                  child: PasswordBox(
                    controller: _apiKeyController,
                    placeholder: 'API Key',
                    onChanged: (val) => settings.apiKey = val.trim(),
                  ),
                ),
              ),
              const Divider(),
              _buildSettingRow(
                icon: FluentIcons.completed,
                title: 'Verify Credentials',
                description: 'Test your username and API key against the e621 API',
                control: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    if (_isVerifying) ...[
                      const ProgressRing(strokeWidth: 2),
                      const SizedBox(width: 8),
                    ],
                    Button(
                      onPressed: _isVerifying ? null : _verifyLogin,
                      child: const Text('Verify login'),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),

        if (_verifyMessage != null) ...[
          const SizedBox(height: 10),
          InfoBar(
            title: Text(_verifySuccess ? 'Login Verified' : 'Verification Failed'),
            content: Text(_verifyMessage!),
            severity: _verifySuccess ? InfoBarSeverity.success : InfoBarSeverity.error,
          ),
        ],

        const SizedBox(height: 24),

        // Data & Storage Section
        _buildSectionHeader('Data & Storage'),
        const SizedBox(height: 8),
        Card(
          child: _buildSettingRow(
            icon: FluentIcons.folder,
            title: 'Data Location',
            description: 'Local storage for cache, bookmarks, settings, and logs (~/.View621)',
            control: Button(
              onPressed: _openDataFolder,
              child: const Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(FluentIcons.open_folder_horizontal, size: 14),
                  SizedBox(width: 6),
                  Text('Open folder'),
                ],
              ),
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildSectionHeader(String title) {
    return Text(
      title,
      style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 18),
    );
  }

  Widget _buildSettingRow({
    required IconData icon,
    required String title,
    required String description,
    required Widget control,
  }) {
    final theme = FluentTheme.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        children: [
          Icon(icon, size: 20, color: theme.accentColor),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(title, style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 14)),
                const SizedBox(height: 2),
                Text(
                  description,
                  style: TextStyle(fontSize: 12, color: theme.resources.textFillColorSecondary),
                ),
              ],
            ),
          ),
          const SizedBox(width: 12),
          control,
        ],
      ),
    );
  }
}
