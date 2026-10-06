using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using unisanchan;

public class CameraCommandHandler
{
    private const float DefaultCameraDuration = 0.8f;

    private readonly ScenarioCameraController cameraController;
    private readonly List<CameraPresetRow> cameraPresets;
    private readonly Image fadePanel;
    private readonly MonoBehaviour coroutineHost;

    private Coroutine fadeRoutine;

    public CameraCommandHandler(
        ScenarioCameraController cameraController,
        List<CameraPresetRow> cameraPresets,
        Image fadePanel,
        MonoBehaviour coroutineHost)
    {
        this.cameraController = cameraController;
        this.cameraPresets = cameraPresets;
        this.fadePanel = fadePanel;
        this.coroutineHost = coroutineHost;
    }

    /// <summary>
    /// Camera／Fade系コマンドなら実行してtrueを返します。
    /// 担当外なら何もせずfalseを返します。
    /// </summary>
    public bool TryExecute(
        ScenarioRow row,
        BlockCompletionTracker completionTracker = null)
    {
        if (row == null)
            return false;

        switch (Normalize(row.Command))
        {
            case "Camera":
                ApplyCameraCommand(
                    row,
                    completionTracker);

                return true;
            
            case "SpawnCamera":
                ApplySpawnCameraCommand(
                    row,
                    completionTracker);

                return true;
            
            case "DestroyCamera":
                ApplyDestroyCameraCommand(row);
                return true;

            case "SelectCamera":
                ApplySelectCameraCommand(row);
                return true;

            case "FadeIn":
                ApplyFadeInCommand(row);
                return true;

            case "FadeOut":
                ApplyFadeOutCommand(row);
                return true;

            default:
                return false;
        }
    }

    private void ApplyCameraCommand(
        ScenarioRow row,
        BlockCompletionTracker completionTracker)
    {
        string presetName = row.Arg2;
        string duration = row.Arg3;

        ApplyCameraValues(
            row.Arg1,
            presetName,
            row.Arg4,
            row.Arg5,
            row.Arg6,
            row.Arg7,
            row.Arg8,
            row.Arg9,
            row.Arg10,
            row.Arg11,
            row.Arg12,
            duration,
            DefaultCameraDuration,
            completionTracker
        );
    }

    private void ApplySpawnCameraCommand(
        ScenarioRow row,
        BlockCompletionTracker completionTracker)
    {
        if (cameraController == null)
        {
            Debug.LogWarning(
                "[Camera Command] SpawnCameraを実行できません。" +
                "ScenarioCameraControllerが設定されていません。");

            return;
        }

        string cameraAssetId =
            Normalize(row.Arg1);

        string cameraInstanceId =
            Normalize(row.Arg2);

        if (string.IsNullOrEmpty(cameraAssetId))
        {
            Debug.LogError(
                "[Camera Command] SpawnCameraのArg1に" +
                "Camera Asset IDを指定してください。");

            return;
        }

        if (string.IsNullOrEmpty(cameraInstanceId))
        {
            Debug.LogError(
                "[Camera Command] SpawnCameraのArg2に" +
                "Camera Instance IDを指定してください。");

            return;
        }

        bool spawned =
            cameraController.SpawnCamera(
                cameraAssetId,
                cameraInstanceId);

        if (!spawned)
            return;

        string presetName =
            Normalize(row.Arg3);

        if (string.IsNullOrEmpty(presetName))
            return;

        ApplyCameraValues(
            cameraInstanceId: cameraInstanceId,
            presetName: presetName,
            xValue: "",
            yValue: "",
            zValue: "",
            rotXValue: "",
            rotYValue: "",
            rotZValue: "",
            fovValue: "",
            easingValue: "",
            completionModeValue: "",
            durationValue: "",
            defaultDuration: DefaultCameraDuration,
            completionTracker: completionTracker);
    }

