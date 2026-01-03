using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Eon.Narrative.LLM.Clients
{
    public class LLMClientBase : ILLMClient
    {
        public string Model { get; set; }
        public float Temperature { get; set; }
        public int MaxOutputTokens { get; set; }
        public float TopP { get; set; }
        public float FrequencyPenalty { get; set; }
        public List<string> StopSequences { get; set; } = new();

        public string SystemInstructions { get; set; } = "You are a helpful assistant.";

        // Conversation History Management
        public List<ChatMessage> ClientConversationHistory { get; set; } = new();

        // Pricing
        public float InputUSDPerMTokens { get; set; }
        public float CachedInputUSDPerMTokens { get; set; }
        public float OutputUSDPerMTokens { get; set; }

        public void ModifySystemInstructions(string instruction)
        {
            SystemInstructions = instruction ?? "";
            Debug.Log($"System Instructions updated.");
        }

        public void AddMessageToHistory(string role, string content)
        {
            ClientConversationHistory ??= new List<ChatMessage>();
            ClientConversationHistory.Add(new ChatMessage { role = role, content = content });
        }

        public virtual List<KeyValuePair<string, string>> GetFormattedConversationHistory()
        {
            if (ClientConversationHistory == null) return new();
            return ClientConversationHistory
                .Select(m => new KeyValuePair<string, string>(m.role, m.content))
                .ToList();
        }

        public void ClearHistory() => ClientConversationHistory?.Clear();

        public virtual async Task<LLMCompletionResult> CreateChatCompletionAsync(string prompt)
        {
            await Task.CompletedTask;
            throw new System.NotImplementedException();
        }

        public virtual async Task<LLMCompletionResult> CreateChatCompletionAsync(string prompt, string instructions)
        {
            ModifySystemInstructions(instructions);
            return await CreateChatCompletionAsync(prompt);
        }
    }
}
