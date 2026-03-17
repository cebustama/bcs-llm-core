using System.Collections.Generic;

namespace BCS.LLM.Core.Prompts
{
    public sealed class PromptBuildContext
    {
        public string AgentId;
        public PromptBuildMode Mode = PromptBuildMode.Default;

        public PromptRetryContext Retry;
        public PromptContractHint ContractHint;
        public IReadOnlyList<PromptArtifactHint> ArtifactHints;
        public IReadOnlyDictionary<string, string> Variables;
    }

    public sealed class PromptRetryContext
    {
        public string ReasonCode;
        public string FailureSummary;
        public string PriorAttemptSummary;
    }

    public sealed class PromptContractHint
    {
        public string ContractName;
        public string ContractVersion;

        // Phase 2 contract-aware shape hints.
        public IReadOnlyList<PromptContractObjectHint> Objects;
        public IReadOnlyList<PromptTokenSetHint> TokenSets;
        public IReadOnlyList<PromptContractRuleHint> StructuredHardRules;

        // Legacy / compatibility surfaces retained so older callers do not break.
        public IReadOnlyList<string> RequiredFields;
        public IReadOnlyDictionary<string, IReadOnlyList<string>> AllowedTokenSets;
        public IReadOnlyList<string> HardRules;
    }

    public sealed class PromptContractObjectHint
    {
        public string ObjectName;
        public bool IsRootObject;
        public IReadOnlyList<PromptContractFieldHint> Fields;
    }

    public sealed class PromptContractFieldHint
    {
        public string FieldName;
        public string DisplayType;
        public bool IsRequired;
        public string TokenSetName;
        public string FixedValue;
        public string Notes;
    }

    public sealed class PromptTokenSetHint
    {
        public string Name;
        public IReadOnlyList<string> AllowedValues;
    }

    public sealed class PromptContractRuleHint
    {
        public string SectionKey;
        public string Text;
    }

    public sealed class PromptArtifactHint
    {
        public string Kind;         // pdf, json, image, etc.
        public string DisplayName;  // author-facing label
        public string Purpose;      // source, evidence, reference
    }
}