    private void ApplyDestroyCameraCommand(
        ScenarioRow row)
    {
        if (cameraController == null)
        {
            Debug.LogWarning(
                "[Camera Command] DestroyCameraを実行できません。" +
                "ScenarioCameraControllerが設定されていません。");

            return;
        }

        string cameraInstanceId =
            Normalize(row.Arg1);

        if (string.IsNullOrEmpty(cameraInstanceId))
        {
            Debug.LogWarning(
                "[Camera Command] DestroyCameraのArg1に" +
                "破棄するCamera Instance IDを指定してください。");

            return;
        }

        string returnCameraInstanceId =
            Normalize(row.Arg2);

        cameraController.DestroyCamera(
            cameraInstanceId,
            returnCameraInstanceId);
    }

    private void ApplySelectCameraCommand(
        ScenarioRow row)
    {
        if (cameraController == null)
        {
            Debug.LogWarning(
                "[Camera Command] SelectCameraを実行できません。" +
                "ScenarioCameraControllerが設定されていません。");

            return;
        }

        string cameraInstanceId =
            Normalize(row.Arg1);

        if (string.IsNullOrEmpty(cameraInstanceId))
        {
            Debug.LogWarning(
                "[Camera Command] SelectCameraのArg1に" +
                "Camera Instance IDを指定してください。");

            return;
        }

        cameraController.SelectCamera(
            cameraInstanceId);
    }

    private void ApplyFadeInCommand(ScenarioRow row)
    {
        float duration = ParseFloat(row.Arg2, 1f);
        float alpha = ParseFloat(row.Arg3, 1f);

        SetFadeColor(ParseFadeColor(row.Arg1));

        StartFade(alpha, 0f, duration);
    }

    private void ApplyFadeOutCommand(ScenarioRow row)
    {
        float duration = ParseFloat(row.Arg2, 1f);
        float alpha = ParseFloat(row.Arg3, 1f);

        SetFadeColor(ParseFadeColor(row.Arg1));

        StartFade(0f, alpha, duration);
    }

    private void StartFade(
        float fromAlpha,
        float toAlpha,
        float duration)
    {
        if (coroutineHost == null)
        {
            Debug.LogWarning(
                "[UnisanChi] Fadeを実行できません。" +
                "Coroutineの実行元が設定されていません。");

            return;
        }

        if (fadeRoutine != null)
            coroutineHost.StopCoroutine(fadeRoutine);

        fadeRoutine = coroutineHost.StartCoroutine(
            FadeCoroutine(
                fromAlpha,
                toAlpha,
                duration
            )
        );
    }

