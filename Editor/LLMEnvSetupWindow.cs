using System.IO;
using System.Text;
using BCS.LLM.Core.Env;
using UnityEditor;
using UnityEngine;

namespace BCS.LLM.Core.Editor
{
    /// <summary>
    /// v0: Environment setup helper.
    /// - Detects LLMEnvSettings asset in Resources.
    /// - Detects .env file presence.
    /// - Guides creation of LLMEnvSettings and writing a minimal OpenAI .env file (API key only).
    /// - Reloads LLMEnvLoader after changes.
    /// </summary>
    public sealed class LLMEnvSetupWindow : EditorWindow
    {
        // Minimal OpenAI key for v0
        private const string KeyOpenAIApiKey = "OPENAI_API_KEY";

        // Where we’ll create the settings asset (must be under Resources to be auto-loaded by LLMEnvLoader)
        private const string ResourcesFolderPath = "Assets/Resources";
        private const string SettingsAssetPath = "Assets/Resources/LLMEnvSettings.asset";

        private Vector2 _scroll;

        // UI state
        private string _envFilePath = LLMEnvLoader.DefaultEnvRelativePath; // default ".env"
        private bool _autoLoadOnStartup = true;
        private bool _allowOsEnvFallback = true;

        private string _openAIApiKey = "";

        // Status cache
        private LLMEnvSettings _settingsAsset;
        private string _resolvedEnvPath;
        private bool _envFileExists;
        private bool _hasApiKey;

        [MenuItem("Tools/LLM/Env Setup")]
        public static void Open()
        {
            var w = GetWindow<LLMEnvSetupWindow>("LLM Env Setup");
            w.minSize = new Vector2(520, 420);
            w.RefreshStatus();
        }

        private void OnEnable() => RefreshStatus();
        private void OnFocus() => RefreshStatus();

        private void RefreshStatus()
        {
            // Load settings asset the same way the loader does (Resources)
            _settingsAsset = Resources.Load<LLMEnvSettings>(LLMEnvLoader.SettingsResourceName);

            // Prefer settings envFilePath if present
            if (_settingsAsset != null && !string.IsNullOrWhiteSpace(_settingsAsset.envFilePath))
            {
                _envFilePath = _settingsAsset.envFilePath;
                _autoLoadOnStartup = _settingsAsset.autoLoadOnStartup;
                _allowOsEnvFallback = _settingsAsset.allowOsEnvFallback;
            }

            _resolvedEnvPath = LLMEnvLoader.ResolvePath(_envFilePath);
            _envFileExists = !string.IsNullOrWhiteSpace(_resolvedEnvPath) && File.Exists(_resolvedEnvPath);

            // Ask loader whether OPENAI_API_KEY is currently available (env file and/or OS env)
            _hasApiKey = LLMEnvLoader.HasNonEmpty(KeyOpenAIApiKey);

            Repaint();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawHeader();
            EditorGUILayout.Space(8);

            DrawStatusPanel();
            EditorGUILayout.Space(8);

            DrawSettingsPanel();
            EditorGUILayout.Space(8);

            DrawEnvFilePanel();
            EditorGUILayout.Space(8);

            DrawWriteEnvPanel();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("LLM Environment Setup (v0)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This window helps you configure env loading for the LLM package.\n\n" +
                "Recommended: keep a .env file at project root (\".env\") and store only OPENAI_API_KEY.\n" +
                "Base URL and endpoints should be handled by defaults/config (not entered here).",
                MessageType.Info);
        }

