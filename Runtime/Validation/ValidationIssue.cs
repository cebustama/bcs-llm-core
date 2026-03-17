using System.Collections.Generic;

namespace BCS.LLM.Core.Validation
{
    public sealed class ValidationIssue
    {
        public string Code;
        public ValidationSeverity Severity;
        public string Stage;
        public string Message;
        public ValidationTarget Target;
        public IReadOnlyDictionary<string, string> Metadata;

        public bool IsWarning => Severity == ValidationSeverity.Warning;
        public bool IsError => Severity == ValidationSeverity.Error;
        public bool IsBlocker => Severity == ValidationSeverity.Blocker;

        public override string ToString()
        {
            var code = string.IsNullOrWhiteSpace(Code) ? string.Empty : $" [{Code}]";
            var stage = string.IsNullOrWhiteSpace(Stage) ? string.Empty : $" [{Stage}]";
            return $"{Severity}{stage}{code}: {Message}";
        }
    }
}
