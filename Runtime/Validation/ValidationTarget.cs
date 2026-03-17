namespace BCS.LLM.Core.Validation
{
    public sealed class ValidationTarget
    {
        public ValidationTargetScope Scope = ValidationTargetScope.WholeResponse;
        public string Path;
        public string ItemId;
        public string StableKey;

        public static ValidationTarget WholeResponse(string path = null)
        {
            return new ValidationTarget
            {
                Scope = ValidationTargetScope.WholeResponse,
                Path = path
            };
        }

        public static ValidationTarget Item(string itemId, string stableKey = null, string path = null)
        {
            return new ValidationTarget
            {
                Scope = ValidationTargetScope.Item,
                ItemId = itemId,
                StableKey = stableKey,
                Path = path
            };
        }

        public static ValidationTarget Field(string path, string itemId = null, string stableKey = null)
        {
            return new ValidationTarget
            {
                Scope = ValidationTargetScope.Field,
                Path = path,
                ItemId = itemId,
                StableKey = stableKey
            };
        }
    }
}
