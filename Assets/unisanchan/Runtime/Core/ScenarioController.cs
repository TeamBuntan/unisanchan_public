using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;
using System.Linq;
using unisanchan;

public class ScenarioController : MonoBehaviour
{
    private enum ScenarioState
    {
        Normal,
        Choice
    }

    [Header("Actor")]
    [SerializeField] private ActorList actorList;
    [SerializeField] private ScenarioCameraController cameraController;

    private bool canAdvance = true;

    private ScenarioState currentState = ScenarioState.Normal;

    [Header("Dialog")]
    [SerializeField] private GameObject dialogWindow;
    [SerializeField] private TMP_Text dialogText;

    [Header("Input")]
    [SerializeField] private ScenarioInputController inputController;

    [Header("Choice")]
    [SerializeField] private ScenarioChoiceUI choiceUI;

    [SerializeField] private bool showAdvanceIndicator = true;
    [SerializeField] private GameObject advanceIndicator;

    [Header("Speaker")]
    [SerializeField] private GameObject speakerNameWindow;
    [SerializeField] private TMP_Text speakerNameText;

    [Header("Fade")]
    [SerializeField] private Image fadePanel;

    private List<ScenarioRow> rows;

    private readonly List<ScenarioEvent> scenarioEvents =
    new List<ScenarioEvent>();

    [Header("Scenario Data")]
    [SerializeField] private ScenarioRegistry scenarioRegistry;

    [Header("Preset Data")]
    [SerializeField] private PresetRegistry presetRegistry;

    [Header("Variable Data")]
    [SerializeField] private VariableRegistry variableRegistry;

    private readonly ScenarioVariableStore variableStore =
    new ScenarioVariableStore();

    private ScenarioSaveCoordinator saveCoordinator;

    private readonly ScenarioNavigator navigator =
        new ScenarioNavigator();

    private List<CameraPresetRow> cameraPresets;
    private List<ParticlePresetRow> particlePresets;
    private List<ModalPresetRow> modalPresets;

    private int currentIndex = 0;
    private string currentEventId = "";

    private Coroutine runningRoutine;
    private bool isCompletionBarrierActive = false;

    [Header("Tips")]
    [SerializeField] private ScenarioTipsUI tipsUI;

    [Header("Guide")]
    [SerializeField] private ScenarioGuideUI guideUI;

    [Header("Modal UI")]
    [SerializeField] private ScenarioModalUI modalUI;
    [SerializeField] private ImageRegistry imageRegistry;

    [Min(0f)]
    [SerializeField] private float modalCloseResumeDelay = 0.15f;

    private ModalCommandHandler modalCommandHandler;

    [Header("Voice")]
    [SerializeField] private VoiceRegistry voiceRegistry;
    [SerializeField] private AudioSource voiceAudioSource;

    private VoiceCommandHandler voiceCommandHandler;

    [Header("BGM")]
    [SerializeField] private AudioRegistry audioRegistry;
    [SerializeField] private AudioSource bgmAudioSource;

    private BgmCommandHandler bgmCommandHandler;

    [Header("Ambience")]
    [SerializeField] private AudioSource ambienceAudioSource;

    private AmbienceCommandHandler ambienceCommandHandler;

    [Header("SE")]
    [SerializeField] private GameObject seAudioHost;

    private SeCommandHandler seCommandHandler;

    [Header("Particle")]
    [SerializeField] private ParticleRegistry particleRegistry;

    private ParticleCommandHandler particleCommandHandler;

    private ScenarioCommandExecutor commandExecutor;
    private ActorCommandHandler actorCommandHandler;
    private CameraCommandHandler cameraCommandHandler;
    private SystemCommandHandler systemCommandHandler;
    private ScenarioUICommandHandler scenarioUICommandHandler;
    private ScenarioChoiceHandler scenarioChoiceHandler;
    private VariableCommandHandler variableCommandHandler;

