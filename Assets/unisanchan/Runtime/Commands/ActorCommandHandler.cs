using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using unisanchan;

public class ActorCommandHandler
{
    private readonly ActorList actorList;
    private readonly Func<IEnumerator, Coroutine> startCoroutine;
    private readonly Dictionary<GameObject, int> activeTransitionSerials = new();
    private readonly Dictionary<GameObject, int> moveTransitionSerials = new();

    private sealed class MaterialAlphaTarget
    {
        public Material Material;
        public int ColorPropertyId;
        public Color OriginalColor;
    }

    public ActorCommandHandler(
        ActorList actorList,
        Func<IEnumerator, Coroutine> startCoroutine)
    {
        this.actorList = actorList;
        this.startCoroutine = startCoroutine;
    }

    /// <summary>
    /// Actor関連コマンドなら実行してtrueを返します。
    /// 担当外のコマンドなら何もせずfalseを返します。
    /// </summary>
    public bool TryExecute(
        ScenarioRow row,
        BlockCompletionTracker completionTracker = null)
    {
        if (row == null)
            return false;

        switch (Normalize(row.Command))
        {
            case "SetActive":
                ExecuteSetActive(row);
                return true;
            
            case "Move":
                ExecuteMove(row, completionTracker);
                return true;

            case "Animation":
                ExecuteAnimation(row);
                return true;

            case "Face":
                ExecuteFace(row);
                return true;

            case "FaceReset":
                ExecuteFaceReset(row);
                return true;

            default:
                return false;
        }
    }

    private int BeginActiveTransition(GameObject actor)
    {
        if (!activeTransitionSerials.TryGetValue(
                actor,
                out int currentSerial))
        {
            currentSerial = 0;
        }

        int nextSerial = currentSerial + 1;
        activeTransitionSerials[actor] = nextSerial;

        return nextSerial;
    }

    private bool IsActiveTransitionCurrent(
        GameObject actor,
        int transitionSerial)
    {
        if (actor == null)
            return false;

        return activeTransitionSerials.TryGetValue(
                actor,
                out int currentSerial) &&
            currentSerial == transitionSerial;
    }

    private void ExecuteSetActive(ScenarioRow row)
    {
        if (actorList == null)
        {
            Debug.LogWarning(
                "[unisanchan] SetActiveを実行できませんでした。" +
                $"ActorListが設定されていません。Actor: {row.Arg1}");

            return;
        }

        GameObject actor = actorList.FindActor(row.Arg1);

        if (actor == null)
        {
            Debug.LogWarning(
                "[unisanchan] SetActiveを安全に無視しました。" +
                "Actorが見つかりません。" +
                $"Actor: {row.Arg1}");

            return;
        }

        if (!TryParseBool(row.Arg2, out bool isActive))
        {
            Debug.LogWarning(
                "[unisanchan] SetActiveを安全に無視しました。" +
                "Arg2をTRUEまたはFALSEとして解釈できません。" +
                $"Actor: {row.Arg1}, Value: {row.Arg2}");

            return;
        }

        float duration = 0f;

        if (!string.IsNullOrWhiteSpace(row.Arg3) &&
            (!TryParseFloat(row.Arg3, out duration) || duration < 0f))
        {
            Debug.LogWarning(
                "[unisanchan] SetActiveを安全に無視しました。" +
                "Arg3を0以上の秒数として解釈できません。" +
                $"Actor: {row.Arg1}, Duration: {row.Arg3}");

            return;
        }

        string transitionMode = Normalize(row.Arg4);

        if (duration <= 0f ||
            string.IsNullOrEmpty(transitionMode) ||
            string.Equals(
                transitionMode,
                "None",
                StringComparison.OrdinalIgnoreCase))
        {
            BeginActiveTransition(actor);
            actor.SetActive(isActive);
            return;
        }

        if (!string.Equals(
                transitionMode,
                "Alpha",
                StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning(
                "[unisanchan] SetActiveを安全に無視しました。" +
                "未対応のTransition Modeです。" +
                $"Actor: {row.Arg1}, Mode: {row.Arg4}");

            return;
        }

        if (startCoroutine == null)
        {
            Debug.LogWarning(
                "[unisanchan] SetActiveのAlpha遷移を開始できませんでした。" +
                $"Coroutineの開始処理が設定されていません。Actor: {row.Arg1}");

            return;
        }

        int transitionSerial =
            BeginActiveTransition(actor);

        startCoroutine.Invoke(
            FadeActiveRoutine(
                actor,
                isActive,
                duration,
                row.Arg1,
                transitionSerial));
    }

    private IEnumerator FadeActiveRoutine(
        GameObject actor,
        bool isActive,
        float duration,
        string actorName,
        int transitionSerial)
    {
        if (!IsActiveTransitionCurrent(
                actor,
                transitionSerial))
        {
            yield break;
        }

        if (isActive)
            actor.SetActive(true);

        List<MaterialAlphaTarget> targets =
            CollectMaterialAlphaTargets(actor);

        if (targets.Count == 0)
        {
            Debug.LogWarning(
                "[unisanchan] SetActiveのAlpha遷移を実行できませんでした。" +
                "_BaseColorまたは_Colorを持つMaterialが見つかりません。" +
                $"Actor: {actorName}");

            actor.SetActive(isActive);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (!IsActiveTransitionCurrent(
                    actor,
                    transitionSerial))
            {
                yield break;
            }

            float progress =
                Mathf.Clamp01(elapsed / duration);

            foreach (MaterialAlphaTarget target in targets)
            {
                float startAlpha = isActive
                    ? 0f
                    : target.OriginalColor.a;

                float endAlpha = isActive
                    ? target.OriginalColor.a
                    : 0f;

                SetMaterialAlpha(
                    target,
                    Mathf.Lerp(
                        startAlpha,
                        endAlpha,
                        progress));
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!IsActiveTransitionCurrent(
                actor,
                transitionSerial))
        {
            yield break;
        }

        foreach (MaterialAlphaTarget target in targets)
        {
            SetMaterialAlpha(
                target,
                isActive
                    ? target.OriginalColor.a
                    : 0f);
        }

        if (!isActive)
        {
            actor.SetActive(false);

            foreach (MaterialAlphaTarget target in targets)
            {
                target.Material.SetColor(
                    target.ColorPropertyId,
                    target.OriginalColor);
            }
        }
    }

    private static List<MaterialAlphaTarget> CollectMaterialAlphaTargets(
        GameObject actor)
    {
        List<MaterialAlphaTarget> targets = new();
        Renderer[] renderers =
            actor.GetComponentsInChildren<Renderer>(true);

        int baseColorId = Shader.PropertyToID("_BaseColor");
        int colorId = Shader.PropertyToID("_Color");

        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.materials)
            {
                if (material == null)
                    continue;

                int propertyId;

                if (material.HasProperty(baseColorId))
                    propertyId = baseColorId;
                else if (material.HasProperty(colorId))
                    propertyId = colorId;
                else
                    continue;

                targets.Add(
                    new MaterialAlphaTarget
                    {
                        Material = material,
                        ColorPropertyId = propertyId,
                        OriginalColor = material.GetColor(propertyId)
                    });
            }
        }

        return targets;
    }