        private void DrawStatusPanel()
        {
            EditorGUILayout.LabelField("Current Status", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Settings asset (Resources):",
                    _settingsAsset != null ? "FOUND" : "MISSING");

                EditorGUILayout.LabelField("Env path (configured):", _envFilePath);
                EditorGUILayout.LabelField("Env path (resolved):",
                    string.IsNullOrWhiteSpace(_resolvedEnvPath) ? "(invalid)" : _resolvedEnvPath);

                EditorGUILayout.LabelField(".env file exists:", _envFileExists ? "YES" : "NO");
                EditorGUILayout.LabelField("OPENAI_API_KEY available:", _hasApiKey ? "YES" : "NO");

                EditorGUILayout.Space(4);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Reload Env Loader"))
                    {
                        LLMEnvLoader.Reload();
                        RefreshStatus();
                    }

                    if (GUILayout.Button("Re-scan"))
                    {
                        RefreshStatus();
                    }
                }
            }
        }

        private void DrawSettingsPanel()
        {
            EditorGUILayout.LabelField("LLMEnvSettings (optional)", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.HelpBox(
                    "LLMEnvSettings is optional. If present in Assets/Resources/LLMEnvSettings.asset and auto-load is enabled, " +
                    "the loader will use its envFilePath automatically.",
                    MessageType.None);

                _envFilePath = EditorGUILayout.TextField(new GUIContent("envFilePath"), _envFilePath);
                _autoLoadOnStartup = EditorGUILayout.Toggle(new GUIContent("autoLoadOnStartup"), _autoLoadOnStartup);
                _allowOsEnvFallback = EditorGUILayout.Toggle(new GUIContent("allowOsEnvFallback"), _allowOsEnvFallback);

                EditorGUILayout.Space(6);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (_settingsAsset == null)
                    {
                        if (GUILayout.Button("Create LLMEnvSettings Asset"))
                        {
                            CreateSettingsAsset();
                            RefreshStatus();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Select Settings Asset"))
                        {
                            Selection.activeObject = _settingsAsset;
                            EditorGUIUtility.PingObject(_settingsAsset);
                        }

                        if (GUILayout.Button("Apply to Settings Asset"))
                        {
                            ApplyToExistingSettingsAsset();
                            RefreshStatus();
                        }
                    }
                }
            }
        }

        private void DrawEnvFilePanel()
        {
            EditorGUILayout.LabelField(".env File (recommended at project root)", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Resolved path:",
                    string.IsNullOrWhiteSpace(_resolvedEnvPath) ? "(invalid)" : _resolvedEnvPath);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Open Project Folder"))
                    {
                        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                        EditorUtility.RevealInFinder(projectRoot);
                    }

                    if (!string.IsNullOrWhiteSpace(_resolvedEnvPath) && File.Exists(_resolvedEnvPath))
                    {
                        if (GUILayout.Button("Show .env File"))
                            EditorUtility.RevealInFinder(_resolvedEnvPath);
                    }
                }
            }
        }

        private void DrawWriteEnvPanel()
        {
            EditorGUILayout.LabelField("Guided OpenAI .env (API key only)", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.HelpBox(
                    "For OpenAI-only v0 you need only OPENAI_API_KEY.\n" +
                    "This will write a .env file containing a single line:\n" +
                    "OPENAI_API_KEY=...",
                    MessageType.None);

                _openAIApiKey = EditorGUILayout.PasswordField(new GUIContent("OPENAI_API_KEY"), _openAIApiKey);

                EditorGUILayout.Space(8);

                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_openAIApiKey)))
                {
                    if (GUILayout.Button("Write .env File"))
                    {
                        WriteEnvFile();
                        LLMEnvLoader.Reload();
                        RefreshStatus();

                        // Optional: clear the field after writing so it’s not lingering in UI state
                        _openAIApiKey = "";
                    }
                }

                if (string.IsNullOrWhiteSpace(_openAIApiKey))
                {
                    EditorGUILayout.HelpBox("Enter an OpenAI API key to enable writing the .env file.", MessageType.Warning);
                }
            }
        }

        private void CreateSettingsAsset()
        {
            if (!AssetDatabase.IsValidFolder(ResourcesFolderPath))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            if (File.Exists(SettingsAssetPath))
            {
                EditorUtility.DisplayDialog("Already exists",
                    "LLMEnvSettings.asset already exists at:\n" + SettingsAssetPath, "OK");
                _settingsAsset = AssetDatabase.LoadAssetAtPath<LLMEnvSettings>(SettingsAssetPath);
                return;
            }

            var asset = ScriptableObject.CreateInstance<LLMEnvSettings>();
            asset.envFilePath = _envFilePath;
            asset.autoLoadOnStartup = _autoLoadOnStartup;
            asset.allowOsEnvFallback = _allowOsEnvFallback;

            AssetDatabase.CreateAsset(asset, SettingsAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _settingsAsset = asset;
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private void ApplyToExistingSettingsAsset()
        {
            if (_settingsAsset == null) return;

            Undo.RecordObject(_settingsAsset, "Update LLMEnvSettings");
            _settingsAsset.envFilePath = _envFilePath;
            _settingsAsset.autoLoadOnStartup = _autoLoadOnStartup;
            _settingsAsset.allowOsEnvFallback = _allowOsEnvFallback;

            EditorUtility.SetDirty(_settingsAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void WriteEnvFile()
        {
            var path = LLMEnvLoader.ResolvePath(_envFilePath);
            if (string.IsNullOrWhiteSpace(path))
            {
                EditorUtility.DisplayDialog("Invalid Path",
                    "The envFilePath is invalid. Please set a valid path (e.g., .env).", "OK");
                return;
            }

            // Confirm overwrite if file exists
            if (File.Exists(path))
            {
                if (!EditorUtility.DisplayDialog("Overwrite .env?",
                        $"A file already exists at:\n{path}\n\nOverwrite it?", "Overwrite", "Cancel"))
                {
                    return;
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine("# LLM package env (OpenAI v0)");
            sb.AppendLine($"{KeyOpenAIApiKey}={_openAIApiKey}");

            // Ensure directory exists
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);

            // Reveal so the user can confirm it exists (and optionally add to .gitignore)
            EditorUtility.RevealInFinder(path);
        }
    }
}
