#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ImageRegistryBuilder
{
    private const string ImageRegistryPath =
        "Assets/unisanchan/Data/ImageRegistry.asset";

    [MenuItem("Tools/unisanchan/Rebuild Image Registry")]
    public static void Rebuild()
    {
        ImageRegistry registry =
            AssetDatabase.LoadAssetAtPath<ImageRegistry>(
                ImageRegistryPath);

        if (registry == null)
        {
            Debug.LogError(
                "[unisanchan] ImageRegistryが見つかりません: " +
                ImageRegistryPath);

            return;
        }

        if (registry.SourceFolders == null ||
            registry.SourceFolders.Count == 0)
        {
            Debug.LogWarning(
                "[unisanchan] Image Root Folderが登録されていません。");

            return;
        }

        Dictionary<string, string> existingIdsByAssetKey =
            BuildExistingIdMap(registry);

        var newEntries =
            new List<ImageRegistry.Entry>();

        var registeredIds =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        bool hasError = false;

        foreach (UnityEngine.Object folder
                 in registry.SourceFolders)
        {
            if (folder == null)
                continue;

            string rootPath =
                AssetDatabase.GetAssetPath(folder);

            if (!AssetDatabase.IsValidFolder(rootPath))
            {
                Debug.LogError(
                    "[unisanchan] フォルダではないAssetが登録されています: " +
                    rootPath);

                hasError = true;
                continue;
            }

            string[] guids =
                AssetDatabase.FindAssets(
                    "t:Sprite",
                    new[] { rootPath });

            var processedPaths =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (string guid in guids)
            {
                string assetPath =
                    AssetDatabase.GUIDToAssetPath(guid);

                if (!processedPaths.Add(assetPath))
                    continue;

                UnityEngine.Object[] assets =
                    AssetDatabase.LoadAllAssetsAtPath(assetPath);

                var sprites = new List<Sprite>();

                foreach (UnityEngine.Object asset in assets)
                {
                    if (asset is Sprite sprite)
                        sprites.Add(sprite);
                }

                if (sprites.Count == 0)
                    continue;

                string relativePath =
                    assetPath.Substring(rootPath.Length)
                        .TrimStart('/', '\\');

                relativePath =
                    Path.ChangeExtension(relativePath, null)
                        .Replace("\\", "/");

                string type =
                    GetTypeName(
                        rootPath,
                        relativePath);

                if (sprites.Count == 1)
                {
                    AddEntry(
                    relativePath,
                    type,
                    sprites[0],
                    existingIdsByAssetKey,
                    registeredIds,
                    newEntries,
                    ref hasError);
                }
                else
                {
                    foreach (Sprite sprite in sprites)
                    {
                        string imageId =
                            relativePath + "/" + sprite.name;

                        AddEntry(
                            imageId,
                            type,
                            sprite,
                            existingIdsByAssetKey,
                            registeredIds,
                            newEntries,
                            ref hasError);
                    }
                }
            }
        }

        if (hasError)
        {
            Debug.LogError(
                "[unisanchan] ImageRegistryの再構築を中止しました。" +
                "上記エラーを修正してください。");

            return;
        }

        newEntries.Sort(
            (a, b) =>
                StringComparer.OrdinalIgnoreCase.Compare(
                    a.ImageID,
                    b.ImageID));

        Undo.RecordObject(
            registry,
            "Rebuild Image Registry");

        registry.Entries.Clear();
        registry.Entries.AddRange(newEntries);

        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();

        Debug.Log(
            "[unisanchan] ImageRegistryを更新しました。" +
            $"登録数: {registry.Entries.Count}");
    }

    private static void AddEntry(
        string generatedImageId,
        string type,
        Sprite sprite,
        Dictionary<string, string> existingIdsByAssetKey,
        HashSet<string> registeredIds,
        List<ImageRegistry.Entry> entries,
        ref bool hasError)
    {
        if (string.IsNullOrWhiteSpace(generatedImageId) ||
            sprite == null)
        {
            return;
        }

        if (!AssetDatabase
            .TryGetGUIDAndLocalFileIdentifier(
                sprite,
                out string assetGuid,
                out long localFileId))
        {
            Debug.LogError(
                "[unisanchan] Spriteの識別情報を取得できません: " +
                sprite.name);

            hasError = true;
            return;
        }

        string assetKey =
            CreateAssetKey(
                assetGuid,
                localFileId);

        string imageId = generatedImageId.Trim();

        if (existingIdsByAssetKey != null &&
            existingIdsByAssetKey.TryGetValue(
                assetKey,
                out string existingImageId) &&
            !string.IsNullOrWhiteSpace(existingImageId))
        {
            imageId = existingImageId.Trim();
        }

        if (!registeredIds.Add(imageId))
        {
            Debug.LogError(
                "[unisanchan] ImageIDが重複しています: " +
                imageId);

            hasError = true;
            return;
        }

        entries.Add(
            new ImageRegistry.Entry
            {
                ImageID = imageId,
                Type = type,
                Sprite = sprite,
                AssetGuid = assetGuid,
                LocalFileId = localFileId
            });
    }
    
    private static string GetTypeName(
        string rootPath,
        string relativePath)
    {
        int separatorIndex =
            relativePath.IndexOf('/');

        if (separatorIndex > 0)
        {
            return relativePath.Substring(
                0,
                separatorIndex);
        }

        return Path.GetFileName(rootPath);
    }

    private static Dictionary<string, string>
        BuildExistingIdMap(ImageRegistry registry)
    {
        var result =
            new Dictionary<string, string>(
                StringComparer.Ordinal);

        if (registry == null ||
            registry.Entries == null)
        {
            return result;
        }

        foreach (ImageRegistry.Entry entry
                in registry.Entries)
        {
            if (entry == null ||
                entry.Sprite == null ||
                string.IsNullOrWhiteSpace(entry.ImageID))
            {
                continue;
            }

            if (!AssetDatabase
                .TryGetGUIDAndLocalFileIdentifier(
                    entry.Sprite,
                    out string assetGuid,
                    out long localFileId))
            {
                continue;
            }

            string assetKey =
                CreateAssetKey(
                    assetGuid,
                    localFileId);

            if (!result.ContainsKey(assetKey))
            {
                result.Add(
                    assetKey,
                    entry.ImageID.Trim());
            }
        }

        return result;
    }

    private static string CreateAssetKey(
        string assetGuid,
        long localFileId)
    {
        return assetGuid + ":" + localFileId;
    }
}

#endif