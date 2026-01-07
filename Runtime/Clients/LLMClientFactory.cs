using BCS.LLM.Core.OpenAI;
using UnityEngine;
using static BCS.LLM.Core.Clients.LLMClientData;

namespace BCS.LLM.Core.Clients
{
    public static class LLMClientFactory
    {
        public static ILLMClient CreateClient(LLMClientData clientData)
        {
            if (clientData == null)
            {
                Debug.LogError("LLMClientData is null. Cannot create LLMClient.");
                return null;
            }

            switch (clientData.Provider)
            {
                case LLMProvider.OpenAI:
                    return new OpenAILLMClient(clientData as OpenAIClientData);
/*
                case LLMProvider.Gemini:
                    return new GeminiLLMClient(clientData as GeminiClientData);
                case LLMProvider.Azure:
                    return new AzureLLMClient(clientData as AzureClientData);
*/
                default:
                    Debug.LogError($"Unsupported LLM Provider: {clientData.Provider}");
                    return null;
            }
        }
    }
}
