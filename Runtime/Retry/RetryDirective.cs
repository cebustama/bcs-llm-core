using System.Collections.Generic;
using BCS.LLM.Core.Prompts;
using BCS.LLM.Core.Validation;

namespace BCS.LLM.Core.Retry
{
    public sealed class RetryDirective
    {
        // High-level retry outcome.
        public RetryDisposition Disposition = RetryDisposition.NoRetry;

        // Stable machine-readable reason, suitable for branching or telemetry.
        public string ReasonCode;

        // Human-readable summary, suitable for PromptRetryContext or UI surfaces.
        public string Summary;

        // Reuse the shared Phase 3 validation targeting model instead of inventing a
        // second retry-target model.
        public ValidationTarget Target;

        // Optional hint for which prompt build mode should be used if the caller re-enters.
        public PromptBuildMode? SuggestedBuildMode;

        // Optional lightweight metadata bag for caller-owned bridge logic.
        // Keep this small and generic; do not use it to smuggle full domain DTOs into Core.
        public IReadOnlyDictionary<string, string> Metadata;
    }
}