    private void Start()
    {
        rows = new List<ScenarioRow>();
        cameraPresets = new List<CameraPresetRow>();
        particlePresets = new List<ParticlePresetRow>();
        modalPresets = new List<ModalPresetRow>();

        if (!TryLoadPresetData())
        {
            enabled = false;
            return;
        }

        if (variableRegistry == null)
        {
            Debug.LogError(
                "[unisanchan] VariableRegistry が設定されていません.");

            enabled = false;
            return;
        }

        variableStore.Initialize(variableRegistry.Variables);

        saveCoordinator = new ScenarioSaveCoordinator(
            variableStore,
            variableRegistry.Variables);

        if (scenarioRegistry == null)
        {
            Debug.LogError(
                "[unisanchan] ScenarioRegistry が設定されていません。");

            enabled = false;
            return;
        }
        else
        {
            navigator.Clear();

            HashSet<string> scenarioNames = new HashSet<string>();

            foreach (ScenarioEntry entry in scenarioRegistry.Scenarios)
            {
                if (entry == null)
                {
                    Debug.LogError(
                        "[unisanchan] ScenarioRegistry に無効なScenarioが登録されています。");

                    enabled = false;
                    return;
                }

                string scenarioName =
                    string.IsNullOrWhiteSpace(entry.Name)
                        ? ""
                        : entry.Name.Trim();

                if (string.IsNullOrEmpty(scenarioName))
                {
                    Debug.LogError(
                        "[unisanchan] Scenario Name が空欄のScenarioがあります。");

                    enabled = false;
                    return;
                }

                if (!scenarioNames.Add(scenarioName))
                {
                    Debug.LogError(
                        $"[unisanchan] Scenario Name「{scenarioName}」が重複しています。");

                    enabled = false;
                    return;
                }

                if (entry.Source == null)
                {
                    Debug.LogError(
                        $"[unisanchan] Scenario「{entry.Name}」にSourceが設定されていません。");

                    enabled = false;
                    return;
                }

                ScenarioData data =
                    JsonScenarioDataLoader.Load(entry.Source);

                if (data == null)
                    continue;

                if (data.rows != null)
                {
                    List<ScenarioEvent> scenarioFileEvents =
                        new List<ScenarioEvent>();

                    if (!ScenarioStructureBuilder.TryBuild(
                            data.rows,
                            scenarioFileEvents))
                    {
                        Debug.LogError(
                            "[unisanchan] シナリオデータの検証に失敗したため、Play Modeを終了します。" +
                            "直前の構造エラーを確認してください。"
                        );

                        enabled = false;

                    #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
                    #endif

                        return;
                    }

                    rows.AddRange(data.rows);
                    scenarioEvents.AddRange(scenarioFileEvents);

                    navigator.BuildLabelTargets(
                        data.rows,
                        scenarioName
                    );
                }
            }
        }

        if (!ValidateJumpTargets())
        {
            enabled = false;
            return;
        }

        if (!ScenarioCheckpointResolver.ValidateCheckpoints(rows, navigator))
        {
            enabled = false;
            return;
        }

        if (!ValidateScenarioConditions())
        {
            enabled = false;
            return;
        }

        rows = ScenarioRowNormalizer.Normalize(rows);

        SetDialogVisible(false);
        SetSpeakerName("");
        SetCanAdvance(true);

        SetState(ScenarioState.Normal);

        actorCommandHandler =
            new ActorCommandHandler(
                actorList,
                routine => StartCoroutine(routine)
            );

        cameraCommandHandler =
            new CameraCommandHandler(
                cameraController,
                cameraPresets,
                fadePanel,
                this
            );

        systemCommandHandler =
            new SystemCommandHandler(
                JumpToLabel,
                fadePanel
            );
        
        variableCommandHandler =
            new VariableCommandHandler(
                variableStore
            );

        voiceCommandHandler =
            new VoiceCommandHandler(
                voiceAudioSource,
                voiceRegistry,
                routine => StartCoroutine(routine)
            );

        bgmCommandHandler =
            new BgmCommandHandler(
                bgmAudioSource,
                audioRegistry,
                routine => StartCoroutine(routine)
            );
        
        ambienceCommandHandler =
            new AmbienceCommandHandler(
                ambienceAudioSource,
                audioRegistry,
                routine => StartCoroutine(routine)
            );
        
        seCommandHandler =
            new SeCommandHandler(
                seAudioHost,
                audioRegistry,
                coroutine => StartCoroutine(coroutine)
            );
        
        particleCommandHandler =
            new ParticleCommandHandler(
                particleRegistry,
                particlePresets,
                actorList,
                cameraController,
                routine => StartCoroutine(routine));

        modalCommandHandler =
            new ModalCommandHandler(
                modalUI,
                imageRegistry,
                modalPresets,
                FormatScenarioText);

        scenarioChoiceHandler =
            new ScenarioChoiceHandler(
                choiceUI,
                dialogWindow,
                speakerNameWindow,
                variableStore
            );

        scenarioUICommandHandler =
            new ScenarioUICommandHandler(
                tipsUI,
                GetSpeakerDisplayName,
                SetSpeakerName,
                ApplyText,
                SetCanAdvance,
                routine => StartCoroutine(routine)
            );

        commandExecutor =
            new ScenarioCommandExecutor(
                ExecuteCommand,
                eventId => currentEventId = eventId,
                actorCommandHandler,
                cameraCommandHandler,
                systemCommandHandler,
                scenarioUICommandHandler,
                variableCommandHandler,
                voiceCommandHandler,
                bgmCommandHandler,
                ambienceCommandHandler,
                seCommandHandler,
                particleCommandHandler,
                modalCommandHandler
                );

        if (ScenarioLoadRequest.TryPeek(out GameSaveData loadData) &&
            string.Equals(
                loadData.ScenePath,
                gameObject.scene.path,
                System.StringComparison.Ordinal))
        {
            if (!ScenarioCheckpointResolver.TryResolve(
                    rows,
                    navigator,
                    loadData.CheckpointID,
                    out int resumeIndex) ||
                !saveCoordinator.TryRestoreCheckpoint(loadData))
            {
                Debug.LogError(
                    $"[unisanchan] セーブ地点から再開できません: " +
                    $"{loadData.CheckpointID}");

                ScenarioLoadRequest.Clear();
                enabled = false;
                return;
            }

            currentIndex = resumeIndex;
            ScenarioLoadRequest.Clear();
            TryStartLandingBlock();
            return;
        }

        TryStartAutoRows();
    }

