using System.Collections.Generic;

namespace BCS.LLM.Core.Retry
{
    public interface IRetryClassifier<in TContext>
    {
        IReadOnlyList<RetryDirective> Classify(TContext context);
    }
}
