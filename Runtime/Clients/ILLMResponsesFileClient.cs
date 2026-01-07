using System.Collections.Generic;
using System.Threading.Tasks;

namespace BCS.LLM.Core.Clients
{
    // Optional capability interface (keeps ILLMClient unchanged).
    public interface ILLMResponsesFileClient : ILLMClient
    {
        Task<LLMCompletionResult> CreateResponseWithFilesAsync(
            string prompt,
            string instructions,
            IReadOnlyList<string> fileIds);
    }
}