    private void ApplyCameraValues(
        string cameraInstanceId,
        string presetName,
        string xValue,
        string yValue,
        string zValue,
        string rotXValue,
        string rotYValue,
        string rotZValue,
        string fovValue,
        string easingValue,
        string completionModeValue,
        string durationValue,
        float defaultDuration,
        BlockCompletionTracker completionTracker)
    {
        if (cameraController == null)
        {
            Debug.LogWarning(
                "[UnisanChi] Cameraコマンドを実行できません。" +
                "ScenarioCameraControllerが設定されていません。");

            return;
        }

        Camera targetCamera =
            cameraController.ResolveCamera(
                cameraInstanceId);

        if (targetCamera == null)
        {
            Debug.LogWarning(
                "[Camera Command] 操作対象のCameraを解決できません。" +
                $" ID: {cameraInstanceId}");

            return;
        }

        float duration =
            ParseFloat(
                durationValue,
                defaultDuration);

        if (!EasingUtility.TryParse(
                easingValue,
                out EasingType easingType))
        {
            Debug.LogWarning(
                "[UnisanChi] 未対応のCamera Easing Typeです。" +
                $" Linearを使用します: {easingValue}");

            easingType = EasingType.Linear;
        }

        string completionMode =
            Normalize(completionModeValue)
                .ToLowerInvariant();

        bool waitForCompletion =
            completionMode != "async";

        if (completionMode != "" &&
            completionMode != "wait" &&
            completionMode != "async")
        {
            Debug.LogWarning(
                "[UnisanChi] CameraのArg12が未対応です。" +
                $" Waitとして処理します: {completionModeValue}");

            waitForCompletion = true;
        }

        string preset =
            Normalize(presetName);

        Vector3 currentPosition =
            cameraController.GetCurrentPosition(
                targetCamera);

        Quaternion currentRotation =
            cameraController.GetCurrentRotation(
                targetCamera);

        Vector3 currentEuler =
            currentRotation.eulerAngles;

        bool hasX =
            TryParseFloat(xValue, out float x);

        bool hasY =
            TryParseFloat(yValue, out float y);

        bool hasZ =
            TryParseFloat(zValue, out float z);

        bool hasRotX =
            TryParseFloat(rotXValue, out float rotX);

        bool hasRotY =
            TryParseFloat(rotYValue, out float rotY);

        bool hasRotZ =
            TryParseFloat(rotZValue, out float rotZ);

        bool hasFov =
            TryParseFloat(fovValue, out float fov);

        if (!string.IsNullOrEmpty(preset))
        {
            CameraPresetRow presetRow =
                FindCameraPreset(preset);

            if (presetRow != null)
            {
                string presetSpace =
                    Normalize(presetRow.Space)
                        .ToLowerInvariant();

                bool useLocalSpace =
                    presetSpace == "local";

                if (presetSpace != "" &&
                    presetSpace != "world" &&
                    presetSpace != "local")
                {
                    Debug.LogWarning(
                        "[Camera Command] CameraPresetのSpaceが未対応です。" +
                        $" Worldとして処理します: {presetRow.Space}");

                    useLocalSpace = false;
                }

                Vector3 basePosition =
                    useLocalSpace
                        ? cameraController.GetCurrentLocalPosition(
                            targetCamera)
                        : cameraController.GetCurrentPosition(
                            targetCamera);

                Quaternion baseRotation =
                    useLocalSpace
                        ? cameraController.GetCurrentLocalRotation(
                            targetCamera)
                        : cameraController.GetCurrentRotation(
                            targetCamera);

                Vector3 baseEuler =
                    baseRotation.eulerAngles;

                float presetDuration =
                    ParseFloat(
                        durationValue,
                        ParseFloat(
                            presetRow.Duration,
                            defaultDuration));

                EasingType presetEasingType =
                    easingType;

                if (string.IsNullOrWhiteSpace(easingValue))
                {
                    if (!EasingUtility.TryParse(
                            presetRow.EasingType,
                            out presetEasingType))
                    {
                        Debug.LogWarning(
                            "[UnisanChi] CameraPresetのEasing Typeが未対応です。" +
                            $" Linearを使用します: {presetRow.EasingType}");

                        presetEasingType =
                            EasingType.Linear;
                    }
                }

                Vector3 targetPosition =
                    new Vector3(
                        hasX
                            ? x
                            : ParseFloat(
                                presetRow.PosX,
                                basePosition.x),

                        hasY
                            ? y
                            : ParseFloat(
                                presetRow.PosY,
                                basePosition.y),

                        hasZ
                            ? z
                            : ParseFloat(
                                presetRow.PosZ,
                                basePosition.z)
                    );

                Quaternion targetRotation =
                    Quaternion.Euler(
                        hasRotX
                            ? rotX
                            : ParseFloat(
                                presetRow.RotX,
                                baseEuler.x),

                        hasRotY
                            ? rotY
                            : ParseFloat(
                                presetRow.RotY,
                                baseEuler.y),

                        hasRotZ
                            ? rotZ
                            : ParseFloat(
                                presetRow.RotZ,
                                baseEuler.z)
                    );

                Action moveCompleted =
                    CreateCompletionCallback(
                        completionTracker,
                        waitForCompletion);

                if (useLocalSpace)
                {
                    cameraController
                        .MoveToLocalPositionAndRotationWithDuration(
                            targetCamera,
                            targetPosition,
                            targetRotation,
                            presetDuration,
                            presetEasingType,
                            moveCompleted
                        );
                }
                else
                {
                    cameraController
                        .MoveToPositionAndRotationWithDuration(
                            targetCamera,
                            targetPosition,
                            targetRotation,
                            presetDuration,
                            presetEasingType,
                            moveCompleted
                        );
                }

                if (hasFov)
                {
                    Action fovCompleted =
                        CreateCompletionCallback(
                            completionTracker,
                            waitForCompletion);

                    cameraController
                        .MoveToFOVWithDuration(
                            targetCamera,
                            fov,
                            presetDuration,
                            presetEasingType,
                            fovCompleted
                        );
                }
                else if (TryParseFloat(
                            presetRow.FOV,
                            out float presetFov))
                {
                    Action fovCompleted =
                        CreateCompletionCallback(
                            completionTracker,
                            waitForCompletion);

                    cameraController
                        .MoveToFOVWithDuration(
                            targetCamera,
                            presetFov,
                            presetDuration,
                            presetEasingType,
                            fovCompleted
                        );
                }

                return;
            }

            Debug.LogError(
                "[Camera Command] CameraPresetが" +
                "Presets.xlsxに見つかりません。" +
                $" Preset: {preset}");

            return;
        }

        Vector3 directTargetPosition =
            new Vector3(
                hasX ? x : currentPosition.x,
                hasY ? y : currentPosition.y,
                hasZ ? z : currentPosition.z
            );

        Quaternion directTargetRotation =
            Quaternion.Euler(
                hasRotX ? rotX : currentEuler.x,
                hasRotY ? rotY : currentEuler.y,
                hasRotZ ? rotZ : currentEuler.z
            );

        if (hasX ||
            hasY ||
            hasZ ||
            hasRotX ||
            hasRotY ||
            hasRotZ)
        {
            Action directMoveCompleted =
                CreateCompletionCallback(
                    completionTracker,
                    waitForCompletion);

            cameraController
                .MoveToPositionAndRotationWithDuration(
                    targetCamera,
                    directTargetPosition,
                    directTargetRotation,
                    duration,
                    easingType,
                    directMoveCompleted
                );
        }

        if (hasFov)
        {
            Action directFovCompleted =
                CreateCompletionCallback(
                    completionTracker,
                    waitForCompletion);

            cameraController
                .MoveToFOVWithDuration(
                    targetCamera,
                    fov,
                    duration,
                    easingType,
                    directFovCompleted
                );
        }
    }

