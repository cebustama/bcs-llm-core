#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using BCS.LLM.Core.Pricing;

public static class OpenAIPricingCatalogMenu
{
    [MenuItem("BCS/LLM/Pricing/Apply OpenAI Standard Defaults (Selected Catalog)")]
    private static void ApplyToSelected()
    {
        var catalog = Selection.activeObject as LLMModelPricingCatalogSO;
        if (catalog == null)
        {
            EditorUtility.DisplayDialog("LLM Pricing", "Select a LLMModelPricingCatalogSO asset first.", "OK");
            return;
        }

        catalog.ApplyOpenAIStandardTextDefaults(overwriteExisting: false);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("LLM Pricing", "OpenAI Standard defaults applied.", "OK");
    }
}
#endif
