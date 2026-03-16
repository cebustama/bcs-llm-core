# SSoT — Runtime and OpenAI Provider

**Status:** Active  
**Authority:** Primary for runtime/client/provider semantics  
**Date:** 2026-03-16

## Scope
This document defines the implemented truth for:
- runtime client abstractions,
- client config asset boundaries,
- OpenAI provider behavior,
- environment/config resolution as consumed by the provider,
- optional file capability surfaces.

## 1) Runtime responsibility split

### 1.1 `LLMClientData`
`LLMClientData` is the provider-agnostic ScriptableObject base for:
- sampling and output-limit parameters,
- stop sequences,
- baseline system instructions,
- per-client fallback pricing fields,
- provider identity and model/base-url abstraction.

It is not the place for provider-specific endpoint semantics beyond abstract properties.

### 1.2 `OpenAIClientData`
`OpenAIClientData` specializes `LLMClientData` with:
- the OpenAI API-variant choice,
- a stable enum-backed model selector,
- env/settings-backed resolution of base URL and endpoints.

### 1.3 `LLMAgentData`
`LLMAgentData` composes:
- agent identity fields,
- an optional dedicated instructions asset,
- a client config asset reference,
- default upload purpose for editor file upload,
- placeholders for initial state/history.

`LLMAgentData` is composition/orchestration data, not the provider implementation.

## 2) Base runtime contract

### 2.1 `ILLMClient`
`ILLMClient` is the current provider-agnostic base contract. It includes:
- general model/sampling parameters,
- system instruction storage,
- per-client fallback pricing fields,
- local conversation-history storage,
- text-only completion entrypoints.

It does **not** include file upload or file attachment methods.

### 2.2 Optional capability interfaces
Two optional capability interfaces currently exist:
- `ILLMFileClient` for provider-side file upload,
- `ILLMResponsesFileClient` for Responses requests that include file IDs.

These capabilities are additive and do not change the base contract shape.

## 3) Client creation
`LLMClientFactory` currently supports OpenAI and returns `OpenAILLMClient` when the provider is OpenAI. Unsupported providers log an error and return null.

## 4) OpenAI provider behavior

### 4.1 Construction
`OpenAILLMClient` copies the relevant runtime parameters from `OpenAIClientData`, resolves base URL and endpoints, reads the API key from env-backed config, initializes the HTTP client, and copies pricing fields into the runtime instance.

### 4.2 API variants
The provider supports two text-generation request paths:
- **Chat Completions**
- **Responses**

The active path is selected by `OpenAIClientData.ApiVariant`.

### 4.3 Chat Completions path
The Chat Completions request path:
- builds a message list with instructions + prompt + history,
- sends `frequency_penalty` and stop sequences when available,
- parses prompt/completion token usage,
- appends user and assistant turns to local history on successful responses.

### 4.4 Responses path
The Responses text-only request path:
- builds a message list from current local history plus the new prompt,
- passes instructions separately,
- avoids request fields known to cause schema issues in the current request shape,
- parses input/output/reasoning token usage,
- appends user and assistant turns to local history on successful responses.

### 4.5 Request history inclusion
The runtime client owns the local history store. It does not expose a first-class “include history in request” flag on the base contract. Callers can influence outgoing context by manipulating `ClientConversationHistory` around the call.

That means:
- history storage is runtime state,
- history inclusion is caller policy.

## 5) File upload and file attachment

### 5.1 Upload
`OpenAILLMClient` implements `ILLMFileClient.UploadFileAsync(...)`.
Current implementation characteristics:
- intended for editor/tooling workflows,
- currently PDF-oriented in practice,
- returns `FileId`, `Filename`, and `Bytes`.

### 5.2 File attachment in requests
`OpenAILLMClient` also implements `ILLMResponsesFileClient`.
Current characteristics:
- attachment is modeled as a Responses-only feature,
- when no file IDs are provided, behavior falls back to the normal text-only path,
- when file IDs are provided but the API variant is not Responses, the provider warns and falls back to text-only behavior.

### 5.3 Serialization note
The file-attachment request shape depends on null fields being ignored so that file-part DTOs do not serialize unsupported text fields into file input parts.

## 6) History mutation on success/failure
Current implemented behavior:
- successful requests append user + assistant turns to local history,
- failure paths return an empty result and do not establish a successful turn,
- callers that temporarily suppress history for a request must restore state themselves if they want continuity.

## 7) Env / settings resolution as consumed by runtime

### 7.1 Path resolution order
The env loader currently resolves `.env` source in this order:
1. `LLM_ENV_PATH` or legacy `EON_ENV_PATH`,
2. `LLMEnvSettings.envFilePath` when auto-load is enabled,
3. default project-root `.env`.

### 7.2 Value resolution
Current OpenAI config behavior:
- `OPENAI_API_KEY` is read from env via `LLMEnvLoader.Get(...)`.
- Base URL and endpoint values are resolved from env first, then `LLMEnvSettings`, then hard-coded defaults.

### 7.3 OS-env fallback behavior
Current behavior is now explicit:
- if `LLMEnvSettings` exists, `allowOsEnvFallback` governs whether missing keys may fall back to OS environment variables;
- if no settings asset exists, permissive OS-env fallback remains enabled for backward compatibility.

That means the setting is now a real runtime gate when settings are present, not just a UI placeholder.

## 8) Pricing values in runtime
Runtime clients carry fallback pricing fields copied from `LLMClientData`. These are not the preferred authoritative source for tooling when a central catalog is available, but they are the implemented fallback path.

## 9) Known implementation-sensitive areas
- Endpoint/base-url edits require client rebuild in editor workflows.
- The provider contract is explicit for file capabilities, but editor tooling still keeps a reflection fallback path for compatibility.
- `LLMClientData.ToString()` now redacts the API key; this should remain true and should not be regressed casually.

## 10) Not authoritative here
This document does not define:
- wizard UX sequencing,
- editor panel behavior,
- manual regression test steps,
- pricing catalog maintenance procedure.
Those belong elsewhere.