    private bool TryLoadPresetData()
    {
        if (presetRegistry == null)
        {
            Debug.LogError(
                "[unisanchan] PresetRegistry が設定されていません。");

            return false;
        }

        HashSet<string> cameraNames =
            new HashSet<string>(
                System.StringComparer.OrdinalIgnoreCase);

        HashSet<string> particleNames =
            new HashSet<string>(
                System.StringComparer.OrdinalIgnoreCase);

        HashSet<string> modalPageKeys =
            new HashSet<string>(
                System.StringComparer.OrdinalIgnoreCase);

        foreach (TextAsset source in presetRegistry.Sources)
        {
            if (source == null)
            {
                Debug.LogError(
                    "[unisanchan] PresetRegistry に無効なJSONが登録されています。");

                return false;
            }

            PresetData data =
                JsonPresetDataLoader.Load(source);

            if (data == null)
                return false;

            foreach (CameraPresetRow preset
                in data.cameraPresets)
            {
                if (preset == null)
                    continue;

                preset.PresetName =
                    preset.PresetName?.Trim() ?? "";

                if (string.IsNullOrEmpty(
                        preset.PresetName))
                {
                    Debug.LogError(
                        $"[unisanchan] CameraPresetにPresetNameが空欄の行があります: {source.name}");

                    return false;
                }

                if (!cameraNames.Add(
                        preset.PresetName))
                {
                    Debug.LogError(
                        $"[unisanchan] CameraPreset「{preset.PresetName}」が重複しています。");

                    return false;
                }

                cameraPresets.Add(preset);
            }

            foreach (ParticlePresetRow preset
                in data.particlePresets)
            {
                if (preset == null)
                    continue;

                preset.PresetName =
                    preset.PresetName?.Trim() ?? "";

                if (string.IsNullOrEmpty(
                        preset.PresetName))
                {
                    Debug.LogError(
                        $"[unisanchan] ParticlePresetにPresetNameが空欄の行があります: {source.name}");

                    return false;
                }

                if (!particleNames.Add(
                        preset.PresetName))
                {
                    Debug.LogError(
                        $"[unisanchan] ParticlePreset「{preset.PresetName}」が重複しています。");

                    return false;
                }

                particlePresets.Add(preset);
            }

            foreach (ModalPresetRow preset
                in data.modalPresets)
            {
                if (preset == null)
                    continue;

                preset.PresetID =
                    preset.PresetID?.Trim() ?? "";

                preset.Page =
                    preset.Page?.Trim() ?? "";

                if (string.IsNullOrEmpty(
                        preset.PresetID))
                {
                    Debug.LogError(
                        $"[unisanchan] ModalPresetにPresetIDが空欄の行があります: {source.name}");

                    return false;
                }

                if (!int.TryParse(
                        preset.Page,
                        out int pageNumber) ||
                    pageNumber < 1)
                {
                    Debug.LogError(
                        $"[unisanchan] ModalPreset「{preset.PresetID}」のPageが不正です: {preset.Page}");

                    return false;
                }

                string modalPageKey =
                    preset.PresetID + "\n" + pageNumber;

                if (!modalPageKeys.Add(
                        modalPageKey))
                {
                    Debug.LogError(
                        $"[unisanchan] ModalPreset「{preset.PresetID}」のPage「{pageNumber}」が重複しています。");

                    return false;
                }

                modalPresets.Add(preset);
            }
        }

        modalPresets.Sort(
            (left, right) =>
            {
                int idComparison =
                    System.StringComparer
                        .OrdinalIgnoreCase
                        .Compare(
                            left.PresetID,
                            right.PresetID);

                if (idComparison != 0)
                    return idComparison;

                int.TryParse(
                    left.Page,
                    out int leftPage);

                int.TryParse(
                    right.Page,
                    out int rightPage);

                return leftPage.CompareTo(
                    rightPage);
            });

        Debug.Log(
            "[unisanchan] PresetRegistryを読み込みました。"
            + $" Camera: {cameraPresets.Count}"
            + $" / Particle: {particlePresets.Count}"
            + $" / Modal: {modalPresets.Count}");

        return true;
    }

