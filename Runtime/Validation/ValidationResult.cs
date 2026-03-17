using System.Collections.Generic;
using System.Linq;

namespace BCS.LLM.Core.Validation
{
    public sealed class ValidationResult
    {
        public string ValidatorId;
        public string Stage;
        public readonly List<ValidationIssue> Issues = new();

        public int WarningCount => Issues.Count(i => i.Severity == ValidationSeverity.Warning);
        public int ErrorCount => Issues.Count(i => i.Severity == ValidationSeverity.Error);
        public int BlockerCount => Issues.Count(i => i.Severity == ValidationSeverity.Blocker);

        public bool HasWarnings => WarningCount > 0;
        public bool HasErrors => ErrorCount > 0;
        public bool HasBlockers => BlockerCount > 0;
        public bool HasIssues => Issues.Count > 0;

        public ValidationSeverity? MaxSeverity
        {
            get
            {
                if (HasBlockers) return ValidationSeverity.Blocker;
                if (HasErrors) return ValidationSeverity.Error;
                if (HasWarnings) return ValidationSeverity.Warning;
                return null;
            }
        }
    }
}
