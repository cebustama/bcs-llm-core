using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BCS.LLM.Core.Anthropic;
using BCS.LLM.Core.Clients;
using BCS.LLM.Core.Env;
using BCS.LLM.Core.OpenAI;
using UnityEditor;
using UnityEngine;

namespace BCS.LLM.Core.Editor
{
    /// <summary>
    /// Environment setup helper.
    /// - Detects LLMEnvSettings asset in Resources.
    /// - Detects .env file presence and which provider keys are available.
    /// - Guides creation of LLMEnvSettings and writing .env entries for
    ///   OpenAI and/or Anthropic.
    /// - Per-provider Ping button: sends a minimal request to verify the
    ///   key + network + provider end-to-end.
    /// - Reloads LLMEnvLoader after changes.
    /// </summary>
    public sealed class LLMEnvSetupWindow : EditorWindow
    {
        private const string KeyOpenAIApiKey = "OPENAI_API_KEY";
        private const string KeyAnthropicApiKey = "ANTHROPIC_API_KEY";

        private const string ResourcesFolderPath = "Assets/Resources";
        private const string SettingsAssetPath = "Assets/Resources/LLMEnvSettings.asset";

        private Vector2 _scroll;

        // Settings panel state
        private string _envFilePath = LLMEnvLoader.DefaultEnvRelativePath; // default ".env"
        private bool _autoLoadOnStartup = true;
        private bool _allowOsEnvFallback = true;

        // Provider-key inputs
        private string _openAIApiKey = "";
        private string _anthropicApiKey = "";

        // Status cache
        private LLMEnvSettings _settingsAsset;
        private string _resolvedEnvPath;
        private bool _envFileExists;
        private bool _hasOpenAIKey;
        private bool _hasAnthropicKey;
        private bool _effectiveAllowOsEnvFallback;

        [MenuItem("Tools/LLM/Env Setup")]
        public static void Open()
        {
            var w = GetWindow<LLMEnvSetupWindow>("LLM Env Setup");
            w.minSize = new Vector2(540, 560);
            w.RefreshStatus();
        }

        private void OnEnable() => RefreshStatus();
        private void OnFocus() => RefreshStatus();

        private void RefreshStatus()
        {
            _settingsAsset = Resources.Load<LLMEnvSettings>(LLMEnvLoader.SettingsResourceName);

            if (_settingsAsset != null && !string.IsNullOrWhiteSpace(_settingsAsset.envFilePath))
            {
                _envFilePath = _settingsAsset.envFilePath;
                _autoLoadOnStartup = _settingsAsset.autoLoadOnStartup;
                _allowOsEnvFallback = _settingsAsset.allowOsEnvFallback;
            }

            _resolvedEnvPath = LLMEnvLoader.ResolvePath(_envFilePath);
            _envFileExists = !string.IsNullOrWhiteSpace(_resolvedEnvPath) && File.Exists(_resolvedEnvPath);

            _effectiveAllowOsEnvFallback = LLMEnvLoader.IsOsEnvFallbackAllowed();

            _hasOpenAIKey = LLMEnvLoader.HasNonEmpty(KeyOpenAIApiKey);
            _hasAnthropicKey = LLMEnvLoader.HasNonEmpty(KeyAnthropicApiKey);

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

            DrawProviderPanel(
                title: "OpenAI",
                envKey: KeyOpenAIApiKey,
                keyFieldGetter: () => _openAIApiKey,
                keyFieldSetter: v => _openAIApiKey = v,
                hasKey: _hasOpenAIKey,
                pingAction: PingOpenAI);

            EditorGUILayout.Space(8);

            DrawProviderPanel(
                title: "Anthropic",
                envKey: KeyAnthropicApiKey,
                keyFieldGetter: () => _anthropicApiKey,
                keyFieldSetter: v => _anthropicApiKey = v,
                hasKey: _hasAnthropicKey,
                pingAction: PingAnthropic);

            EditorGUILayout.EndScrollView();
        }

        // -------------------------
        // UI sections
        // -------------------------

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("LLM Environment Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This window configures env loading for the LLM Core package.\n\n" +
                "Recommended: keep a .env file at project root (\".env\") and store " +
                "OPENAI_API_KEY and/or ANTHROPIC_API_KEY there.\n\n" +
                "Base URLs and endpoints come from LLMEnvSettings (with env overrides).\n\n" +
                "Writing a key here merges into the existing .env — other keys are preserved.",
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
                EditorGUILayout.LabelField("OS env fallback enabled:",
                    _effectiveAllowOsEnvFallback ? "YES" : "NO");

                EditorGUILayout.LabelField("OPENAI_API_KEY available:", _hasOpenAIKey ? "YES" : "NO");
                EditorGUILayout.LabelField("ANTHROPIC_API_KEY available:", _hasAnthropicKey ? "YES" : "NO");

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
                    "LLMEnvSettings is optional. If present in " +
                    "Assets/Resources/LLMEnvSettings.asset and auto-load is enabled, " +
                    "the loader will use its envFilePath automatically.\n\n" +
                    "allowOsEnvFallback controls whether missing keys may fall back to " +
                    "OS environment variables.",
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

        /// <summary>
        /// Render one provider section (label, API key input, write button,
        /// status, ping button). Used for both OpenAI and Anthropic.
        /// </summary>
        private void DrawProviderPanel(
            string title,
            string envKey,
            Func<string> keyFieldGetter,
            Action<string> keyFieldSetter,
            bool hasKey,
            Action pingAction)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField($"{envKey} currently set:", hasKey ? "YES" : "NO");

                var current = keyFieldGetter();
                var next = EditorGUILayout.PasswordField(new GUIContent(envKey), current);
                if (!ReferenceEquals(next, current))
                    keyFieldSetter(next);

                EditorGUILayout.Space(6);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(keyFieldGetter())))
                    {
                        if (GUILayout.Button($"Write {envKey} to .env"))
                        {
                            WriteOrUpdateEnvKey(envKey, keyFieldGetter());
                            LLMEnvLoader.Reload();
                            // Clear field after write so it doesn't linger in UI state
                            keyFieldSetter("");
                            RefreshStatus();
                        }
                    }