    public void RequestInput(ScenarioInputAction action)
    {
        if (modalCommandHandler != null &&
            modalCommandHandler.IsVisible)
        {
            switch (action)
            {
                case ScenarioInputAction.Advance:
                case ScenarioInputAction.Right:
                    modalCommandHandler.NextPage();
                    break;

                case ScenarioInputAction.Left:
                    modalCommandHandler.PreviousPage();
                    break;

                case ScenarioInputAction.Cancel:
                    RequestCloseModal();
                    break;
            }

            // Modal表示中は、通常のシナリオや選択肢へ
            // 同じ入力を渡さない。
            return;
        }

        if (isCompletionBarrierActive)
        {
            return;
        }

        if (runningRoutine != null)
        {
            return;
        }

        if (!canAdvance)
        {
            return;
        }

        switch (action)
        {
            case ScenarioInputAction.Advance:
                RequestAdvance();
                break;

            case ScenarioInputAction.Cancel:
                ExecuteTriggerSequenceRow("Cancel");
                break;

            case ScenarioInputAction.Input1:
                ExecuteTriggerSequenceRow("Input1");
                break;

            case ScenarioInputAction.Input2:
                ExecuteTriggerSequenceRow("Input2");
                break;

            case ScenarioInputAction.Input3:
                ExecuteTriggerSequenceRow("Input3");
                break;

            case ScenarioInputAction.Input4:
                ExecuteTriggerSequenceRow("Input4");
                break;

            case ScenarioInputAction.Up:
                if (currentState == ScenarioState.Choice)
                {
                    scenarioChoiceHandler.MoveSelection(-1);
                }
                else
                {
                    ExecuteTriggerSequenceRow("Up");
                }
                break;

            case ScenarioInputAction.Down:
                if (currentState == ScenarioState.Choice)
                {
                    scenarioChoiceHandler.MoveSelection(1);
                }
                else
                {
                    ExecuteTriggerSequenceRow("Down");
                }
                break;

            case ScenarioInputAction.Left:
                ExecuteTriggerSequenceRow("Left");
                break;

            case ScenarioInputAction.Right:
                ExecuteTriggerSequenceRow("Right");
                break;
        }
    }

    public bool CanOpenSaveLoadMenu()
    {
        return isActiveAndEnabled
            && runningRoutine == null
            && !isCompletionBarrierActive
            && canAdvance
            && (modalCommandHandler == null ||
                !modalCommandHandler.IsVisible);
    }

    public void RequestCloseModal()
    {
        if (modalCommandHandler == null ||
            !modalCommandHandler.IsVisible)
        {
            return;
        }

        modalCommandHandler.Hide();

        if (modalCloseResumeDelay <= 0f)
        {
            TryStartLandingBlock();
            return;
        }

        runningRoutine =
            StartCoroutine(
                ResumeAfterModalRoutine());
    }

    private IEnumerator ResumeAfterModalRoutine()
    {
        // 将来Time.timeScaleを0にしてModal中のゲームを
        // 一時停止しても動作するよう、実時間で待機する。
        yield return new WaitForSecondsRealtime(
            modalCloseResumeDelay);

        runningRoutine = null;

        // 次BlockのTriggerを問わず実行する。
        TryStartLandingBlock();
    }


    public void RequestAdvance()
    {
        if (!canAdvance)
            return;

        if (currentState == ScenarioState.Choice)
        {
            ConfirmChoice();
            return;
        }

        ExecuteNextSequenceRow();
    }

    public void SaveManualSlot(int slotIndex)
    {
        if (saveCoordinator == null)
        {
            Debug.LogWarning(
                "[unisanchan] セーブ処理の初期化が完了していません。");
            return;
        }

        saveCoordinator.TrySaveManualSlot(slotIndex);
    }

    private void ConfirmChoice()
    {
        if (!scenarioChoiceHandler.TryGetSelectedTargetLabel(
                out string targetLabel))
        {
            return;
        }

        scenarioChoiceHandler.CompleteChoice(tipsUI);

        SetState(ScenarioState.Normal);

        JumpToLabel(targetLabel);

        flowChanged = false;

        TryStartLandingBlock();
    }

    private void SetCanAdvance(bool value)
    {
        canAdvance = value;
        RefreshAdvanceIndicator();
    }

    private void SetState(ScenarioState state)
    {
        currentState = state;

        if (guideUI != null)
        {
            if (currentState == ScenarioState.Choice)
            {
                guideUI.ShowText("↑↓：選択\nEnter：決定");
            }
            else
            {
                guideUI.Hide();
            }
        }
    }

    private void RefreshAdvanceIndicator()
    {
        if (advanceIndicator == null)
            return;

        bool isDialogVisible =
            dialogWindow != null &&
            dialogWindow.activeSelf;

        advanceIndicator.SetActive(
            isDialogVisible &&
            showAdvanceIndicator &&
            canAdvance);
    }

