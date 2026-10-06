using System;
using System.Collections.Generic;
using UnityEngine;

public class FaceController : MonoBehaviour
{
    public enum FaceControlType
    {
        AnimatorState,
        BlendShape
    }

    [Serializable]
    public class FaceEntry
    {
        [Tooltip("Excelから指定する表情名。例：Default、Smile、Sad")]
        public string faceName;

        [Tooltip("表情の制御方式")]
        public FaceControlType controlType = FaceControlType.AnimatorState;

        [Tooltip("Animator方式で再生するState名")]
        public string animatorStateName;

        [Tooltip("BlendShape方式で操作するRenderer")]
        public SkinnedMeshRenderer faceRenderer;

        [Tooltip("BlendShape名")]
        public string blendShapeName;

        [Range(0f, 100f)]
        [Tooltip("BlendShapeの適用値")]
        public float blendShapeWeight = 100f;
    }

    [Header("References")]
    [SerializeField] private Animator animator;

    [Tooltip("Animator表情レイヤー。ユニティちゃんは通常1")]
    [SerializeField] private int animatorLayer = 1;

    [Tooltip("Animator表情の切替時間")]
    [SerializeField] private float crossFadeDuration = 0.1f;

    [Header("Face Settings")]
    [SerializeField] private List<FaceEntry> faces = new List<FaceEntry>();

    public string CurrentFaceName { get; private set; }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public bool SetFace(string faceName)
    {
        FaceEntry entry = FindFace(faceName);

        if (entry == null)
        {
            Debug.LogWarning(
                $"[unisanchan] 登録されていない表情です。" +
                $"Actor: {name}, Face: {faceName}"
            );

            return false;
        }

        ResetAllBlendShapes();

        switch (entry.controlType)
        {
            case FaceControlType.AnimatorState:
                return ApplyAnimatorState(entry);

            case FaceControlType.BlendShape:
                return ApplyBlendShape(entry);

            default:
                return false;
        }
    }

    public bool ResetFace()
    {
        // Faceコマンドで設定したBlendShapeを解除
        ResetAllBlendShapes();

        // Faceコマンドによる上書きを解除し、
        // Animatorへ表情制御を戻す
        if (animator != null &&
            animatorLayer >= 0 &&
            animatorLayer < animator.layerCount)
        {
            animator.SetLayerWeight(animatorLayer, 0f);
        }

        CurrentFaceName = "";
        return true;
    }

    private bool ApplyAnimatorState(FaceEntry entry)
    {
        if (animator == null)
        {
            Debug.LogWarning(
                $"[unisanchan] Animatorが見つからないため、表情を変更できません。" +
                $"Actor: {name}"
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(entry.animatorStateName))
        {
            Debug.LogWarning(
                $"[unisanchan] Animator State名が空です。" +
                $"Actor: {name}, Face: {entry.faceName}"
            );

            return false;
        }

        animator.CrossFade(
            entry.animatorStateName,
            Mathf.Max(0f, crossFadeDuration),
            animatorLayer
        );

        animator.SetLayerWeight(animatorLayer, 1f);

        CurrentFaceName = entry.faceName;
        return true;
    }

    private bool ApplyBlendShape(FaceEntry entry)
    {
        if (entry.faceRenderer == null ||
            entry.faceRenderer.sharedMesh == null)
        {
            Debug.LogWarning(
                $"[unisanchan] Face Rendererが設定されていません。" +
                $"Actor: {name}, Face: {entry.faceName}"
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(entry.blendShapeName))
        {
            Debug.LogWarning(
                $"[unisanchan] BlendShape名が空です。" +
                $"Actor: {name}, Face: {entry.faceName}"
            );

            return false;
        }

        int index =
            entry.faceRenderer.sharedMesh.GetBlendShapeIndex(
                entry.blendShapeName
            );

        if (index < 0)
        {
            Debug.LogWarning(
                $"[unisanchan] BlendShapeが見つかりません。" +
                $"Actor: {name}, Face: {entry.faceName}, " +
                $"BlendShape: {entry.blendShapeName}"
            );

            return false;
        }

        entry.faceRenderer.SetBlendShapeWeight(
            index,
            entry.blendShapeWeight
        );

        CurrentFaceName = entry.faceName;
        return true;
    }

    private void ResetAllBlendShapes()
    {
        foreach (FaceEntry entry in faces)
        {
            if (entry == null ||
                entry.controlType != FaceControlType.BlendShape ||
                entry.faceRenderer == null ||
                entry.faceRenderer.sharedMesh == null ||
                string.IsNullOrWhiteSpace(entry.blendShapeName))
            {
                continue;
            }

            int index =
                entry.faceRenderer.sharedMesh.GetBlendShapeIndex(
                    entry.blendShapeName
                );

            if (index >= 0)
                entry.faceRenderer.SetBlendShapeWeight(index, 0f);
        }
    }

    private FaceEntry FindFace(string faceName)
    {
        if (string.IsNullOrWhiteSpace(faceName))
            return null;

        string target = faceName.Trim();

        foreach (FaceEntry entry in faces)
        {
            if (entry == null)
                continue;

            if (string.Equals(
                    entry.faceName?.Trim(),
                    target,
                    StringComparison.OrdinalIgnoreCase))
            {
                return entry;
            }
        }

        return null;
    }
}