using BCS.LLM.Core.Clients;
using System.Collections.Generic;
using UnityEngine;

namespace BCS.LLM.Core.Agents
{
    [CreateAssetMenu(fileName = "NewLLMAgentData", menuName = "BCS/LLM/Agent Data", order = 1)]
    public class LLMAgentData : ScriptableObject
    {
        [Header("Agent Configuration")]
        public string AgentName;
        public string AgentID;
        public LLMAgentInstructionsData AgentInstructionsData;

        [Header("LLM Client Configuration")]
        public LLMClientData LlmClientData;

        [Header("Initial State")]
        // TODO: Use serializable format or just custom class
        public List<KeyValuePair<string, string>> InitialState;

        [Header("Files (optional)")]
        [Tooltip("Default purpose used when uploading files via ILLMFileClient (Editor tooling).")]
        public string DefaultUploadPurpose = "user_data";

        [Header("Conversation History")]
        public List<string> InitialHistory;
    }
}