    private void ExecuteTriggerSequenceRow(string triggerName)
    {
        if (rows == null || rows.Count == 0)
            return;

        if (currentIndex >= rows.Count)
            return;

        string requestedTrigger = Normalize(triggerName);

        ScenarioRow firstChoiceRow = rows[currentIndex];

        string eventId = Normalize(firstChoiceRow.EventID);
        string blockId = Normalize(firstChoiceRow.BlockID);

        for (int i = currentIndex; i < rows.Count; i++)
        {
            ScenarioRow candidateRow = rows[i];

            if (Normalize(candidateRow.EventID) != eventId)
                break;

            if (Normalize(candidateRow.BlockID) != blockId)
                break;

            if (Normalize(candidateRow.Trigger) != requestedTrigger)
                continue;

            currentIndex = i;

            CloseDialogForNextBlock();

            List<ScenarioRow> blockRows = CollectBlockRows(candidateRow);
            runningRoutine = StartCoroutine(SequenceRoutine(blockRows));
            return;
        }
    }

    private bool TryStartLandingBlock()
    {
        if (rows == null || rows.Count == 0)
            return false;

        while (currentIndex < rows.Count &&
            IsLabelRow(rows[currentIndex]))
        {
            currentIndex++;
        }

        if (currentIndex >= rows.Count)
            return false;

        ScenarioRow firstRow = rows[currentIndex];

        // FlowControlで着地した最初のBlockは、
        // Triggerを再要求せずそのまま実行する。
        List<ScenarioRow> blockRows =
            CollectBlockRows(firstRow);

        if (blockRows == null || blockRows.Count == 0)
            return false;

        CloseDialogForNextBlock();

        runningRoutine =
            StartCoroutine(
                SequenceRoutine(blockRows)
            );

        return true;
    }
    
    private void ExecuteNextSequenceRow()
    {
        if (rows == null || rows.Count == 0)
        {
            Debug.LogWarning("Scenario rows are empty.");
            return;
        }

        if (currentIndex >= rows.Count)
        {
            Debug.Log("Scenario End");
            return;
        }

        while (currentIndex < rows.Count &&
            IsLabelRow(rows[currentIndex]))
        {
            currentIndex++;
        }

        if (currentIndex >= rows.Count)
        {
            Debug.Log("Scenario End");
            return;
        }

        ScenarioRow firstRow = rows[currentIndex];
        string trigger = Normalize(firstRow.Trigger);

        // 現在位置がAdvance系でなければ、先の行を探さず入力を無視する
        if (!string.IsNullOrEmpty(trigger) && trigger != "Advance")
            return;

        CloseDialogForNextBlock();

        List<ScenarioRow> blockRows = CollectBlockRows(firstRow);
        runningRoutine = StartCoroutine(SequenceRoutine(blockRows));
    }

    private List<ScenarioRow> CollectBlockRows(ScenarioRow firstRow)
    {
        List<ScenarioRow> blockRows = new List<ScenarioRow>();

        string eventId = Normalize(firstRow.EventID);
        string blockId = Normalize(firstRow.BlockID);
        string trigger = Normalize(firstRow.Trigger);

        while (currentIndex < rows.Count)
        {
            ScenarioRow row = rows[currentIndex];

            if (Normalize(row.EventID) != eventId)
                break;

            if (Normalize(row.BlockID) != blockId)
                break;

            if (Normalize(row.Trigger) != trigger)
                break;

            blockRows.Add(row);
            currentIndex++;
        }

        return blockRows;
    }

