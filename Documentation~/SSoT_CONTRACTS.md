# SSoT Contracts — BCS / Eon LLM Core

**Status:** Active  
**Authority:** Primary for cross-cutting shared semantics  
**Date:** 2026-03-16

## Purpose
This document stores rules and definitions that span runtime, provider, editor tooling, and pricing.

## Shared terms
- **ClientData**: a ScriptableObject defining provider-independent and provider-specific runtime configuration.
- **AgentData**: a ScriptableObject that composes an instructions asset, client config, upload purpose, and initial state/history placeholders.
- **System instructions**: baseline instruction text attached to the client config.
- **Agent instructions**: instructions text stored in a dedicated instructions asset.
- **Wizard override instructions**: editor-only textual override typed directly in the wizard.
- **Local conversation history**: the client's in-memory `ClientConversationHistory`.
- **Request history inclusion**: whether current local history is sent as context in the outgoing request.
- **File upload**: uploading a local PDF to the provider and receiving a file identifier.
- **File attachment**: including one or more uploaded `file_id`s in a request payload.
- **Pricing catalog**: non-secret ScriptableObject containing per-provider / per-model / per-tier USD-per-1M-token rates.

## Cross-cutting invariants

### 1) Secrets and non-secrets
- Secrets must come from env / OS env, not serialized assets.
- `OPENAI_API_KEY` is a secret.
- Base URL and endpoint defaults are non-secret configuration and may live in settings assets.
- Debug-oriented string representations must redact secret values rather than printing them directly.

### 2) Authority split for instructions
Effective instructions are resolved in this order:
1. wizard override text,
2. agent instructions asset text,
3. client-data `SystemInstructions`,
4. empty string.

This precedence is a package-level behavioral contract for the current editor tooling.

### 3) History semantics
- Local history storage and request-history inclusion are distinct concepts.
- Runtime clients own their local history store.
- Whether to suppress or include history in the outgoing request is a caller policy, not a change to the base runtime contract.

### 4) Capability interfaces
- `ILLMClient` is the base provider-agnostic contract.
- `ILLMFileClient` is an optional capability for provider-side file upload.
- `ILLMResponsesFileClient` is an optional capability for Responses requests with attached file IDs.
- Optional capabilities must not silently redefine the base `ILLMClient` surface.
- The current preferred integration pattern is:
  - explicit capability interface first,
  - reflection only as editor-side compatibility fallback.

### 5) OpenAI variant boundary
- Text-only requests can go through Chat Completions or Responses.
- File attachment is currently modeled as an OpenAI Responses feature, not a generic provider feature.

### 6) Pricing semantics
- Pricing is expressed as USD per 1M tokens.
- Cached input tokens are a subset of input tokens.
- Reasoning tokens may be treated as output tokens for estimation purposes when the caller opts in.
- Estimates are approximate tooling outputs, not billing reconciliation.

### 7) Env-fallback rule
- When `LLMEnvSettings` exists, `allowOsEnvFallback` governs whether missing values may fall back to OS environment variables.
- When no settings asset exists, the current behavior remains permissive for backward compatibility and OS-env fallback is still allowed.

### 8) Naming / compatibility
- `BCS.LLM.Core.*` is the preferred namespace/document terminology.
- Legacy `Eon.*` mentions are compatibility references only and must not be treated as the primary naming system.

## Boundary map
- Runtime/client/provider semantics → `SSoT_Runtime_and_OpenAI_Provider.md`
- Wizard/editor orchestration behavior → `SSoT_Editor_Tooling_and_Wizard.md`
- Pricing catalog / estimation behavior → `SSoT_Pricing_Pipeline.md`