    private static void SetMaterialAlpha(
        MaterialAlphaTarget target,
        float alpha)
    {
        Color color = target.OriginalColor;
        color.a = alpha;

        target.Material.SetColor(
            target.ColorPropertyId,
            color);
    }

    private int BeginMoveTransition(GameObject actor)
    {
        if (!moveTransitionSerials.TryGetValue(
                actor,
                out int currentSerial))
        {
            currentSerial = 0;
        }

        int nextSerial = currentSerial + 1;
        moveTransitionSerials[actor] = nextSerial;

        return nextSerial;
    }

    private bool IsMoveTransitionCurrent(
        GameObject actor,
        int transitionSerial)
    {
        if (actor == null)
            return false;

        return moveTransitionSerials.TryGetValue(
                actor,
                out int currentSerial) &&
            currentSerial == transitionSerial;
    }

    private void ExecuteMove(
        ScenarioRow row,
        BlockCompletionTracker completionTracker)
    {
        if (actorList == null)
        {
            Debug.LogWarning(
                "[unisanchan] Moveを実行できませんでした。" +
                $"ActorListが設定されていません。Actor: {row.Arg1}");

            return;
        }

        GameObject actor =
            actorList.FindActor(row.Arg1);

        if (actor == null)
        {
            Debug.LogWarning(
                "[unisanchan] Moveを安全に無視しました。" +
                "Actorが見つかりません。" +
                $"Actor: {row.Arg1}");

            return;
        }

        string coordinateMode =
            Normalize(row.Arg6);

        if (string.IsNullOrEmpty(coordinateMode))
            coordinateMode = "Local";

        Vector3 currentPosition;
        bool isRelative = false;

        switch (coordinateMode.ToLowerInvariant())
        {
            case "local":
                currentPosition =
                    actor.transform.localPosition;
                break;

            case "world":
                currentPosition =
                    actor.transform.position;
                break;

            case "relative":
                currentPosition = Vector3.zero;
                isRelative = true;
                break;

            default:
                Debug.LogWarning(
                    "[unisanchan] Moveを安全に無視しました。" +
                    "未対応のCoordinate Modeです。" +
                    $"Actor: {row.Arg1}, Mode: {row.Arg6}");

                return;
        }

        if (!TryParseOptionalMoveCoordinate(
                row.Arg2,
                currentPosition.x,
                "X",
                row.Arg1,
                out float x) ||
            !TryParseOptionalMoveCoordinate(
                row.Arg3,
                currentPosition.y,
                "Y",
                row.Arg1,
                out float y) ||
            !TryParseOptionalMoveCoordinate(
                row.Arg4,
                currentPosition.z,
                "Z",
                row.Arg1,
                out float z))
        {
            return;
        }

        Vector3 destination =
            new Vector3(x, y, z);

        bool useWorldPosition =
            string.Equals(
                coordinateMode,
                "World",
                StringComparison.OrdinalIgnoreCase);

        Vector3 startPosition = useWorldPosition
            ? actor.transform.position
            : actor.transform.localPosition;

        if (isRelative)
        {
            destination =
                startPosition + destination;
        }

        float duration = 0f;

        if (!string.IsNullOrWhiteSpace(row.Arg5) &&
            (!TryParseFloat(row.Arg5, out duration) ||
            duration < 0f))
        {
            Debug.LogWarning(
                "[unisanchan] Moveを安全に無視しました。" +
                "Arg5を0以上の秒数として解釈できません。" +
                $"Actor: {row.Arg1}, Duration: {row.Arg5}");

            return;
        }

        if (!EasingUtility.TryParse(
                row.Arg7,
                out EasingType easingType))
        {
            Debug.LogWarning(
                "[unisanchan] Moveを安全に無視しました。" +
                "未対応のEasing Typeです。" +
                $"Actor: {row.Arg1}, Easing: {row.Arg7}");

            return;
        }

        int transitionSerial =
            BeginMoveTransition(actor);

        if (duration <= 0f)
        {
            if (useWorldPosition)
                actor.transform.position = destination;
            else
                actor.transform.localPosition = destination;

            return;
        }

        if (startCoroutine == null)
        {
            Debug.LogWarning(
                "[unisanchan] Moveを開始できませんでした。" +
                $"Coroutineの開始処理が設定されていません。Actor: {row.Arg1}");

            return;
        }

        CommandCompletionTicket completionTicket = null;

        if (completionTracker != null)
        {
            completionTicket =
                new CommandCompletionTicket();

            completionTracker.Add(completionTicket);
        }

        startCoroutine.Invoke(
            MoveRoutine(
                actor,
                startPosition,
                destination,
                duration,
                useWorldPosition,
                easingType,
                transitionSerial,
                completionTicket));
    }

