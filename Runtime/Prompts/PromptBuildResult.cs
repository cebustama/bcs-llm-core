using System.Collections.Generic;

namespace BCS.LLM.Core.Prompts
{
    public sealed class PromptBuildResult
    {
        public string InstructionsText;
        public string UserPromptText;

        public PromptBuildMode Mode = PromptBuildMode.Default;

        // Optional diagnostics / future editor visibility
        public IReadOnlyList<PromptSection> InstructionSections;
        public IReadOnlyList<PromptSection> UserSections;

        // Logical-only artifact hints. Never provider file_ids.
        public IReadOnlyList<PromptArtifactHint> ArtifactHints;

        public IReadOnlyDictionary<string, string> Metadata;
    }

    public sealed class PromptSection
    {
        public string Label;
        public string Text;
    }
}