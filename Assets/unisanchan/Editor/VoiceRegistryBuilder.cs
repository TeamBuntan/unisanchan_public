#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEngine;

public static class VoiceRegistryBuilder
{
    private const string VoiceRegistryPath =
        "Assets/unisanchan/Data/VoiceRegistry.asset";

    [MenuItem("Tools/unisanchan/Rebuild Voice Registry")]
    public static void Rebuild()
    {
        EventSystemSettings settings =
            Object.FindFirstObjectByType<EventSystemSettings>();

        if (settings == null)
        {
            Debug.LogWarning(
                "[unisanchan] EventSystemSettings が見つかりません。");
            return;
        }

        if (settings.VoiceRootFolder == null)
        {
            Debug.LogWarning(
                "[unisanchan] Voice Root Folder が設定されていません。");
            return;
        }

        string rootPath =
            AssetDatabase.GetAssetPath(settings.VoiceRootFolder);

        if (string.IsNullOrWhiteSpace(rootPath))
        {
            Debug.LogWarning(
                "[unisanchan] Voice Root Folder のパスを取得できません。");
            return;
        }

        VoiceRegistry registry =
            AssetDatabase.LoadAssetAtPath<VoiceRegistry>(
                VoiceRegistryPath);

        if (registry == null)
        {
            Debug.LogError(
                "[unisanchan] VoiceRegistry が見つかりません: " +
                VoiceRegistryPath);
            return;
        }

        registry.Entries.Clear();

        string[] guids =
            AssetDatabase.FindAssets(
                "t:AudioClip",
                new[] { rootPath });

        foreach (string guid in guids)
        {
            string assetPath =
                AssetDatabase.GUIDToAssetPath(guid);

            AudioClip clip =
                AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);

            if (clip == null)
                continue;

            string relativePath =
                assetPath.Substring(rootPath.Length)
                    .TrimStart('/', '\\');

            relativePath =
                Path.ChangeExtension(relativePath, null)
                    .Replace("\\", "/");

            registry.Entries.Add(
                new VoiceRegistry.Entry
                {
                    VoicePath = relativePath,
                    Clip = clip
                });
        }

        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"[unisanchan] VoiceRegistry を更新しました。登録数: {registry.Entries.Count}");
    }
}

#endif