# LLMAgentWizardWindow v0 — Manual Test Cases (UPDATED 2026-01-03)

These are **step-by-step** test cases you can run in a clean Unity project to validate:
- env setup
- agent loading + client rebuild
- Responses + Chat Completions
- history storage vs “Use Conversation History”
- token usage parsing
- pricing estimate display
- error handling

---

## 0) One-time setup prerequisites

1. Ensure you have:
   - `LLMEnvSettings.asset` under `Assets/Resources/` (or equivalent)
   - `LLMEnvSetupWindow` accessible from a menu item
2. Ensure your OpenAI key is written to `.env` as:
   - `OPENAI_API_KEY=...`

Expected:
- No exceptions on project load.
- Env loader logs (if you have logging) indicate the key was found.

---

## 1) Asset wiring (smoke test)

1. Create an `OpenAIClientData` asset.
   - Set ApiVariant = **Responses** (recommended).
   - Set a known model id you expect to work.
2. Create an `LLMAgentInstructionsData` asset with a distinctive instruction, e.g.:
   - “You MUST answer with exactly 3 bullet points.”
3. Create an `LLMAgentData` asset:
   - Assign the `OpenAIClientData`
   - Assign the instructions asset

Expected:
- Inspector shows references are not null.
- No warnings about missing API key.

---

## 2) Open the wizard + rebuild

1. Open **LLMAgentWizardWindow** from its menu item.
2. Select your `LLMAgentData`.
3. Click **Rebuild Client**.

Expected:
- Status shows “client built” (or equivalent).
- No exceptions in Console.

---

## 3) Ping (connectivity/auth)

1. Click **Ping**.

Expected:
- A response is returned quickly.
- If you display token usage: input/output tokens are non-zero (or at least one category is non-zero).

Failure modes to verify:
- If `OPENAI_API_KEY` is missing/invalid: the wizard shows a clear error and exits busy state.

---

## 4) Basic prompt/response

1. Enter a simple prompt:
   - “Say 'ok'.”
2. Send it.

Expected:
- Output text is not null.
- History now contains at least:
  - user prompt
  - assistant reply

---

## 5) Instructions application

### 5.1 Agent instructions ON
1. Toggle **Use Agent Instructions** = ON.
2. Prompt:
   - “Explain gravity.”

Expected:
- Output respects the instruction format (e.g., exactly 3 bullet points if that’s your rule).

### 5.2 Agent instructions OFF / override
1. Toggle **Use Agent Instructions** = OFF.
2. Set **Override Instructions** to:
   - “Reply with exactly one sentence.”
3. Prompt:
   - “Explain gravity.”

Expected:
- Output matches override rules.

---

## 6) History storage vs history usage (critical)

### 6.1 Confirm history is stored
1. Send:
   - “Remember that my favorite fruit is mango.”
2. Verify history panel now shows the turn.

Expected:
- The history list clearly grew (user + assistant).

### 6.2 “Use Conversation History” = ON
1. Toggle **Use Conversation History** = ON.
2. Send:
   - “What is my favorite fruit?”

Expected:
- Model answers “mango” (or close).

### 6.3 “Use Conversation History” = OFF (UI-only suppression)
1. Toggle **Use Conversation History** = OFF.
2. Send the exact same question:
   - “What is my favorite fruit?”

Expected:
- Model should *not* reliably know (it may guess).
- **Important:** history panel should still show the new turn *stored* after the request.

If you also display token counts:
- Input tokens should drop noticeably compared to the “history ON” case.

---

## 7) API Variant regression: Chat Completions vs Responses

1. In `OpenAIClientData`, switch ApiVariant:
   - from **Responses** → **Chat Completions**
2. Rebuild client.
3. Ping.
4. Send a prompt.

Expected:
- Still works.
- Token usage fields populate (depending on endpoint response).

Then:
1. Switch back to **Responses**
2. Rebuild
3. Ping

Expected:
- Works and does not return `unknown_parameter` errors.

---

## 8) Token usage parsing regression (cached + reasoning)

Goal: ensure these fields never crash your tooling, even if 0.

1. Make a request.
2. Confirm your UI shows:
   - InputTokens >= 0
   - CachedInputTokens >= 0 and <= InputTokens
   - OutputTokens >= 0
   - ReasoningTokens >= 0

Expected:
- No negative values.
- No exceptions even when “details” fields are missing.

---

## 9) Pricing estimate sanity checks

### 9.1 Per-client rates
1. In `OpenAIClientData` (or underlying `LLMClientData`), set pricing:
   - input / cached input / output USD per 1M
2. Make a request and confirm cost estimate displays.

Expected:
- Total estimate > 0 for non-trivial usage.
- If cached tokens are 0, cached cost is 0.

### 9.2 Catalog rates
1. Create a `LLMModelPricingCatalogSO`.
2. Apply OpenAI defaults.
3. Make sure the wizard is pointing to the catalog (if supported in your v0 UI).
4. Make a request.

Expected:
- Wizard resolves an entry for (provider, model, tier) and estimates cost.

---

## 10) Error handling tests

### 10.1 Missing key
1. Temporarily remove/rename `.env` or clear `OPENAI_API_KEY`.
2. Restart Unity (or reload env loader).
3. Ping in wizard.

Expected:
- Clean failure message.
- Busy state clears (no stuck UI).

### 10.2 Bad model id
1. Set an invalid model id.
2. Rebuild + send.

Expected:
- Error returned from API is surfaced in UI/log.
- No exceptions.

### 10.3 Offline
1. Disable network or block `api.openai.com`.
2. Ping.

Expected:
- Handled exception; UI recovers.

---

## 11) Persistence tests (editor-only)

1. Edit instructions text in the wizard.
2. Close the wizard window.
3. Reopen and reselect the same agent.

Expected:
- The instruction changes persist (SO was marked dirty + saved).

Same for:
- client config values you edit in the UI.

---

## 12) Quick pass criteria

You can consider v0 “green” if:
- Ping works on both endpoints
- history is always stored and the “Use History” toggle correctly affects context usage
- token usage doesn’t crash and clamps cached ≤ input
- cost estimate is stable (even if approximate)
- no stuck busy state on errors
