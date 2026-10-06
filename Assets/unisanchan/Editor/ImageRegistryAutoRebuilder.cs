#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

public class ImageRegistryAutoRebuilder : AssetPostprocessor
{
    private const string ImageRegistryPath =
        "Assets/unisanchan/Data/ImageRegistry.asset";

    private static bool rebuildScheduled;

    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (rebuildScheduled)
            return;

        ImageRegistry registry =
            AssetDatabase.LoadAssetAtPath<ImageRegistry>(
                ImageRegistryPath);

        if (registry == null ||
            registry.SourceFolders == null)
        {
            return;
        }

        if (!ContainsRegisteredPath(
                registry,
                importedAssets) &&
            !ContainsRegisteredPath(
                registry,
                deletedAssets) &&
            !ContainsRegisteredPath(
                registry,
                movedAssets) &&
            !ContainsRegisteredPath(
                registry,
                movedFromAssetPaths))
        {
            return;
        }

        rebuildScheduled = true;

        EditorApplication.delayCall +=
            RunRebuild;
    }

    private static bool ContainsRegisteredPath(
        ImageRegistry registry,
        string[] assetPaths)
    {
        if (assetPaths == null)
            return false;

        foreach (string assetPath in assetPaths)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                continue;

            foreach (UnityEngine.Object folder
                     in registry.SourceFolders)
            {
                if (folder == null)
                    continue;

                string rootPath =
                    AssetDatabase.GetAssetPath(folder);

                if (string.IsNullOrWhiteSpace(rootPath))
                    continue;

                if (string.Equals(
                        assetPath,
                        rootPath,
                        StringComparison.OrdinalIgnoreCase) ||
                    assetPath.StartsWith(
                        rootPath + "/",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void RunRebuild()
    {
        rebuildScheduled = false;

        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        ImageRegistryBuilder.Rebuild();
    }
}

#endif