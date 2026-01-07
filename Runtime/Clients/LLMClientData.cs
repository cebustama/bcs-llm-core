using System.Collections.Generic;
using UnityEngine;

namespace BCS.LLM.Core.Clients
{
    [CreateAssetMenu(fileName = "NewLLMClientData", menuName = "BCS/LLM/Client Data", order = 1)]
    public abstract class LLMClientData : ScriptableObject
    {
        public enum LLMProvider
        {
            OpenAI,
            Gemini,
            Azure
        }

        public virtual LLMProvider Provider { get; } // Virtual property for provider type

        [Header("General Configuration")]
        [Tooltip("Controls the randomness of the output.")]
        [Range(0.0f, 2.0f)]
        public float Temperature = 1.0f;

        [Tooltip("Limits the number of tokens in the generated output.")]
        public int MaxOutputTokens = 200;

        [Tooltip("Controls the diversity of the output by considering the top P% of probability mass.")]
        [Range(0.0f, 1.0f)]
        public float TopP = 1.0f;

        [Tooltip("Penalizes repeated tokens to reduce repetition.")]
        [Range(-2.0f, 2.0f)]
        public float FrequencyPenalty = 0.0f;

        [Tooltip("Specifies token sequences where the model should stop generating output.")]
        public List<string> StopSequences = new List<string>();

        [Tooltip("System instructions for the model.")]
        [TextArea]
        public string SystemInstructions = "You are a helpful assistant.";

        /// <summary>
        /// Abstract property to retrieve the model string for the selected provider.
        /// </summary>
        public abstract string ModelString { get; }

        /// <summary>
        /// Abstract property to retrieve the API key for the selected provider.
        /// </summary>
        public abstract string ApiKey { get; }

        /// <summary>
        /// Abstract property to retrieve the Base URL for the selected provider.
        /// </summary>
        public abstract string BaseUrl { get; }

        [Header("Pricing")]
        public float InputUSDPerMTokens;
        public float CachedInputUSDPerMTokens;
        public float OutputUSDPerMTokens;

        public override string ToString()
        {
            return $"Provider: {Provider}\n" +
                $"Model: {ModelString}\n" +
                $"API Key: {ApiKey}\n" +
                $"Base URL: {BaseUrl}\n" +
                $"Temperature: {Temperature}\n" +
                $"Max Output Tokens: {MaxOutputTokens}\n" +
                $"Top P: {TopP}\n" +
                $"Frequency Penalty: {FrequencyPenalty}\n" +
                $"Stop Sequences: {string.Join(", ", StopSequences)}\n" +
                $"System Instructions: {SystemInstructions}";
        }
    }
}
