using System.Collections.Generic;

namespace BCS.LLM.Core.Validation
{
    public sealed class RepairHint
    {
        public string ReasonCode;
        public string Summary;
        public string GuidanceText;
        public ValidationTarget Target;
        public IReadOnlyDictionary<string, string> Metadata;
    }
}