                    using (new EditorGUI.DisabledScope(!hasKey))
                    {
                        if (GUILayout.Button($"Ping {title}"))
                        {
                            pingAction();
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(keyFieldGetter()) && !hasKey)
                {
                    EditorGUILayout.HelpBox(
                        $"Enter a {title} API key to enable writing the .env file.",
                        MessageType.Warning);
                }
            }
        }

        // -------------------------
        // Settings asset
        // -------------------------

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

        // -------------------------
        // .env merge writer (read existing → update/add key → write back)
        // -------------------------

        private void WriteOrUpdateEnvKey(string key, string value)
        {
            var path = LLMEnvLoader.ResolvePath(_envFilePath);
            if (string.IsNullOrWhiteSpace(path))
            {
                EditorUtility.DisplayDialog("Invalid Path",
                    "The envFilePath is invalid. Please set a valid path (e.g., .env).", "OK");
                return;
            }

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Read existing lines (or start fresh)
            var lines = new List<string>();
            if (File.Exists(path))
            {
                try { lines.AddRange(File.ReadAllLines(path, Encoding.UTF8)); }
                catch (Exception ex) { Debug.LogWarning($"Could not read existing .env: {ex.Message}"); }
            }

            // Update existing key in place, or append.
            bool updated = false;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("#")) continue; // preserve comments

                var eqIdx = line.IndexOf('=');
                if (eqIdx <= 0) continue;

                var lineKey = line.Substring(0, eqIdx).Trim();
                if (string.Equals(lineKey, key, StringComparison.Ordinal))
                {
                    lines[i] = $"{key}={value}";
                    updated = true;
                    break;
                }
            }

            if (!updated)
            {
                if (lines.Count == 0)
                    lines.Add("# LLM Core env");
                lines.Add($"{key}={value}");
            }

            File.WriteAllLines(path, lines, Encoding.UTF8);
            EditorUtility.RevealInFinder(path);
        }

        // -------------------------
        // Ping (per-provider; uses temporary in-memory client data)
        // -------------------------

        private static async void PingOpenAI()
        {
            var data = ScriptableObject.CreateInstance<OpenAIClientData>();
            data.MaxOutputTokens = 16;
            data.SystemInstructions = "";
            await PingClient(data, "OpenAI", data.ModelString);
            ScriptableObject.DestroyImmediate(data);
        }

        private static async void PingAnthropic()
        {
            var data = ScriptableObject.CreateInstance<AnthropicClientData>();
            data.MaxOutputTokens = 16;
            data.SystemInstructions = "";
            await PingClient(data, "Anthropic", data.ModelString);
            ScriptableObject.DestroyImmediate(data);
        }

        private static async System.Threading.Tasks.Task PingClient(
            LLMClientData data, string providerLabel, string modelLabel)
        {
            ILLMClient client = LLMClientFactory.CreateClient(data);
            if (client == null)
            {
                EditorUtility.DisplayDialog(
                    $"{providerLabel} Ping",
                    "Could not build client. Check Console for details.",
                    "OK");
                return;
            }

            float t0 = Time.realtimeSinceStartup;
            try
            {
                var result = await client.CreateChatCompletionAsync("ping", "");
                float elapsedMs = (Time.realtimeSinceStartup - t0) * 1000f;

                if (result == null || string.IsNullOrEmpty(result.OutputText))
                {
                    EditorUtility.DisplayDialog(
                        $"{providerLabel} Ping",
                        $"FAILED after {elapsedMs:0}ms.\n\n" +
                        "The request returned no text. Common causes:\n" +
                        "  • API key invalid or missing\n" +
                        "  • Network blocked\n" +
                        "  • Model name not accepted by the API\n\n" +
                        "Check the Console for the exact error.",
                        "OK");
                    return;
                }

                EditorUtility.DisplayDialog(
                    $"{providerLabel} Ping",
                    $"OK ({elapsedMs:0}ms, model: {modelLabel})\n\n" +
                    $"Tokens — in: {result.InputTokens}, out: {result.OutputTokens}\n\n" +
                    $"Response: {result.OutputText}",
                    "OK");
            }
            catch (Exception ex)
            {
                float elapsedMs = (Time.realtimeSinceStartup - t0) * 1000f;
                EditorUtility.DisplayDialog(
                    $"{providerLabel} Ping Error",
                    $"After {elapsedMs:0}ms:\n\n{ex.GetType().Name}: {ex.Message}",
                    "OK");
            }
        }
    }
}