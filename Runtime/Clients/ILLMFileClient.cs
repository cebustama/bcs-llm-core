using System.Threading.Tasks;

namespace BCS.LLM.Core.Clients
{
    public interface ILLMFileClient
    {
        Task<LLMFileUploadResult> UploadFileAsync(
            string filePath, 
            string purpose = "user_data");
    }

    public sealed class LLMFileUploadResult
    {
        public string FileId;
        public string Filename;
        public long Bytes;
    }
}
