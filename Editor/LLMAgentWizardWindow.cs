#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BCS.LLM.Core.Agents;
using BCS.LLM.Core.Clients;
using BCS.LLM.Core.OpenAI;
using BCS.LLM.Core.Pricing;
using UnityEditor;
using UnityEngine;

namespace BCS.LLM.Core.Editor
{
    public class LLMAgentWizardWindow : EditorWindow
    {
        // -------------------------
        // Menu
        // -------------------------
        [MenuItem("Tools/LLM/Agent Wizard (v0)")]
        public static void ShowWindow()
        {
            var w = GetWindow<LLMAgentWizardWindow>();
            w.titleContent = new GUIContent("LLM Agent Wizard (v0)");
            w.minSize = new Vector2(520, 640);
            w.Show();
        }

        // -------------------------
        // State
        // -------------------------
        private LLMAgentData _agent;
        private ILLMClient _client;

        private bool _busy;
        private string _status;

        private Vector2 _mainScroll;
        private Vector2 _historyScroll;

        // Foildouts
        private bool _foldAgent = true;
        private bool _foldInstructions = true;
        private bool _foldClient = true;
        private bool _foldConsole = true;
        private bool _foldHistory = true;
        private bool _foldFiles = true;

        // Instructions
        private bool _useAgentInstructionsAsset = true;
        private bool _overrideInstructions;
        private string _instructionsOverride;

        // Console
        private string _prompt = "Hi";
        private string _response;
        private bool _useConversationHistoryInRequest = true;
        private bool _attachLastUploadedPdfToRequest = true;

        // Usage
        private LLMCompletionResult _lastResult;
        private DateTime _lastRequestUtc;
        private bool _estimateCost = true;

        // Pricing catalog (optional; preferred over per-client fields)
        private LLMModelPricingCatalogSO _pricingCatalog;
        private LLMModelPricingCatalogSO.ServiceTier _pricingTier = LLMModelPricingCatalogSO.ServiceTier.Standard;
        private bool _treatReasoningAsOutput = true;

        // Files UI state
        private string _pdfPath;
        private string _uploadPurpose = "user_data";
        private LLMFileUploadResult _lastUpload;

        private void OnEnable()
        {
            _status = "Ready.";
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                DrawHeader();

                _mainScroll = EditorGUILayout.BeginScrollView(_mainScroll);

                DrawAgentPanel();
                EditorGUILayout.Space(6);

                DrawInstructionsPanel();
                EditorGUILayout.Space(6);

                DrawClientPanel();
                EditorGUILayout.Space(6);

                DrawFilesPanel();
                EditorGUILayout.Space(6);

                DrawConsolePanel();
                EditorGUILayout.Space(6);

                DrawHistoryPanel();

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("LLM Agent Wizard (v0)", EditorStyles.boldLabel);

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(_busy))
                {
                    if (GUILayout.Button("Repaint", GUILayout.Width(80)))
                        Repaint();
                }
            }

