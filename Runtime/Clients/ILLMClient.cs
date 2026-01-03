using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eon.Narrative.LLM.Clients
{
    public interface ILLMClient
    {
        // Shared parameters for LLM clients
        string Model { get; set; }
        float Temperature { get; set; }
        int MaxOutputTokens { get; set; }
        float TopP { get; set; }
        float FrequencyPenalty { get; set; }
        List<string> StopSequences { get; set; }

        // System Instruction Management
        string SystemInstructions { get; set; }
        void ModifySystemInstructions(string instructions);

        // Pricing parameters
        float InputUSDPerMTokens { get; set; }
        float CachedInputUSDPerMTokens { get; set; }
        float OutputUSDPerMTokens { get; set; }

        // Conversation History Management
        List<ChatMessage> ClientConversationHistory { get; set; }
        void AddMessageToHistory(string role, string content);
        List<KeyValuePair<string, string>> GetFormattedConversationHistory();
        void ClearHistory();

        // Methods for interaction
        Task<LLMCompletionResult> CreateChatCompletionAsync(string prompt);
        Task<LLMCompletionResult> CreateChatCompletionAsync(string prompt, string instructions);
    }

    public class ChatMessage
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    public class LLMCompletionResult
    {
        public string OutputText { get; set; }

        /// <summary> Total input tokens sent to the model for this request. </summary>
        public int InputTokens { get; set; }

        /// <summary>
        /// Subset of <see cref="InputTokens"/> served from prompt cache (if supported/available).
        /// </summary>
        public int CachedInputTokens { get; set; }

        /// <summary> Total output tokens produced for this request. </summary>
        public int OutputTokens { get; set; }

        /// <summary>
        /// Reasoning tokens reported separately by some APIs/models (when available).
        /// Most pricing treats these as output tokens.
        /// </summary>
        public int ReasoningTokens { get; set; }
    }
}
