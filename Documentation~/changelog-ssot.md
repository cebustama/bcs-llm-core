# Semantic Changelog — BCS / Eon LLM Core

## 2026-03-16 — Post-fix documentation alignment
- Updated `CURRENT_STATE.md` to reflect that the env-fallback and API-key-redaction issues were resolved in code.
- Updated `SSoT_CONTRACTS.md` to promote the env-fallback rule and the “redact secrets in debug-oriented strings” rule into current cross-cutting truth.
- Updated `SSoT_Runtime_and_OpenAI_Provider.md` to replace the old `allowOsEnvFallback` delta with the implemented runtime behavior.
- Updated `SSoT_Editor_Tooling_and_Wizard.md` to record the current stance:
  - explicit capability interface first,
  - reflection only as compatibility fallback.
- Recommended adding a short documentation-update protocol note to `SSoT_INDEX.md` or `Documentation~/README.md` so the maintenance loop is visible inside the package docs themselves.

## 2026-03-16 — Documentation governance migration
- Introduced a lightweight SSoT governance baseline under `Documentation~/`.
- Split previous mixed documentation responsibilities into:
  - `CURRENT_STATE.md`
  - `planning/Roadmap_LLM_Core.md`
  - `SSoT_Runtime_and_OpenAI_Provider.md`
  - `SSoT_Editor_Tooling_and_Wizard.md`
  - `SSoT_Pricing_Pipeline.md`
- Reclassified the EditorWindow integration guide as reference documentation.
- Reclassified manual wizard tests as validation/reference documentation.
- Archived the previous mixed state-plan document.
- Absorbed the previous pricing explainer into a pricing subsystem SSoT.
- Added explicit authority mapping via `coverage-matrix.md`.

## 2026-03-16 — Newly explicit implementation truths captured
- Base runtime contract remains `ILLMClient`.
- File upload and Responses-with-files are optional capability interfaces, not base-contract features.
- Wizard history suppression is an editor policy layered on top of runtime history storage.
- Effective instructions precedence was recorded explicitly.
- Pricing estimate precedence was recorded explicitly: catalog first, then client fallback.
- The `allowOsEnvFallback` settings field was originally documented as a current implementation/documentation delta and was later resolved in the post-fix alignment update above.
- The API-key exposure in `LLMClientData.ToString()` was originally documented as a watch item and was later resolved in the post-fix alignment update above.