    private IEnumerator MoveRoutine(
        GameObject actor,
        Vector3 startPosition,
        Vector3 destination,
        float duration,
        bool useWorldPosition,
        EasingType easingType,
        int transitionSerial,
        CommandCompletionTicket completionTicket)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (!IsMoveTransitionCurrent(actor, transitionSerial))
            {
                completionTicket?.Complete();
                yield break;
            }

            float progress =
                Mathf.Clamp01(elapsed / duration);

            float easedProgress =
                EasingUtility.Evaluate(
                    easingType,
                    progress);

            Vector3 currentPosition =
                Vector3.Lerp(
                    startPosition,
                    destination,
                    easedProgress);

            if (useWorldPosition)
                actor.transform.position = currentPosition;
            else
                actor.transform.localPosition = currentPosition;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!IsMoveTransitionCurrent(actor, transitionSerial))
        {
            completionTicket?.Complete();
            yield break;
        }

        if (useWorldPosition)
            actor.transform.position = destination;
        else
            actor.transform.localPosition = destination;
        
        completionTicket?.Complete();
    }

    private static bool TryParseOptionalMoveCoordinate(
        string value,
        float currentValue,
        string axisName,
        string actorName,
        out float result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = currentValue;
            return true;
        }

        if (TryParseFloat(value, out result))
            return true;

        Debug.LogWarning(
            "[unisanchan] Moveを安全に無視しました。" +
            $"{axisName}座標を数値として解釈できません。" +
            $"Actor: {actorName}, Value: {value}");

        return false;
    }

    private void ExecuteAnimation(ScenarioRow row)
    {
        if (actorList == null)
        {
            Debug.LogWarning(
                "[unisanchan] Animationを実行できませんでした。" +
                $"ActorListが設定されていません。Actor: {row.Arg1}");

            return;
        }

        Animator animator =
            actorList.FindAnimator(row.Arg1);

        if (animator == null)
        {
            Debug.LogWarning(
                "[unisanchan] Animationを安全に無視しました。" +
                "ActorまたはAnimatorが見つかりません。" +
                $"Actor: {row.Arg1}");

            return;
        }

        string parameterName =
            Normalize(row.Arg2);

        string parameterType =
            Normalize(row.Arg3);

        string parameterValue =
            Normalize(row.Arg4);

        if (string.IsNullOrEmpty(parameterName))
        {
            Debug.LogWarning(
                "[unisanchan] Animationを安全に無視しました。" +
                $"Parameter名が空です。Actor: {row.Arg1}");

            return;
        }

        if (string.IsNullOrEmpty(parameterType))
            parameterType = "Trigger";

        switch (parameterType.ToLowerInvariant())
        {
            case "trigger":
                animator.SetTrigger(parameterName);
                break;

            case "bool":
                ExecuteBoolAnimation(
                    animator,
                    parameterName,
                    parameterValue,
                    row);

                break;

            case "int":
            case "integer":
                if (!int.TryParse(parameterValue, out int intValue))
                {
                    Debug.LogWarning(
                        "[unisanchan] Int値を解釈できません。" +
                        $"Actor: {row.Arg1}, " +
                        $"Parameter: {parameterName}, " +
                        $"Value: {row.Arg4}");

                    return;
                }

                animator.SetInteger(
                    parameterName,
                    intValue);

                break;

            case "float":
                if (!TryParseFloat(
                        parameterValue,
                        out float floatValue))
                {
                    Debug.LogWarning(
                        "[unisanchan] Float値を解釈できません。" +
                        $"Actor: {row.Arg1}, " +
                        $"Parameter: {parameterName}, " +
                        $"Value: {row.Arg4}");

                    return;
                }

                animator.SetFloat(
                    parameterName,
                    floatValue);

                break;

            default:
                Debug.LogWarning(
                    "[unisanchan] 未対応のAnimator Parameter Typeです。" +
                    $"Type: {row.Arg3}, " +
                    $"Actor: {row.Arg1}, " +
                    $"Parameter: {parameterName}");

                break;
        }
    }

    private void ExecuteBoolAnimation(
        Animator animator,
        string parameterName,
        string parameterValue,
        ScenarioRow row)
    {
        if (!TryParseBool(
                parameterValue,
                out bool boolValue))
        {
            Debug.LogWarning(
                "[unisanchan] Bool値を解釈できません。" +
                $"Actor: {row.Arg1}, " +
                $"Parameter: {parameterName}, " +
                $"Value: {row.Arg4}");

            return;
        }

        animator.SetBool(
            parameterName,
            boolValue);

        if (!boolValue)
            return;

        if (!TryParseFloat(
                row.Arg5,
                out float resetSeconds))
        {
            return;
        }

        if (resetSeconds < 0f)
            return;

        if (startCoroutine == null)
            return;

        startCoroutine.Invoke(
            ResetBoolAfterDelay(
                animator,
                parameterName,
                resetSeconds));
    }

    private void ExecuteFace(ScenarioRow row)
    {
        if (actorList == null)
        {
            Debug.LogWarning(
                "[unisanchan] Faceを実行できませんでした。" +
                $"ActorListが設定されていません。Actor: {row.Arg1}");

            return;
        }

        FaceController faceController =
            actorList.FindFaceController(row.Arg1);

        if (faceController == null)
        {
            Debug.LogWarning(
                "[unisanchan] Faceを安全に無視しました。" +
                "ActorまたはFaceControllerが見つかりません。" +
                $"Actor: {row.Arg1}");

            return;
        }

        string faceName =
            Normalize(row.Arg2);

        if (string.IsNullOrEmpty(faceName))
        {
            Debug.LogWarning(
                "[unisanchan] Faceを安全に無視しました。" +
                $"表情名が空です。Actor: {row.Arg1}");

            return;
        }

        faceController.SetFace(faceName);
    }

    private void ExecuteFaceReset(ScenarioRow row)
    {
        if (actorList == null)
        {
            Debug.LogWarning(
                "[unisanchan] FaceResetを実行できませんでした。" +
                $"ActorListが設定されていません。Actor: {row.Arg1}");

            return;
        }

        FaceController faceController =
            actorList.FindFaceController(row.Arg1);

        if (faceController == null)
        {
            Debug.LogWarning(
                "[UnisanChi] FaceResetを安全に無視しました。" +
                "ActorまたはFaceControllerが見つかりません。" +
                $"Actor: {row.Arg1}");

            return;
        }

        faceController.ResetFace();
    }

    private static IEnumerator ResetBoolAfterDelay(
        Animator animator,
        string parameterName,
        float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        else
            yield return null;

        if (animator != null)
        {
            animator.SetBool(
                parameterName,
                false);
        }
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Trim();
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
            out result);
    }

    private static bool TryParseBool(
        string value,
        out bool result)
    {
        string normalized =
            Normalize(value).ToLowerInvariant();

        switch (normalized)
        {
            case "true":
            case "1":
            case "yes":
            case "on":
                result = true;
                return true;

            case "false":
            case "0":
            case "no":
            case "off":
                result = false;
                return true;

            default:
                result = false;
                return false;
        }
    }
}
