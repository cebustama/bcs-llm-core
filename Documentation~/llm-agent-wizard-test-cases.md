# LLMAgentWizardWindow v0.1 — Manual Test Cases — UPDATED 2026-01-07

These test cases validate:
- env setup (no secrets in assets)
- agent loading + client rebuild
- Chat Completions vs Responses
- history storage vs “Use Conversation History (in request)”
- token usage parsing + cost estimate
- **Files (PDF)** upload + attach to Responses request
- error handling + regressions

---

## 0) One-time setup prerequisites

1. Install dependencies:
   - `com.unity.nuget.newtonsoft-json`
   - LLM Core package

2. Ensure `.env` is gitignored.

3. Create or confirm env key:
   - Run `Tools → LLM → Env Setup`
   - Set `OPENAI_API_KEY` in `.env`

4. Create assets:
   - `OpenAIClientData` (two variants recommended):
     - A) ApiVariant = Chat
     - B) ApiVariant = Responses
   - (Optional) `LLMAgentInstructionsData`
   - `LLMAgentData` referencing the chosen `OpenAIClientData`

5. Have a PDF available locally for testing.

---

## 1) Open Wizard
1. Open `Tools → LLM → Agent Wizard (v0)` (menu path may vary).
2. Confirm UI loads with no exceptions.

Expected:
- No compilation errors
- Wizard shows status “Ready” or similar

---

## 2) Assign agent & rebuild client
1. Assign an `LLMAgentData` in the wizard.
2. Click **Rebuild Client**.

Expected:
- Status changes to “Client rebuilt.”
- No exceptions in Console

---

## 3) Ping (sanity check)
1. Click **Ping**.

Expected:
- Status changes to “Ping OK” (or similar)
- Response contains a short acknowledgment

---

## 4) Text-only prompt (Chat variant)
1. Set ClientData ApiVariant = **Chat** (or select the agent that uses it).
2. Rebuild client.
3. Enter prompt: `Reply with: ok`
4. Click **Send Prompt**.

Expected:
- Response is “ok”
- History adds user + assistant turn

---

## 5) Text-only prompt (Responses variant)
1. Switch to agent/client configured with ApiVariant = **Responses**.
2. Rebuild client.
3. Enter prompt: `Reply with: ok`
4. Click **Send Prompt**.

Expected:
- Response is “ok”
- No `unknown_parameter` error

---

## 6) History policy behavior
### 6.1 Use History ON
1. Ensure **Use Conversation History (in request)** is ON.
2. Send:
   - Prompt A: `Remember the number 7. Reply ONLY 'ok'.`
   - Prompt B: `What number did I tell you to remember? Reply with the number only.`
Expected:
- Model replies `7` (history was available)

### 6.2 Use History OFF
1. Toggle **Use Conversation History (in request)** OFF.
2. Send:
   - Prompt A: `Remember the number 9. Reply ONLY 'ok'.`
   - Prompt B: `What number did I tell you to remember? Reply with the number only.`
Expected:
- Prompt B likely fails (history not included)
- BUT the wizard’s local history still contains both turns

---

## 7) Token usage parsing + clamp
1. Send a short prompt.
2. Confirm usage fields populate without exceptions:
   - InputTokens, CachedInputTokens, OutputTokens, ReasoningTokens
3. Confirm rule: `CachedInputTokens <= InputTokens` always holds.

Expected:
- No negative values
- Cached clamped if provider returns inconsistent data

---

## 8) Pricing estimate (optional)
1. Enable “Estimate Cost”
2. Assign a Pricing Catalog (or rely on ClientData pricing fields)
3. Send a prompt

Expected:
- A cost estimate appears and does not crash
- Estimate updates based on usage values

---

## 9) Error handling — missing key
1. Temporarily remove/rename `OPENAI_API_KEY` from `.env` (or set empty).
2. Rebuild client.
3. Send a prompt.

Expected:
- Request fails gracefully
- Busy flag resets
- Status shows an error message
- Console contains provider error

Restore the key afterwards.

---

# Files (PDF) tests

## 10) Upload PDF → file_id
Preconditions:
- Client is rebuilt
- Any ApiVariant is OK for upload (but you’ll attach only in Responses)

Steps:
1. Open **Files (PDF Upload)** panel.
2. Click **Browse…** and select a `.pdf`.
3. Click **Upload PDF → file_id**.

Expected:
- Status: “Uploaded …”
- A non-empty `file_id` is shown
- Copy button copies the `file_id` to clipboard
- Non-PDF files are rejected

---

## 11) Attach PDF to a Responses request (positive test)
Preconditions:
- Agent uses ApiVariant = **Responses**
- You have a valid `file_id` from §10
- “Attach last uploaded PDF” toggle is enabled (if present)

Steps:
1. Switch to Responses agent and **Rebuild Client**.
2. Ensure attach toggle is ON and shows the `file_id`.
3. Send prompt:
   - `From the attached PDF, list the species covered. Reply as a comma-separated list.`
4. Send prompt:
   - `From the attached PDF, quote the exact title of the document (short).`

Expected:
- Answers should clearly depend on PDF content
- No `unknown_parameter` errors
- Response should not look like a generic guess

---

## 12) Attach PDF — negative tests

### 12.1 Attach ON but no file_id
1. Clear last upload or restart wizard.
2. Ensure attach toggle is ON (if available).
3. Send prompt.

Expected:
- UI disables attach toggle OR soft-warns
- Request is sent text-only (no crash)

### 12.2 ApiVariant != Responses
1. Switch to Chat agent.
2. Rebuild client.
3. Attempt to attach file and send prompt.

Expected:
- Wizard disables attach OR falls back to text-only with a warning

### 12.3 Regression: null-field serialization
This is the historical failure:
- `Unknown parameter: input[0].content[0].text` (or similar)
Cause:
- file-part DTO serialized `text: null` on `input_file` part.

Steps:
1. Attach PDF (Responses).
2. Send a prompt.

Expected:
- No unknown parameter errors.
If error reappears:
- Verify JSON serialization ignores nulls for file-part requests.

---

## 13) Endpoint edits require rebuild
1. Change BaseUrl or endpoint strings in the ClientData asset.
2. Send a prompt without rebuilding.

Expected:
- Behavior does NOT change until you click **Rebuild Client**.
3. Rebuild client and retry.

Expected:
- New endpoints/base URL now apply.

---

## 14) Quick pass criteria
Consider v0.1 “green” if:
- Ping works
- Chat text-only works
- Responses text-only works
- History policy behaves as expected (store always; optionally exclude from request)
- Usage parsing does not crash; cached ≤ input
- Cost estimate doesn’t crash (even if approximate)
- PDF upload returns a valid `file_id`
- Responses + attach reads the PDF (answers depend on file)
- No stuck busy state on failures