    private IEnumerator SequenceRoutine(List<ScenarioRow> blockRows)
    {
        if (blockRows == null || blockRows.Count == 0)
        {
            runningRoutine = null;
            yield break;
        }

        bool hasNext = false;
        float nextSeconds = 0f;
        bool hasHold = false;
        bool hasCompletionBarrier = false;
        bool hasFlowControl = false;
        ScenarioRow deferredFlowControlRow = null;

        BlockCompletionTracker completionTracker =
            new BlockCompletionTracker();

        bool blockHasCompletionBarrier = blockRows.Any(
            row => row != null &&
                CommandFlowResolver.GetFlowType(row.Command)
                    == CommandFlowType.CompletionBarrier
        );

        if (blockHasCompletionBarrier)
        {
            isCompletionBarrierActive = true;
        }

        for (int i = 0; i < blockRows.Count; i++)
        {
            ScenarioRow row = blockRows[i];

            CommandFlowType flowType =
                CommandFlowResolver.GetFlowType(row.Command);

            switch (flowType)
            {
                case CommandFlowType.Hold:
                    hasHold = true;
                    break;

                case CommandFlowType.CompletionBarrier:
                    hasCompletionBarrier = true;
                    break;
            }

            // FlowControlはBlock内のCompletionBarrier完了まで実行を保留する。
            // JumpはExcelの行順に条件を調べ、最初に成立したものを採用する。
            if (flowType == CommandFlowType.FlowControl)
            {
                if (deferredFlowControlRow == null &&
                    (Normalize(row.Command) != "Jump" ||
                    EvaluateJumpCondition(row.Arg2)))
                {
                    deferredFlowControlRow = row;
                }

                hasFlowControl = deferredFlowControlRow != null;
                continue;
            }

            commandExecutor.ExecuteRow(
                row,
                i == 0,
                completionTracker);

            // 念のため、別経路で処理位置が変更された場合は打ち切る
            if (flowChanged)
                break;

            if (Normalize(row.Command) == "Wait")
            {
                float waitDuration =
                    ParseFloat(row.Arg1, 0f);

                completionTracker.Add(
                    CreateWaitCompletionTicket(waitDuration)
                );
            }
            else if (Normalize(row.Command) == "Next")
            {
                hasNext = true;
                nextSeconds = ParseFloat(row.Arg1, 0f);
            }
        }

        if (flowChanged)
        {
            flowChanged = false;
            runningRoutine = null;

            TryStartAutoRows();
            yield break;
        }

        if (hasNext)
        {
            if (nextSeconds > 0f)
                yield return new WaitForSeconds(nextSeconds);

            runningRoutine = null;
            ExecuteNextSequenceRow();
            yield break;
        }

        while (!completionTracker.IsCompleted)
        {
            yield return null;
        }

        if (blockHasCompletionBarrier)
        {
            isCompletionBarrierActive = false;
        }

        runningRoutine = null;

        if (deferredFlowControlRow != null)
        {
            commandExecutor.ExecuteRow(deferredFlowControlRow, false);

            // BackToADVなどで、このScenarioController自身が
            // 非アクティブになった場合は以降の進行処理を行わない
            if (!isActiveAndEnabled ||
                !gameObject.activeInHierarchy)
            {
                runningRoutine = null;
                yield break;
            }

            if (flowChanged)
            {
                flowChanged = false;
                runningRoutine = null;

                TryStartLandingBlock();
                yield break;
            }
        }

        // HoldがあるBlockは、入力・選択などによる解除を待つ
        if (hasHold)
        {
            TryStartAutoRows();
            yield break;
        }

        // FlowControlは通常の次Block進行を行わない
        if (hasFlowControl)
        {
            TryStartAutoRows();
            yield break;
        }

        // CompletionBarrierを含むBlockは、
        // Ticket完了後にBlockを終了する。
        // 次BlockがAutoなら自動開始し、
        // Advance/Input系なら対応する入力を待つ。
        if (hasCompletionBarrier)
        {
            TryStartAutoRows();
            yield break;
        }

        // PassThroughのみのBlockも即座に次Blockへ進む
        if (!TryStartAutoRows())
        {
            ExecuteNextSequenceRow();
        }

        yield break;
    }

    private void ExecuteChoice(ScenarioRow row)
    {
        SetState(ScenarioState.Choice);

        scenarioChoiceHandler.BeginChoice(
            rows,
            row
        );

        SetCanAdvance(true);
    }

    private void ExecuteCommand(ScenarioRow row)
    {
        switch (Normalize(row.Command))
        {
            case "SavePoint":
                saveCoordinator.RecordCheckpoint(
                    row, gameObject.scene.path, false);
                break;

            case "AutoSave":
                saveCoordinator.RecordCheckpoint(
                    row, gameObject.scene.path, true);
                break;
                
            case "Wait":
                break;

            case "Next":
                break;

            case "Choice":
            {
                ExecuteChoice(row);
                break;
            }
            
            default:
                Debug.LogWarning($"Unknown Command: {row.Command} / EventID: {row.EventID}");
                break;

        }
    }

    private bool flowChanged = false;

    private string FormatScenarioText(string text)
    {
        return ScenarioTextTagFormatter.Format(
            text,
            inputController);
    }
            
    private void ApplyText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            SetDialogVisible(false);

            if (dialogText != null)
                dialogText.text = "";