            if (!string.IsNullOrWhiteSpace(_status))
                EditorGUILayout.HelpBox(_status, MessageType.Info);
        }

        private void DrawAgentPanel()
        {
            _foldAgent = EditorGUILayout.BeginFoldoutHeaderGroup(_foldAgent, "1) Agent");
            if (!_foldAgent)
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }

            EditorGUI.BeginDisabledGroup(_busy);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();
                var newAgent = (LLMAgentData)EditorGUILayout.ObjectField("Agent", _agent, typeof(LLMAgentData), false);
                if (EditorGUI.EndChangeCheck())
                {
                    _agent = newAgent;
                    _status = _agent != null ? $"Agent set: {_agent.name}" : "No agent selected.";
                    // Don't auto-rebuild: keep user in control.
                    _client = null;
                    _lastResult = null;
                    _response = "";
                }

                EditorGUILayout.Space(4);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(_agent == null))
                    {
                        if (GUILayout.Button("Rebuild Client", GUILayout.Height(22)))
                            RebuildClient();

                        if (GUILayout.Button("Select Asset", GUILayout.Height(22), GUILayout.Width(120)))
                        {
                            if (_agent != null)
                            {
                                Selection.activeObject = _agent;
                                EditorGUIUtility.PingObject(_agent);
                            }
                        }
                    }
                }

                if (_agent == null)
                {
                    EditorGUILayout.HelpBox("Assign an LLMAgentData asset to begin.", MessageType.Info);
                }
                else
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.TextField("Client Data", _agent.LlmClientData ? _agent.LlmClientData.name : "(none)");
                        EditorGUILayout.TextField("Instructions Data", _agent.AgentInstructionsData ? _agent.AgentInstructionsData.name : "(none)");
                    }
                }
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawInstructionsPanel()
        {
            _foldInstructions = EditorGUILayout.BeginFoldoutHeaderGroup(_foldInstructions, "2) Instructions");
            if (!_foldInstructions)
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }

            EditorGUI.BeginDisabledGroup(_busy);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_agent == null)
                {
                    EditorGUILayout.HelpBox("Assign an agent first.", MessageType.Info);
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndFoldoutHeaderGroup();
                    return;
                }

                _useAgentInstructionsAsset = EditorGUILayout.ToggleLeft(
                    new GUIContent("Use Agent Instructions Asset", "When ON: uses LLMAgentInstructionsData.InstructionsText (if assigned)."),
                    _useAgentInstructionsAsset);

                _overrideInstructions = EditorGUILayout.ToggleLeft(
                    new GUIContent("Override Instructions", "When ON: uses the override text below instead of agent/client instructions."),
                    _overrideInstructions);

                if (_overrideInstructions)
                {
                    EditorGUILayout.LabelField("Instructions Override");
                    _instructionsOverride = EditorGUILayout.TextArea(_instructionsOverride ?? "", GUILayout.MinHeight(70));
                }
                else
                {
                    var instr = GetEffectiveInstructions();
                    EditorGUILayout.LabelField("Effective Instructions Preview", EditorStyles.boldLabel);
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.TextArea(instr ?? "", GUILayout.MinHeight(90));
                    }

                    if (_useAgentInstructionsAsset && _agent.AgentInstructionsData == null)
                        EditorGUILayout.HelpBox("Agent has no InstructionsData assigned (will fall back to ClientData.SystemInstructions).", MessageType.Warning);
                }

                // Inline edit agent instructions asset if present
                if (_agent.AgentInstructionsData != null)
                {
                    EditorGUILayout.Space(8);
                    EditorGUILayout.LabelField("Edit Agent Instructions Asset", EditorStyles.boldLabel);

                    EditorGUI.BeginChangeCheck();
                    var txt = EditorGUILayout.TextArea(_agent.AgentInstructionsData.InstructionsText ?? "", GUILayout.MinHeight(90));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_agent.AgentInstructionsData, "Edit Agent Instructions");
                        _agent.AgentInstructionsData.InstructionsText = txt;
                        EditorUtility.SetDirty(_agent.AgentInstructionsData);
                    }
                }
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawClientPanel()
        {
            _foldClient = EditorGUILayout.BeginFoldoutHeaderGroup(_foldClient, "3) Client Config");
            if (!_foldClient)
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }

            EditorGUI.BeginDisabledGroup(_busy);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_agent == null)
                {
                    EditorGUILayout.HelpBox("Assign an agent first.", MessageType.Info);
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndFoldoutHeaderGroup();
                    return;
                }

                EditorGUI.BeginChangeCheck();
                var newClientData = (LLMClientData)EditorGUILayout.ObjectField(
                    "Client Data",
                    _agent.LlmClientData,
                    typeof(LLMClientData),
                    false);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_agent, "Assign Client Data");
                    _agent.LlmClientData = newClientData;
                    EditorUtility.SetDirty(_agent);
                    _status = "ClientData changed. Rebuild client if needed.";
                }

                var cd = _agent.LlmClientData;
                if (cd == null)
                {
                    EditorGUILayout.HelpBox("No client data assigned.", MessageType.Warning);
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndFoldoutHeaderGroup();
                    return;
                }

                // OpenAI-specific
                if (cd is OpenAIClientData oai)
                {
                    EditorGUILayout.LabelField("OpenAI", EditorStyles.boldLabel);

                    EditorGUI.BeginChangeCheck();

                    var apiVariant = (OpenAIClientData.OpenAIApiVariant)EditorGUILayout.EnumPopup("API Variant", oai.ApiVariant);
                    var model = (OpenAIClientData.OpenAIModel)EditorGUILayout.EnumPopup("Model", oai.selectedModel);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(oai, "Edit OpenAI Config");
                        oai.ApiVariant = apiVariant;
                        oai.selectedModel = model;
                        EditorUtility.SetDirty(oai);
                    }

                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.TextField("Model String", oai.ModelString);
                        EditorGUILayout.TextField("Base URL", oai.BaseUrl);
                        EditorGUILayout.TextField("Chat Endpoint", oai.ChatEndpoint);
                        EditorGUILayout.TextField("Responses Endpoint", oai.ResponsesEndpoint);
                        EditorGUILayout.Toggle("API Key Present", !string.IsNullOrWhiteSpace(oai.ApiKey));
                    }

                    EditorGUILayout.Space(8);
                }

                // Common knobs
                EditorGUILayout.LabelField("Sampling", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();

                float temp = EditorGUILayout.Slider("Temperature", cd.Temperature, 0f, 2f);
                float topP = EditorGUILayout.Slider("Top P", cd.TopP, 0f, 1f);
                int maxOut = EditorGUILayout.IntField("Max Output Tokens", cd.MaxOutputTokens);
                float freq = EditorGUILayout.Slider("Frequency Penalty", cd.FrequencyPenalty, -2f, 2f);

                EditorGUILayout.Space(4);
                DrawStopSequencesList(cd);

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("System Instructions (ClientData)", EditorStyles.boldLabel);
                string sysInstr = EditorGUILayout.TextArea(cd.SystemInstructions ?? "", GUILayout.MinHeight(70));

                // These are still useful as a fallback (or quick testing) even if you use a catalog.
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Pricing (USD per 1M tokens) — fallback", EditorStyles.boldLabel);
                float inCost = EditorGUILayout.FloatField("Input", cd.InputUSDPerMTokens);
                float cachedInCost = EditorGUILayout.FloatField("Cached Input", cd.CachedInputUSDPerMTokens);
                float outCost = EditorGUILayout.FloatField("Output", cd.OutputUSDPerMTokens);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(cd, "Edit Client Config");
                    cd.Temperature = temp;
                    cd.TopP = topP;
                    cd.MaxOutputTokens = Mathf.Max(1, maxOut);
                    cd.FrequencyPenalty = freq;
                    cd.SystemInstructions = sysInstr;
                    cd.InputUSDPerMTokens = Mathf.Max(0f, inCost);
                    cd.CachedInputUSDPerMTokens = Mathf.Max(0f, cachedInCost);
                    cd.OutputUSDPerMTokens = Mathf.Max(0f, outCost);

                    EditorUtility.SetDirty(cd);

                    // Push changes into the live client where possible
                    ApplyClientDataToRuntimeClient();
                }

                EditorGUILayout.Space(8);
                if (GUILayout.Button("Apply Config To Runtime Client", GUILayout.Height(22)))
                {
                    ApplyClientDataToRuntimeClient();
                    _status = "Applied ClientData to runtime client.";
                }

                if (_client == null)
                    EditorGUILayout.HelpBox("No runtime client yet. Click Rebuild Client in the Agent panel.", MessageType.Info);
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawFilesPanel()
        {
            _foldFiles = 
                EditorGUILayout.BeginFoldoutHeaderGroup(
                    _foldFiles, "4) Files (PDF Upload)");
            if (!_foldFiles)
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_agent == null)
                {
                    EditorGUILayout.HelpBox("Assign an agent first.", MessageType.Info);
                    EditorGUILayout.EndFoldoutHeaderGroup();
                    return;
                }

                if (_client == null)
                {
                    EditorGUILayout.HelpBox("Client is not built. " +
                        "Click 'Rebuild Client' first.", MessageType.Warning);
                    EditorGUILayout.EndFoldoutHeaderGroup();
                    return;
                }

                var fileClient = _client as ILLMFileClient;
                if (fileClient == null)
                {
                    EditorGUILayout.HelpBox(
                        "This client does not support file uploads " +
                        "(ILLMFileClient not implemented).",
                        MessageType.Info);
                    EditorGUILayout.EndFoldoutHeaderGroup();
                    return;
                }

                // Default purpose from agent (config-only)
                if (string.IsNullOrWhiteSpace(_uploadPurpose))
                    _uploadPurpose = !string.IsNullOrWhiteSpace(_agent.DefaultUploadPurpose)
                        ? _agent.DefaultUploadPurpose
                        : "user_data";

                using (new EditorGUI.DisabledScope(_busy))
                {
                    EditorGUILayout.LabelField("PDF", EditorStyles.boldLabel);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUI.DisabledScope(true))
                            EditorGUILayout.TextField("Path", _pdfPath ?? "");

                        if (GUILayout.Button("Browse...", GUILayout.Width(90)))
                        {
                            var p = EditorUtility.OpenFilePanel("Select PDF", "", "pdf");
                            if (!string.IsNullOrWhiteSpace(p))
                                _pdfPath = p;
                        }
                    }

                    _uploadPurpose = EditorGUILayout.TextField(
                        new GUIContent("Purpose", "OpenAI Files API 'purpose'. " +
                        "Default: user_data"),
                        _uploadPurpose ?? "user_data");

                    EditorGUILayout.Space(6);

                    using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_pdfPath)))
                    {
                        if (GUILayout.Button("Upload PDF → file_id", GUILayout.Height(24)))
                            _ = UploadPdfAsync(fileClient);
                    }

                    if (_lastUpload != null && !string.IsNullOrWhiteSpace(_lastUpload.FileId))
                    {
                        EditorGUILayout.Space(6);
                        EditorGUILayout.LabelField("Last Upload", EditorStyles.boldLabel);

                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUILayout.TextField("file_id", _lastUpload.FileId);
                            EditorGUILayout.TextField("filename", _lastUpload.Filename);
                            EditorGUILayout.LongField("bytes", _lastUpload.Bytes);
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("Copy file_id"))
                                EditorGUIUtility.systemCopyBuffer = _lastUpload.FileId;

                            if (GUILayout.Button("Clear"))
                                _lastUpload = null;
                        }
                    }
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawConsolePanel()
        {
            _foldConsole = EditorGUILayout.BeginFoldoutHeaderGroup(_foldConsole, "4) Test Console");
            if (!_foldConsole)
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_agent == null)
                {
                    EditorGUILayout.HelpBox("Assign an agent first.", MessageType.Info);
                    EditorGUILayout.EndFoldoutHeaderGroup();
                    return;
                }

                if (_client == null)
                    EditorGUILayout.HelpBox("No runtime client. Rebuild Client first.", MessageType.Warning);

                EditorGUI.BeginDisabledGroup(_busy || _client == null);

                // -------------------------
                // Toggles (request behavior)
                // -------------------------
                _useConversationHistoryInRequest = EditorGUILayout.ToggleLeft(
                    new GUIContent(
                        "Use Conversation History (in request)",
                        "When OFF: request is sent with empty history, but the new turn is merged back so history is still stored."),
                    _useConversationHistoryInRequest);

                // Files → attach to request (Responses only)
                bool isResponses =
                    _agent != null &&
                    _agent.LlmClientData is OpenAIClientData oai &&
                    oai.ApiVariant == OpenAIClientData.OpenAIApiVariant.Responses;

                bool hasFileId = _lastUpload != null && !string.IsNullOrWhiteSpace(_lastUpload.FileId);

                using (new EditorGUI.DisabledScope(!isResponses || !hasFileId))
                {
                    _attachLastUploadedPdfToRequest = EditorGUILayout.ToggleLeft(
                        new GUIContent(
                            "Attach last uploaded PDF (file_id) to request (Responses only)",
                            "When ON and a file_id exists, the next Send Prompt will include it in the Responses request.\n" +
                            "When OFF, prompt is sent as plain text."),
                        _attachLastUploadedPdfToRequest);
                }

                if (!isResponses)
                {
                    EditorGUILayout.HelpBox(
                        "File attachments are only supported in this tool when the client uses OpenAI 'Responses' variant. " +
                        "Switch the ClientData ApiVariant to Responses to enable attachment.",
                        MessageType.Info);
                }
                else if (!hasFileId)
                {
                    EditorGUILayout.HelpBox(
                        "No uploaded PDF available. Upload a PDF in the 'Files (PDF Upload)' panel to enable attachment.",
                        MessageType.Info);
                }
                else if (_attachLastUploadedPdfToRequest)
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.TextField("Attached file_id", _lastUpload.FileId);
                    }
                }

                // Instructions source
                _useAgentInstructionsAsset = EditorGUILayout.ToggleLeft(
                    new GUIContent(
                        "Use Agent Instructions Asset",
                        "When ON: uses LLMAgentInstructionsData.InstructionsText (if assigned)."),
                    _useAgentInstructionsAsset);

                _overrideInstructions = EditorGUILayout.ToggleLeft(
                    new GUIContent(
                        "Override Instructions",
                        "When ON: uses the override text below instead of agent/client instructions."),
                    _overrideInstructions);

                if (_overrideInstructions)
                {
                    EditorGUILayout.LabelField("Instructions Override");
                    _instructionsOverride = EditorGUILayout.TextArea(_instructionsOverride ?? "", GUILayout.MinHeight(70));
                }

                // -------------------------
                // Cost estimation
                // -------------------------
                _estimateCost = EditorGUILayout.ToggleLeft("Estimate Cost", _estimateCost);

                if (_estimateCost)
                {
                    _pricingCatalog = (LLMModelPricingCatalogSO)EditorGUILayout.ObjectField(
                        new GUIContent(
                            "Pricing Catalog (optional)",
                            "If set, rates are read from the catalog (preferred). Otherwise falls back to ClientData pricing fields."),
                        _pricingCatalog,
                        typeof(LLMModelPricingCatalogSO),
                        false);

                    _pricingTier = (LLMModelPricingCatalogSO.ServiceTier)EditorGUILayout.EnumPopup(
                        new GUIContent("Pricing Tier", "Service tier used when looking up the model in the catalog."),
                        _pricingTier);

                    _treatReasoningAsOutput = EditorGUILayout.ToggleLeft(
                        new GUIContent(
                            "Treat reasoning tokens as output",
                            "If ON: reasoning tokens are added to output tokens for cost estimation (common billing behavior)."),
                        _treatReasoningAsOutput);
                }

                EditorGUILayout.Space(6);

                // -------------------------
                // Ping
                // -------------------------
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Ping", GUILayout.Height(24), GUILayout.Width(110)))
                        PingAsync();

                    GUILayout.FlexibleSpace();
                }

                EditorGUILayout.Space(6);

                // -------------------------
                // Prompt
                // -------------------------
                EditorGUILayout.LabelField("Prompt", EditorStyles.boldLabel);
                _prompt = EditorGUILayout.TextArea(_prompt ?? "", GUILayout.MinHeight(120));

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Send Prompt", GUILayout.Height(26)))
                        SendPromptAsync();

                    if (GUILayout.Button("Clear Response", GUILayout.Height(26), GUILayout.Width(120)))
                    {
                        _response = "";
                        _lastResult = null;
                    }
                }

                EditorGUI.EndDisabledGroup();

                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Response", EditorStyles.boldLabel);

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextArea(_response ?? "", GUILayout.MinHeight(160));
                }

                DrawUsageAndCost();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }


        private void DrawHistoryPanel()
        {
            _foldHistory = EditorGUILayout.BeginFoldoutHeaderGroup(_foldHistory, "5) History");
            if (!_foldHistory)
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_client == null)
                {
                    EditorGUILayout.HelpBox("No runtime client.", MessageType.Info);
                    EditorGUILayout.EndFoldoutHeaderGroup();
                    return;
                }

                var formatted = _client.GetFormattedConversationHistory() ?? new List<KeyValuePair<string, string>>();
                EditorGUILayout.LabelField($"Stored Turns: {formatted.Count}", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Clear History", GUILayout.Height(22)))
                    {
                        _client.ClearHistory();
                        _status = "History cleared.";
                    }

                    if (GUILayout.Button("Copy History", GUILayout.Height(22), GUILayout.Width(120)))
                    {
                        var txt = string.Join("\n\n", formatted.Select(kv => $"{kv.Key}:\n{kv.Value}"));
                        EditorGUIUtility.systemCopyBuffer = txt;
                        _status = "History copied to clipboard.";
                    }
                }

                EditorGUILayout.Space(6);

                _historyScroll = EditorGUILayout.BeginScrollView(_historyScroll, GUILayout.MinHeight(220));
                for (int i = 0; i < formatted.Count; i++)
                {
                    var kv = formatted[i];
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField($"{i + 1}. {kv.Key}", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(kv.Value ?? "", EditorStyles.wordWrappedLabel);
                    }
                }
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // -------------------------
        // Actions
        // -------------------------
        private void RebuildClient()
        {
            if (_agent == null)
            {
                _status = "No agent selected.";
                return;
            }

            if (_agent.LlmClientData == null)
            {
                _status = "Agent has no ClientData assigned.";
                return;
            }

            _client = LLMClientFactory.CreateClient(_agent.LlmClientData);
            ApplyClientDataToRuntimeClient();

            _status = _client != null
                ? "Client rebuilt."
                : "Client rebuild failed (see Console for errors).";
        }

        private void ApplyClientDataToRuntimeClient()
        {
            if (_client == null || _agent == null || _agent.LlmClientData == null)
                return;

            var cd = _agent.LlmClientData;

            // Push common config into runtime client.
            _client.Model = cd.ModelString;
            _client.Temperature = cd.Temperature;
            _client.MaxOutputTokens = cd.MaxOutputTokens;
            _client.TopP = cd.TopP;
            _client.FrequencyPenalty = cd.FrequencyPenalty;
            _client.StopSequences = cd.StopSequences ?? new List<string>();

            _client.SystemInstructions = cd.SystemInstructions ?? "";

            // Fallback rates (catalog overrides these in the tool if present)
            _client.InputUSDPerMTokens = cd.InputUSDPerMTokens;
            _client.CachedInputUSDPerMTokens = cd.CachedInputUSDPerMTokens;
            _client.OutputUSDPerMTokens = cd.OutputUSDPerMTokens;
        }

        private string GetEffectiveInstructions()
        {
            if (_overrideInstructions)
                return _instructionsOverride ?? "";

            if (_useAgentInstructionsAsset && _agent != null && _agent.AgentInstructionsData != null)
                return _agent.AgentInstructionsData.InstructionsText ?? "";

            // Fallback to ClientData.SystemInstructions
            if (_agent != null && _agent.LlmClientData != null)
                return _agent.LlmClientData.SystemInstructions ?? "";

            return "";
        }

        private async void SendPromptAsync()
        {
            if (_busy || _client == null) return;

            var prompt = (_prompt ?? "").Trim();
            if (string.IsNullOrWhiteSpace(prompt))
            {
                _status = "Prompt is empty.";
                return;
            }

            // Determine file_ids to attach (if any)
            IReadOnlyList<string> fileIds = null;

            bool isResponses =
                _agent != null &&
                _agent.LlmClientData is OpenAIClientData oai &&
                oai.ApiVariant == OpenAIClientData.OpenAIApiVariant.Responses;

            if (isResponses &&
                _attachLastUploadedPdfToRequest &&
                _lastUpload != null &&
                !string.IsNullOrWhiteSpace(_lastUpload.FileId))
            {
                fileIds = new List<string> { _lastUpload.FileId };
            }

            _busy = true;
            _status = (fileIds != null && fileIds.Count > 0) ? "Sending (with PDF)..." : "Sending...";
            Repaint();

            var instructions = GetEffectiveInstructions();

            LLMCompletionResult result = null;
            Exception error = null;

            try
            {
                result = await ExecuteWithHistoryPolicyAsync(
                    _client,
                    prompt,
                    instructions,
                    includeHistoryInRequest: _useConversationHistoryInRequest,
                    mergeNewTurnBackWhenSuppressed: true,
                    fileIds: fileIds);
            }
            catch (Exception ex)
            {
                error = ex;
            }

            var doneResult = result;
            var doneError = error;

            EditorApplication.delayCall += () =>
            {
                _busy = false;

                if (doneError != null)
                {
                    _status = $"Error: {doneError.Message}";
                }
                else
                {
                    _lastResult = doneResult;
                    _lastRequestUtc = DateTime.UtcNow;

                    _response = doneResult?.OutputText ?? "";
                    _status = string.IsNullOrWhiteSpace(doneResult?.OutputText)
                        ? "Request completed (empty response). Check Console logs for API errors."
                        : "Request completed.";
                }

                Repaint();
            };
        }


        private async void PingAsync()
        {
            if (_busy) return;

            if (_client == null)
                RebuildClient();

            if (_client == null)
            {
                _status = "No runtime client.";
                return;
            }

            _busy = true;
            _status = "Pinging...";
            Repaint();

            // Save current runtime settings
            int oldMax = _client.MaxOutputTokens;
            float oldTemp = _client.Temperature;

            // Responses requires max_output_tokens >= 16 (Chat Completions can go lower)
            int pingMax = 10;
            if (_agent != null &&
                _agent.LlmClientData is OpenAIClientData oai &&
                oai.ApiVariant == OpenAIClientData.OpenAIApiVariant.Responses)
            {
                pingMax = 16;
            }

            _client.MaxOutputTokens = pingMax;
            _client.Temperature = 0f;

            const string pingPrompt = "Reply with exactly: pong";
            const string pingInstr = "Ignore all prior instructions. Output exactly: pong";

            LLMCompletionResult result = null;
            Exception error = null;

            try
            {
                // For ping: do NOT pollute persistent history.
                result = await ExecuteWithHistoryPolicyAsync(
                    _client,
                    pingPrompt,
                    pingInstr,
                    includeHistoryInRequest: false,
                    mergeNewTurnBackWhenSuppressed: false);
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                // Always restore clamps (even if request fails)
                _client.MaxOutputTokens = oldMax;
                _client.Temperature = oldTemp;
            }

            var doneResult = result;
            var doneError = error;

            EditorApplication.delayCall += () =>
            {
                _busy = false;

                if (doneError != null)
                {
                    _status = $"Ping error: {doneError.Message}";
                }
                else
                {
                    var txt = (doneResult?.OutputText ?? "").Trim();
                    _status = string.Equals(txt, "pong", StringComparison.OrdinalIgnoreCase)
                        ? "Ping OK (pong)."
                        : $"Ping failed (got: \"{txt}\").";
                }

                Repaint();
            };
        }

        private static Task<LLMCompletionResult> ExecuteWithHistoryPolicyAsync(
    ILLMClient client,
    string prompt,
    string instructions,
    bool includeHistoryInRequest,
    bool mergeNewTurnBackWhenSuppressed)
        {
            return ExecuteWithHistoryPolicyAsync(
                client,
                prompt,
                instructions,
                includeHistoryInRequest,
                mergeNewTurnBackWhenSuppressed,
                fileIds: null);
        }

        private static async Task<LLMCompletionResult> ExecuteWithHistoryPolicyAsync(
            ILLMClient client,
            string prompt,
            string instructions,
            bool includeHistoryInRequest,
            bool mergeNewTurnBackWhenSuppressed,
            IReadOnlyList<string> fileIds)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (includeHistoryInRequest)
                return await CreateChatCompletionMaybeWithFilesAsync(client, prompt, instructions, fileIds);

            // Snapshot current history
            var snapshot = CloneHistory(client.ClientConversationHistory);

            // Swap in empty history (so request contains no prior messages)
            client.ClientConversationHistory = new List<ChatMessage>();

            // Execute (client will append the new turn to the now-empty list)
            var result = await CreateChatCompletionMaybeWithFilesAsync(client, prompt, instructions, fileIds);

            // Capture new turn and restore
            var newTurn = CloneHistory(client.ClientConversationHistory);

            client.ClientConversationHistory = snapshot;

            if (mergeNewTurnBackWhenSuppressed && newTurn.Count > 0)
                client.ClientConversationHistory.AddRange(newTurn);

            return result;
        }

        private static Task<LLMCompletionResult> CreateChatCompletionMaybeWithFilesAsync(
            ILLMClient client,
            string prompt,
            string instructions,
            IReadOnlyList<string> fileIds)
        {
            // Default behavior: text-only
            if (fileIds == null || fileIds.Count == 0)
                return client.CreateChatCompletionAsync(prompt, instructions);

            if (client is ILLMResponsesFileClient responsesFileClient)
                return responsesFileClient.CreateResponseWithFilesAsync(
                    prompt, instructions, fileIds);

            // Try to call a 3-arg overload if the concrete client provides it:
            // CreateChatCompletionAsync(string prompt, string instructions, IReadOnlyList<string> fileIds)
            var mi = client.GetType().GetMethod(
                "CreateChatCompletionAsync",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(string), typeof(string), typeof(IReadOnlyList<string>) },
                modifiers: null);

            if (mi != null)
                return (Task<LLMCompletionResult>)mi.Invoke(client, new object[] { prompt, instructions, fileIds });

            Debug.LogWarning(
                $"Client '{client.GetType().Name}' does not expose CreateChatCompletionAsync(prompt, instructions, fileIds). " +
                "Sending text-only instead.");
            return client.CreateChatCompletionAsync(prompt, instructions);
        }


        private static List<ChatMessage> CloneHistory(List<ChatMessage> src)
        {
            if (src == null) return new List<ChatMessage>();
            var dst = new List<ChatMessage>(src.Count);
            foreach (var m in src)
            {
                if (m == null) continue;
                dst.Add(new ChatMessage { role = m.role, content = m.content });
            }
            return dst;
        }

        private async Task UploadPdfAsync(ILLMFileClient fileClient)
        {
            if (_busy) return;

            _busy = true;
            _status = "Uploading PDF...";
            Repaint();

            try
            {
                _lastUpload = await fileClient.UploadFileAsync(
                    _pdfPath,
                    string.IsNullOrWhiteSpace(_uploadPurpose) ? "user_data" : _uploadPurpose);

                _status = $"Upload OK. file_id = {_lastUpload.FileId}";
            }
            catch (Exception ex)
            {
                _status = $"Upload FAILED: {ex.Message}";
                Debug.LogException(ex);
            }
            finally
            {
                _busy = false;
                Repaint();
            }
        }


        // -------------------------
        // UI helpers
        // -------------------------
        private void DrawStopSequencesList(LLMClientData cd)
        {
            cd.StopSequences ??= new List<string>();

            EditorGUILayout.LabelField("Stop Sequences", EditorStyles.boldLabel);

            int removeIndex = -1;

            for (int i = 0; i < cd.StopSequences.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    cd.StopSequences[i] = EditorGUILayout.TextField($"#{i + 1}", cd.StopSequences[i] ?? "");
                    if (GUILayout.Button("X", GUILayout.Width(22)))
                        removeIndex = i;
                }
            }

            if (removeIndex >= 0 && removeIndex < cd.StopSequences.Count)
                cd.StopSequences.RemoveAt(removeIndex);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Stop", GUILayout.Width(120)))
                    cd.StopSequences.Add("");
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawUsageAndCost()
        {
            if (_lastResult == null) return;

            int input = Mathf.Max(0, _lastResult.InputTokens);
            int cached = Mathf.Max(0, _lastResult.CachedInputTokens);
            int output = Mathf.Max(0, _lastResult.OutputTokens);
            int reasoning = Mathf.Max(0, _lastResult.ReasoningTokens);

            if (cached > input) cached = input;
            int nonCached = input - cached;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Usage + Cost", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField("Input Tokens", input);
                    EditorGUILayout.IntField("Cached Input Tokens", cached);
                    EditorGUILayout.IntField("Non-cached Input Tokens", nonCached);
                    EditorGUILayout.IntField("Output Tokens", output);
                    EditorGUILayout.IntField("Reasoning Tokens", reasoning);

                    int total = input + output + reasoning;
                    EditorGUILayout.IntField("Total Tokens (incl. reasoning)", total);

                    EditorGUILayout.TextField("Last Request (UTC)", _lastRequestUtc == default ? "" : _lastRequestUtc.ToString("u"));
                }

                if (!_estimateCost || _client == null)
                    return;

                var usage = new LLMPricingEstimator.TokenUsage
                {
                    inputTokens = input,
                    cachedInputTokens = cached,
                    outputTokens = output,
                    reasoningTokens = reasoning
                };

                // Prefer catalog, fallback to per-client fields.
                bool usedCatalog = false;
                string pricingSource = null;

                double inputRate = 0.0;
                double cachedRate = 0.0;
                double outputRate = 0.0;

                if (_pricingCatalog != null && _agent != null && _agent.LlmClientData != null)
                {
                    string providerId = _agent.LlmClientData.Provider.ToString();
                    string modelId = _client.Model;

                    if (_pricingCatalog.TryGet(providerId, modelId, _pricingTier, out var entry))
                    {
                        inputRate = entry.inputUsdPer1M;
                        cachedRate = entry.cachedInputUsdPer1M;
                        outputRate = entry.outputUsdPer1M;

                        if (HasAnyPricing(inputRate, cachedRate, outputRate))
                        {
                            usedCatalog = true;
                            pricingSource = $"Catalog: {_pricingCatalog.name} ({providerId}/{modelId}/{_pricingTier})";
                        }
                        else
                        {
                            pricingSource = $"Catalog entry found, but rates are 0 ({providerId}/{modelId}/{_pricingTier}).";
                        }
                    }
                    else
                    {
                        pricingSource = $"Catalog has no entry for ({providerId}/{modelId}/{_pricingTier}).";
                    }
                }

                if (!usedCatalog)
                {
                    inputRate = Math.Max(0.0, _client.InputUSDPerMTokens);
                    cachedRate = Math.Max(0.0, _client.CachedInputUSDPerMTokens);
                    outputRate = Math.Max(0.0, _client.OutputUSDPerMTokens);

                    if (!HasAnyPricing(inputRate, cachedRate, outputRate))
                    {
                        EditorGUILayout.HelpBox(
                            "No pricing data available.\n\n" +
                            "To enable cost estimates:\n" +
                            "• Assign a Pricing Catalog that includes this provider/model, or\n" +
                            "• Fill the ClientData pricing fields (USD per 1M tokens).",
                            MessageType.Info);
                        return;
                    }

                    pricingSource = "ClientData pricing fields (fallback)";
                }

                var breakdown = LLMPricingEstimator.Estimate(
                    usage,
                    inputRate,
                    cachedRate,
                    outputRate,
                    treatReasoningAsOutput: _treatReasoningAsOutput);

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Pricing Source", pricingSource);
                EditorGUILayout.LabelField("Rates (USD / 1M)",
                    $"Input {LLMPricingEstimator.FormatUsd(inputRate)} | Cached {LLMPricingEstimator.FormatUsd(cachedRate)} | Output {LLMPricingEstimator.FormatUsd(outputRate)}");

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Estimated Cost (USD)", "$" + LLMPricingEstimator.FormatUsd(breakdown.totalUsd));
                EditorGUILayout.LabelField("  Non-cached input", "$" + LLMPricingEstimator.FormatUsd(breakdown.nonCachedInputUsd));
                EditorGUILayout.LabelField("  Cached input", "$" + LLMPricingEstimator.FormatUsd(breakdown.cachedInputUsd));
                EditorGUILayout.LabelField("  Output" + (_treatReasoningAsOutput ? " (incl. reasoning)" : ""), "$" + LLMPricingEstimator.FormatUsd(breakdown.outputUsd));
            }
        }

        private static bool HasAnyPricing(double inputUsdPer1M, double cachedInputUsdPer1M, double outputUsdPer1M)
            => inputUsdPer1M > 0.0 || cachedInputUsdPer1M > 0.0 || outputUsdPer1M > 0.0;
    }
}
#endif
