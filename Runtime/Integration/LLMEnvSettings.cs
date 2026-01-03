using UnityEngine;

namespace BCS.LLM.Core.Env
{
    [CreateAssetMenu(menuName = "LLM/Env Settings", fileName = "LLMEnvSettings")]
    public sealed class LLMEnvSettings : ScriptableObject
    {
        [Header("Env File")]
        [Tooltip("Project-relative or absolute path to the .env file. Recommended: .env at project root.")]
        public string envFilePath = ".env";

        [Tooltip("If true, the loader attempts to load envFilePath on first access.")]
        public bool autoLoadOnStartup = true;

        [Tooltip("If true, missing keys fall back to OS environment variables.")]
        public bool allowOsEnvFallback = true;

        [Header("OpenAI Defaults (non-secret)")]
        [Tooltip("Default OpenAI API base URL used when OPENAI_BASE_URL is not provided. Usually keep this as-is.")]
        public string openAIBaseUrl = "https://api.openai.com";

        [Tooltip("Default Chat Completions endpoint used when OPENAI_CHAT_ENDPOINT is not provided.")]
        public string openAIChatEndpoint = "/v1/chat/completions";

        [Tooltip("Default Responses endpoint used when OPENAI_RESPONSES_ENDPOINT is not provided.")]
        public string openAIResponsesEndpoint = "/v1/responses";
    }
}
