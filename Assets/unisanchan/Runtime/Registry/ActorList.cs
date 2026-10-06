using System;
using System.Collections.Generic;
using UnityEngine;

public class ActorList : MonoBehaviour
{
    [Serializable]
    public class ActorEntry
    {
        [Tooltip("ExcelのTargetやArg欄から呼び出す名前")]
        public string actorName;

        [Tooltip("操作対象となるGameObject")]
        public GameObject targetObject;

        [Tooltip("特殊な構造のモデルでAnimatorを自動取得できない場合のみ指定")]
        public Animator animatorOverride;
    }

    [Header("開始時に登録するActor")]
    [SerializeField]
    private List<ActorEntry> initialActors = new();

    private readonly Dictionary<string, GameObject> actors = new();
    private readonly Dictionary<string, Animator> animatorOverrides = new();

    private void Awake()
    {
        RegisterInitialActors();
    }

    private void RegisterInitialActors()
    {
        actors.Clear();
        animatorOverrides.Clear();

        foreach (ActorEntry entry in initialActors)
        {
            if (entry == null)
                continue;

            if (string.IsNullOrWhiteSpace(entry.actorName))
            {
                Debug.LogWarning(
                    "[ActorList] 名前が空欄の登録項目を無視しました。",
                    this);
                continue;
            }

            if (entry.targetObject == null)
            {
                Debug.LogWarning(
                    $"[ActorList] Actor「{entry.actorName}」にObjectが設定されていません。",
                    this);
                continue;
            }

            Register(
                entry.actorName,
                entry.targetObject,
                entry.animatorOverride);
        }
    }

    public bool Register(
        string actorName,
        GameObject targetObject,
        Animator animatorOverride = null)
    {
        string normalizedName = Normalize(actorName);

        if (string.IsNullOrEmpty(normalizedName))
        {
            Debug.LogWarning("[ActorList] 空の名前では登録できません。");
            return false;
        }

        if (targetObject == null)
        {
            Debug.LogWarning(
                $"[ActorList] Actor「{actorName}」のObjectがnullです。");
            return false;
        }

        if (actors.ContainsKey(normalizedName))
        {
            Debug.LogWarning(
                $"[ActorList] Actor「{actorName}」はすでに登録されています。上書きします。");
        }

        actors[normalizedName] = targetObject;

        if (animatorOverride != null)
            animatorOverrides[normalizedName] = animatorOverride;
        else
            animatorOverrides.Remove(normalizedName);

        return true;
    }

    public bool Unregister(string actorName)
    {
        string normalizedName = Normalize(actorName);

        animatorOverrides.Remove(normalizedName);
        return actors.Remove(normalizedName);
    }

    public GameObject FindActor(string actorName)
    {
        string normalizedName = Normalize(actorName);

        if (actors.TryGetValue(normalizedName, out GameObject actor))
        {
            if (actor != null)
                return actor;

            actors.Remove(normalizedName);
            animatorOverrides.Remove(normalizedName);
        }

        return null;
    }

    public Animator FindAnimator(string actorName)
    {
        string normalizedName = Normalize(actorName);

        if (animatorOverrides.TryGetValue(
                normalizedName,
                out Animator overrideAnimator) &&
            overrideAnimator != null)
        {
            return overrideAnimator;
        }

        GameObject actor = FindActor(actorName);

        if (actor == null)
            return null;

        Animator animator = actor.GetComponent<Animator>();

        if (animator == null)
            animator = actor.GetComponentInChildren<Animator>(true);

        return animator;
    }

    public FaceController FindFaceController(string actorName)
    {
        GameObject actor = FindActor(actorName);

        if (actor == null)
            return null;

        FaceController faceController =
            actor.GetComponent<FaceController>();

        if (faceController == null)
        {
            faceController =
                actor.GetComponentInChildren<FaceController>(true);
        }

        return faceController;
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Trim().ToLowerInvariant();
    }
}