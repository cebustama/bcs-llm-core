using System.Collections.Generic;

namespace BCS.LLM.Core.Validation
{
    public interface IRepairHintProvider<in T>
    {
        IReadOnlyList<RepairHint> BuildHints(T value, ValidationResult validation);
    }
}
