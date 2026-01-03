using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BCS.LLM.Core.Env
{
    /// <summary>Loads key/value pairs from a .env file and/or OS env vars.</summary>
    public static class LLMEnvLoader
    {
        public const string EnvPathVar = "LLM_ENV_PATH";
        public const string LegacyEnvPathVar = "EON_ENV_PATH"; // optional compatibility
        public const string SettingsResourceName = "LLMEnvSettings";
        public const string DefaultEnvRelativePath = ".env";

        private static readonly Dictionary<string, string> _vars = new(StringComparer.Ordinal);
        private static bool _initialized;
        private static string _loadedFromPath;

        public static bool IsInitialized => _initialized;
        public static string LoadedFromPath => _loadedFromPath;

        public static void EnsureLoaded()
        {
            if (_initialized) return;
            LoadFromBestSource();
            _initialized = true;
        }

        public static void Reload()
        {
            _vars.Clear();
            _loadedFromPath = null;
            _initialized = false;
            EnsureLoaded();
        }

        private static void LoadFromBestSource()
        {
            // 1) OS env var path override
            var osPath = Environment.GetEnvironmentVariable(EnvPathVar);
            if (string.IsNullOrWhiteSpace(osPath))
                osPath = Environment.GetEnvironmentVariable(LegacyEnvPathVar);

            if (!string.IsNullOrWhiteSpace(osPath))
            {
                TryLoadEnvFile(osPath, throwIfMissing: false);
                _loadedFromPath = ResolvePath(osPath);
                return;
            }

            // 2) Resources settings asset
            var settings = Resources.Load<LLMEnvSettings>(SettingsResourceName);
            if (settings != null && settings.autoLoadOnStartup && !string.IsNullOrWhiteSpace(settings.envFilePath))
            {
                TryLoadEnvFile(settings.envFilePath, throwIfMissing: false);
                _loadedFromPath = ResolvePath(settings.envFilePath);
                return;
            }

            // 3) Fallback: project root ".env"
            TryLoadEnvFile(DefaultEnvRelativePath, throwIfMissing: false);
            _loadedFromPath = ResolvePath(DefaultEnvRelativePath);
        }

        public static void TryLoadEnvFile(string filePath, bool throwIfMissing = false)
        {
            var resolved = ResolvePath(filePath);

            if (string.IsNullOrWhiteSpace(resolved) || !File.Exists(resolved))
            {
                if (throwIfMissing)
                    throw new FileNotFoundException($"Env file not found at path: {resolved}");
                return;
            }

            foreach (var rawLine in File.ReadAllLines(resolved))
            {
                if (string.IsNullOrWhiteSpace(rawLine))
                    continue;

                var trimmed = rawLine.Trim();

                if (trimmed.StartsWith("#", StringComparison.Ordinal) ||
                    trimmed.StartsWith("//", StringComparison.Ordinal))
                    continue;

                // Allow KEY= (empty value) and split only on first '='
                var parts = trimmed.Split(new[] { '=' }, 2, StringSplitOptions.None);
                if (parts.Length != 2)
                    continue;

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                if (string.IsNullOrEmpty(key))
                    continue;

                _vars[key] = value; // overwrite allowed
            }
        }

        public static string Get(string key) => TryGet(key, out var v) ? v : null;

        public static bool TryGet(string key, out string value)
        {
            EnsureLoaded();

            if (string.IsNullOrEmpty(key))
            {
                value = null;
                return false;
            }

            if (_vars.TryGetValue(key, out value))
                return true;

            // Optional OS fallback (always on here; you can gate it via settings if you want)
            value = Environment.GetEnvironmentVariable(key);
            return value != null;
        }

        public static string GetOrDefault(string key, string defaultValue)
            => TryGet(key, out var v) ? v : defaultValue;

        public static bool HasNonEmpty(string key)
            => TryGet(key, out var v) && !string.IsNullOrWhiteSpace(v);

        public static string ResolvePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return null;
            if (Path.IsPathRooted(filePath)) return filePath;

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, filePath));
        }
    }
}
