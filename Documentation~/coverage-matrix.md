# Coverage Matrix — BCS / Eon LLM Core

| Concept | Primary authority | Secondary references | Notes |
|---|---|---|---|
| Documentation reading order | `SSoT_INDEX.md` | root `README.md`, `Documentation~/README.md` | Navigation only |
| Documentation update protocol | `SSoT_INDEX.md` | `Documentation_Update_Protocol_Addendum.md`, root `README.md` | Local maintenance loop |
| Package overview / install / quick start | root `README.md` | `Documentation~/README.md` | Non-authoritative overview |
| Shared env / secrets policy | `SSoT_CONTRACTS.md` | root `README.md`, reference integration guide | Cross-cutting |
| Instruction precedence | `SSoT_CONTRACTS.md` + `SSoT_Editor_Tooling_and_Wizard.md` | reference integration guide | Wizard behavior |
| Prompt builder shared semantics | `SSoT_CONTRACTS.md` | `SSoT_Runtime_and_OpenAI_Provider.md`, `reference/Prompt_Builder_Implementation_Guide.md` | Terms/invariants |
| Prompt builder minimal adoption path | `reference/Prompt_Builder_Implementation_Guide.md` | `SSoT_CONTRACTS.md`, `SSoT_Runtime_and_OpenAI_Provider.md` | Reference/onboarding guidance only |
| Prompt builder runtime boundary | `SSoT_Runtime_and_OpenAI_Provider.md` | `SSoT_CONTRACTS.md`, `reference/Prompt_Builder_Implementation_Guide.md` | `IPromptBuilder<TInput>` and execution split |
| Prompt execution helper pattern | `SSoT_Runtime_and_OpenAI_Provider.md` | reference integration guide, Prompt Builder guide | Runtime convenience layer |
| Validation / repair shared semantics | `SSoT_CONTRACTS.md` | `SSoT_Runtime_and_OpenAI_Provider.md`, `reference/Prompt_Builder_Implementation_Guide.md` | Cross-cutting optional extension surfaces |
| Validation / repair runtime boundary | `SSoT_Runtime_and_OpenAI_Provider.md` | `SSoT_CONTRACTS.md`, Prompt Builder guide | Shared validators/repair hints vs provider/editor/project responsibility |
| Retry-classification shared semantics | `SSoT_CONTRACTS.md` | `SSoT_Runtime_and_OpenAI_Provider.md`, `reference/Prompt_Builder_Implementation_Guide.md` | `RetryDisposition`, `RetryDirective`, `IRetryClassifier<TContext>` |
| Retry-classification runtime boundary | `SSoT_Runtime_and_OpenAI_Provider.md` | `SSoT_CONTRACTS.md`, Prompt Builder guide, editor integration guide | Shared classifier surface vs provider/editor/project responsibility |
| Orchestration shared semantics | `SSoT_CONTRACTS.md` | `SSoT_Runtime_and_OpenAI_Provider.md`, `planning/Roadmap_LLM_Core.md` | Typed attempt-state + linear runner invariants |
| Orchestration runtime boundary | `SSoT_Runtime_and_OpenAI_Provider.md` | `SSoT_CONTRACTS.md`, editor integration guide | Shared runner vs provider/editor/project responsibility |
| Editor-owned workflow bridge pattern | `SSoT_Editor_Tooling_and_Wizard.md` | `reference/LLM_Core_EditorWindow_Integration_Guide.md` | Shared orchestration does not imply generic editor workflow UI |
| Base runtime client contract | `SSoT_Runtime_and_OpenAI_Provider.md` | root `README.md` | `ILLMClient` and behavior boundaries |
| Optional file upload capability | `SSoT_Runtime_and_OpenAI_Provider.md` | reference integration guide, test cases | `ILLMFileClient` |
| Optional Responses-with-files capability | `SSoT_Runtime_and_OpenAI_Provider.md` | editor SSoT, reference integration guide, test cases | `ILLMResponsesFileClient` |
| OpenAI API-variant semantics | `SSoT_Runtime_and_OpenAI_Provider.md` | root `README.md`, reference integration guide | Chat vs Responses |
| Wizard history policy | `SSoT_Editor_Tooling_and_Wizard.md` | `SSoT_CONTRACTS.md`, test cases | Editor policy |
| Wizard rebuild requirement | `SSoT_Editor_Tooling_and_Wizard.md` | test cases | Endpoint/config edits require rebuild |
| Agent/config asset roles | `SSoT_Runtime_and_OpenAI_Provider.md` | `SSoT_CONTRACTS.md`, root `README.md` | Data asset responsibilities |
| Env loader path resolution order | `SSoT_Runtime_and_OpenAI_Provider.md` | `SSoT_CONTRACTS.md` | Implementation truth |
| Pricing catalog semantics | `SSoT_Pricing_Pipeline.md` | root `README.md` | Non-secret catalog |
| Pricing estimation precedence | `SSoT_Pricing_Pipeline.md` | editor tooling SSoT | Catalog first, client fallback |
| Manual regression validation | `reference/llm-agent-wizard-test-cases.md` | — | Validation only |
| Reusable EditorWindow integration pattern | `reference/LLM_Core_EditorWindow_Integration_Guide.md` | `SSoT_Editor_Tooling_and_Wizard.md` | Reference only |
| Editor-owned retry UX / prompt-bridge pattern | `SSoT_Editor_Tooling_and_Wizard.md` | `reference/LLM_Core_EditorWindow_Integration_Guide.md` | Shared retry surfaces do not imply generic retry UI |
| Prompt Builder implementation pattern | `reference/Prompt_Builder_Implementation_Guide.md` | `SSoT_Runtime_and_OpenAI_Provider.md`, `SSoT_CONTRACTS.md` | Reference only |
| Current active milestone / holding stance | `CURRENT_STATE.md` | roadmap | Operational focus |
| Planned next steps | `planning/Roadmap_LLM_Core.md` | `CURRENT_STATE.md` | Planning only |
| Superseded mixed state-plan doc | `archive/llm-agent-wizard-state-and-plan.md` | — | Historical only |
| Absorbed old pricing explainer | `archive/llm-pricing-pipeline.md` | `SSoT_Pricing_Pipeline.md` | Historical only |