            return;
        }

        string formattedText =
            FormatScenarioText(text);

        SetDialogVisible(true);

        if (dialogText != null)
            dialogText.text = formattedText;
    }

    private void SetDialogVisible(bool visible)
    {
        if (dialogWindow != null)
            dialogWindow.SetActive(visible);

        RefreshAdvanceIndicator();

        if (!visible)
            SetSpeakerName("");
    }

    private void CloseDialogForNextBlock()
    {
        SetDialogVisible(false);
    }

    private void SetSpeakerName(string speaker)
    {
        bool hasSpeaker = !string.IsNullOrWhiteSpace(speaker);

        if (speakerNameWindow != null)
            speakerNameWindow.SetActive(hasSpeaker);

        if (speakerNameText != null)
            speakerNameText.text = hasSpeaker ? speaker.Trim() : "";
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }

    private static bool IsLabelRow(ScenarioRow row)
    {
        if (row == null)
            return false;

        return Normalize(row.EventID).StartsWith("*");
    }

    private static string ParseSpeakerActorId(string speaker)
    {
        string normalized = Normalize(speaker);

        if (string.IsNullOrEmpty(normalized))
            return "";

        int separatorIndex = normalized.IndexOf('|');

        if (separatorIndex < 0)
            return normalized;

        return normalized
            .Substring(0, separatorIndex)
            .Trim();
    }

    private static string GetSpeakerDisplayName(string speaker)
    {
        string normalized = Normalize(speaker);

        if (string.IsNullOrEmpty(normalized))
            return "";

        int separatorIndex = normalized.IndexOf('|');

        if (separatorIndex < 0)
            return normalized;

        string displayName = normalized
            .Substring(separatorIndex + 1)
            .Trim();

        // 「ActorID|」のように表示名だけ空だった場合はActor IDを表示する
        return string.IsNullOrEmpty(displayName)
            ? ParseSpeakerActorId(normalized)
            : displayName;
    }

    private static float ParseFloat(string value, float defaultValue)
    {
        return TryParseFloat(value, out float result) ? result : defaultValue;
    }

    private CommandCompletionTicket CreateWaitCompletionTicket(float seconds)
    {
        CommandCompletionTicket ticket =
            new CommandCompletionTicket();

        StartCoroutine(
            CompleteTicketAfterDelay(
                ticket,
                seconds
            )
        );

        return ticket;
    }

    private IEnumerator CompleteTicketAfterDelay(
        CommandCompletionTicket ticket,
        float seconds)
    {
        if (seconds > 0f)
            yield return new WaitForSeconds(seconds);

        ticket?.Complete();
    }

    private static bool TryParseFloat(string value, out float result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = 0f;
            return false;
        }

        string normalized = value
            .Trim()
            .Replace("　", "")
            .Replace("，", ".")
            .Replace(",", ".")
            .Replace("０", "0")
            .Replace("１", "1")
            .Replace("２", "2")
            .Replace("３", "3")
            .Replace("４", "4")
            .Replace("５", "5")
            .Replace("６", "6")
            .Replace("７", "7")
            .Replace("８", "8")
            .Replace("９", "9");

        return float.TryParse(
            normalized,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out result
        );
    }

    private bool IsAutoExecutableRow(ScenarioRow row)
    {
        if (row == null)
            return false;

        string trigger = Normalize(row.Trigger);

        // 明示的なAutoはそのまま自動実行
        if (trigger == "Auto")
            return true;

        // Triggerが設定されているBlockは自動実行しない
        if (!string.IsNullOrEmpty(trigger))
            return false;

        string eventId = Normalize(row.EventID);
        string blockId = Normalize(row.BlockID);

        // Trigger空欄のBlockは、
        // PassThrough または Hold のCommandを含む場合は自動開始する。
        foreach (ScenarioRow candidate in rows)
        {
            if (candidate == null)
                continue;

            if (Normalize(candidate.EventID) != eventId)
                continue;

            if (Normalize(candidate.BlockID) != blockId)
                continue;

            if (!string.IsNullOrEmpty(Normalize(candidate.Trigger)))
                continue;

            CommandFlowType flowType =
                CommandFlowResolver.GetFlowType(candidate.Command);

            if (flowType == CommandFlowType.PassThrough ||
                flowType == CommandFlowType.Hold)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryStartAutoRows()
    {
        if (rows == null || currentIndex >= rows.Count)
            return false;

        if (!IsAutoExecutableRow(rows[currentIndex]))
            return false;

        runningRoutine = StartCoroutine(AutoRowsRoutine());
        return true;
    }

    private IEnumerator AutoRowsRoutine()
    {
        while (rows != null && currentIndex < rows.Count)
        {
            while (currentIndex < rows.Count &&
                IsLabelRow(rows[currentIndex]))
            {
                currentIndex++;
            }

            if (currentIndex >= rows.Count)
                break;

            ScenarioRow firstRow = rows[currentIndex];

            if (!IsAutoExecutableRow(firstRow))
                break;

            List<ScenarioRow> blockRows = CollectBlockRows(firstRow);

            float waitSeconds = 0f;
            ScenarioRow deferredJumpRow = null;

            BlockCompletionTracker completionTracker =
                new BlockCompletionTracker();

            for (int i = 0; i < blockRows.Count; i++)
            {
                ScenarioRow row = blockRows[i];
                string command = Normalize(row.Command);

                // Jumpは同一Blockの演出を破壊しないよう、
                // Block終了時まで実行を保留する
                if (command == "Jump")
                {
                    if (deferredJumpRow == null &&
                        EvaluateJumpCondition(row.Arg2))
                    {
                        deferredJumpRow = row;
                    }

                    continue;
                }

                commandExecutor.ExecuteRow(
                    row,
                    i == 0,
                    completionTracker);

                if (command == "BackToADV")
                {
                    runningRoutine = null;
                    yield break;
                }

                if (command == "Choice")
                {
                    // Choice表示後は、選択結果が確定するまで
                    // 後続Blockを自動実行しない
                    runningRoutine = null;
                    yield break;
                }

                if (string.Equals(
                        command,
                        "Modal",
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    // Modal表示後は、ユーザーが閉じるまで
                    // 後続Blockを自動実行しない。
                    runningRoutine = null;
                    yield break;
                }

                // Jumpなどで処理位置が変更されたら、元のBlockを打ち切る
                if (flowChanged)
                    break;

                // WaitはBlock全体の保持時間として扱う
                if (command == "Wait")
                {
                    waitSeconds = Mathf.Max(
                        waitSeconds,
                        ParseFloat(row.Arg1, 0f)
                    );
                }
                // AutoのDialogはArg1秒だけ表示を保持する
                else if (command == "Dialog")
                {
                    waitSeconds = Mathf.Max(
                        waitSeconds,
                        ParseFloat(row.Arg1, 2f)
                    );
                }
            }

            if (flowChanged)
            {
                flowChanged = false;
                continue;
            }

            // Block内の演出を保持
            if (waitSeconds > 0f)
                yield return new WaitForSeconds(waitSeconds);

            while (!completionTracker.IsCompleted)
            {
                yield return null;
            }

            // 同一BlockにJumpが誤って置かれていても、
            // 演出終了後までは実行しない
            if (deferredJumpRow != null)
            {
                commandExecutor.ExecuteRow(deferredJumpRow, false);

                if (flowChanged)
                {
                    flowChanged = false;
                    runningRoutine = null;

                    TryStartLandingBlock();
                    yield break;
                }
            }
        }

        runningRoutine = null;
    }

    private bool ValidateJumpTargets()
    {
        bool isValid = true;

        foreach (ScenarioRow row in rows)
        {
            if (row == null)
                continue;

            string command = Normalize(row.Command);

            if (command != "Choice" &&
                command != "Jump")
            {
                continue;
            }

            string targetLabel = Normalize(row.Arg1);

            if (string.IsNullOrEmpty(targetLabel))
            {
                Debug.LogError(
                    $"[unisanchan] {command} の遷移先Labelが空欄です。\n" +
                    $"Excel行: {row.ExcelRow}\n" +
                    $"EventID: {row.EventID}\n" +
                    $"BlockID: {row.BlockID}"
                );

                isValid = false;
                continue;
            }

            if (!navigator.ContainsLabel(targetLabel))
            {
                Debug.LogError(
                    $"[unisanchan] {command} の遷移先Labelが存在しません。\n" +
                    $"Label: {targetLabel}\n" +
                    $"Excel行: {row.ExcelRow}\n" +
                    $"EventID: {row.EventID}\n" +
                    $"BlockID: {row.BlockID}"
                );

                isValid = false;
            }
        }

        return isValid;
    }

    private bool ValidateScenarioConditions()
    {
        bool isValid = true;

        foreach (ScenarioRow row in rows)
        {
            if (row == null)
                continue;

            string command = Normalize(row.Command);

            if (command != "Choice" &&
                command != "Jump")
            {
                continue;
            }

            string condition = Normalize(row.Arg2);

            if (string.IsNullOrEmpty(condition))
                continue;

            if (!unisanchan.ExpressionEvaluator.TryEvaluateCondition(
                    condition,
                    variableStore.ResolveNumber,
                    variableStore.ResolveBool,
                    out _))
            {
                Debug.LogError(
                    $"[unisanchan] {command} の条件式を評価できません。\n" +
                    $"Condition: {condition}\n" +
                    $"Excel行: {row.ExcelRow}\n" +
                    $"EventID: {row.EventID}\n" +
                    $"BlockID: {row.BlockID}"
                );

                isValid = false;
            }
        }

        return isValid;
    }

    private bool EvaluateJumpCondition(string condition)
    {
        if (string.IsNullOrWhiteSpace(condition))
            return true;

        if (!unisanchan.ExpressionEvaluator.TryEvaluateCondition(
                condition,
                variableStore.ResolveNumber,
                variableStore.ResolveBool,
                out bool result))
        {
            Debug.LogWarning(
                $"[unisanchan] Jump条件式を評価できませんでした: {condition}");

            return false;
        }

        return result;
    }

    private void JumpToLabel(string label)
    {
        string targetLabel = Normalize(label);

        if (string.IsNullOrEmpty(targetLabel))
        {
            Debug.LogWarning(
                "[UnisanChi] Jump先ラベルが指定されていません。");
            return;
        }

        if (!navigator.TryGetTarget(
                targetLabel,
                out string targetEventId,
                out string targetBlockId,
                out string targetTrigger))
        {
            Debug.LogWarning(
                $"[UnisanChi] Jump先ラベルが見つからないか、" +
                $"ジャンプ先情報が不正です。Label: {targetLabel}");

            return;
        }

        for (int i = 0; i < rows.Count; i++)
        {
            ScenarioRow row = rows[i];

            if (Normalize(row.EventID) != targetEventId)
                continue;

            if (Normalize(row.BlockID) != targetBlockId)
                continue;
            
            if (Normalize(row.Trigger) != targetTrigger)
                continue;

            currentIndex = i;
            flowChanged = true;

            return;
        }

        Debug.LogWarning(
            $"[UnisanChi] Jump先Blockが見つかりません。" +
            $"Label: {targetLabel}, " +
            $"EventID: {targetEventId}, BlockID: {targetBlockId}"
        );
    }
}