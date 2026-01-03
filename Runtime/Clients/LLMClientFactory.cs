using UnityEngine;
using static Eon.Narrative.LLM.Clients.LLMClientData;

namespace Eon.Narrative.LLM.Clients
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