    private static Action CreateCompletionCallback(
        BlockCompletionTracker completionTracker,
        bool waitForCompletion)
    {
        if (!waitForCompletion ||
            completionTracker == null)
        {
            return null;
        }

        CommandCompletionTicket ticket =
            new CommandCompletionTicket();

        completionTracker.Add(ticket);

        return ticket.Complete;
    }

    private CameraPresetRow FindCameraPreset(
        string presetName)
    {
        if (cameraPresets == null)
            return null;

        foreach (CameraPresetRow preset in cameraPresets)
        {
            if (preset == null)
                continue;

            if (Normalize(preset.PresetName) ==
                Normalize(presetName))
            {
                return preset;
            }
        }

        return null;
    }

    private void SetFadeColor(Color color)
    {
        if (fadePanel == null)
            return;

        color.a = fadePanel.color.a;
        fadePanel.color = color;
    }

    private IEnumerator FadeCoroutine(
        float fromAlpha,
        float toAlpha,
        float duration)
    {
        if (fadePanel == null)
            yield break;

        Color color = fadePanel.color;
        color.a = fromAlpha;
        fadePanel.color = color;

        if (duration <= 0f)
        {
            color.a = toAlpha;
            fadePanel.color = color;
            fadeRoutine = null;
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsedTime / duration);

            color.a =
                Mathf.Lerp(
                    fromAlpha,
                    toAlpha,
                    progress);

            fadePanel.color = color;

            yield return null;
        }

        color.a = toAlpha;
        fadePanel.color = color;
        fadeRoutine = null;
    }

    private static Color ParseFadeColor(string value)
    {
        switch (Normalize(value).ToLowerInvariant())
        {
            case "white":
                return Color.white;

            case "red":
                return Color.red;

            case "black":
            default:
                return Color.black;
        }
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Trim();
    }

    private static float ParseFloat(
        string value,
        float defaultValue)
    {
        return TryParseFloat(value, out float result)
            ? result
            : defaultValue;
    }

    private static bool TryParseFloat(
        string value,
        out float result)
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
}