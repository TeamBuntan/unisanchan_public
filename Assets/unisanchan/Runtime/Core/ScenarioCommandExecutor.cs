using System;
using UnityEngine;
using unisanchan;

public class ScenarioCommandExecutor
{
    private readonly Action<ScenarioRow> executeCommand;
    private readonly Action<string> updateCurrentEventId;
    private readonly ActorCommandHandler actorCommandHandler;
    private readonly CameraCommandHandler cameraCommandHandler;
    private readonly SystemCommandHandler systemCommandHandler;
    private readonly ScenarioUICommandHandler scenarioUICommandHandler;
    private readonly VariableCommandHandler variableCommandHandler;
    private readonly VoiceCommandHandler voiceCommandHandler;
    private readonly BgmCommandHandler bgmCommandHandler;
    private readonly AmbienceCommandHandler ambienceCommandHandler;
    private readonly SeCommandHandler seCommandHandler;
    private readonly ParticleCommandHandler particleCommandHandler;
    private readonly ModalCommandHandler modalCommandHandler;

    public ScenarioCommandExecutor(
        Action<ScenarioRow> executeCommand,
        Action<string> updateCurrentEventId,
        ActorCommandHandler actorCommandHandler,
        CameraCommandHandler cameraCommandHandler,
        SystemCommandHandler systemCommandHandler,
        ScenarioUICommandHandler scenarioUICommandHandler,
        VariableCommandHandler variableCommandHandler,
        VoiceCommandHandler voiceCommandHandler,
        BgmCommandHandler bgmCommandHandler = null,
        AmbienceCommandHandler ambienceCommandHandler = null,
        SeCommandHandler seCommandHandler = null,
        ParticleCommandHandler particleCommandHandler = null,
        ModalCommandHandler modalCommandHandler = null)
    {
        this.executeCommand = executeCommand;
        this.updateCurrentEventId = updateCurrentEventId;
        this.actorCommandHandler = actorCommandHandler;
        this.cameraCommandHandler = cameraCommandHandler;
        this.systemCommandHandler = systemCommandHandler;
        this.scenarioUICommandHandler = scenarioUICommandHandler;
        this.variableCommandHandler = variableCommandHandler;
        this.voiceCommandHandler = voiceCommandHandler;
        this.bgmCommandHandler = bgmCommandHandler;
        this.ambienceCommandHandler = ambienceCommandHandler;
        this.seCommandHandler = seCommandHandler;
        this.particleCommandHandler = particleCommandHandler;
        this.modalCommandHandler = modalCommandHandler;
    }

    public void ExecuteRow(
        ScenarioRow row,
        bool updateCurrentEvent,
        BlockCompletionTracker completionTracker = null)
    {
        if (row == null)
            return;

    #if UNITY_EDITOR
        if (!string.IsNullOrWhiteSpace(row.DebugLog))
        {
            Debug.Log(
                "[Excel Event System Debug]\n" +
                $"EventID : {row.EventID}\n" +
                $"BlockID : {row.BlockID}\n" +
                $"Trigger : {row.Trigger}\n" +
                $"Command : {row.Command}\n" +
                $"Log : {row.DebugLog}"
            );
        }
    #endif

        if (updateCurrentEvent &&
            !string.IsNullOrWhiteSpace(row.EventID))
        {
            updateCurrentEventId?.Invoke(row.EventID.Trim());
        }

        if (!string.IsNullOrWhiteSpace(row.Command))
        {   
            if (actorCommandHandler != null &&
                actorCommandHandler.TryExecute(
                    row,
                    completionTracker))
            {
                return;
            }

            if (cameraCommandHandler != null &&
                cameraCommandHandler.TryExecute(
                    row,
                    completionTracker))
            {
                return;
            }

            if (systemCommandHandler != null &&
                systemCommandHandler.TryExecute(row))
            {
                return;
            }

            if (variableCommandHandler != null &&
                variableCommandHandler.TryExecute(row))
            {
                return;
            }

            if (voiceCommandHandler != null &&
                voiceCommandHandler.TryExecute(row))
            {
                return;
            }

            if (bgmCommandHandler != null &&
                bgmCommandHandler.TryExecute(row))
            {
                return;
            }

            if (ambienceCommandHandler != null &&
                ambienceCommandHandler.TryExecute(row))
            {
                return;
            }

            if (seCommandHandler != null &&
                seCommandHandler.TryExecute(row))
            {
                return;
            }

            if (particleCommandHandler != null &&
                particleCommandHandler.TryExecute(row))
            {
                return;
            }

            if (modalCommandHandler != null &&
                modalCommandHandler.TryExecute(row))
            {
                return;
            }

            if (scenarioUICommandHandler != null &&
                scenarioUICommandHandler.TryExecute(row))
            {
                if (string.Equals(
                        row.Command?.Trim(),
                        "Dialog",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(row.Voice))
                    {
                        if (voiceCommandHandler != null &&
                            !voiceCommandHandler.IsFadingOut)
                        {
                            voiceCommandHandler.Stop();
                        }
                    }
                    else
                    {
                        voiceCommandHandler?.Play(row.Voice);
                    }
                }

                return;
            }

            executeCommand?.Invoke(row);
            return;
        }

        Debug.Log("EventID : " + row.EventID);
        Debug.Log("Text : " + row.Text);
    }
}