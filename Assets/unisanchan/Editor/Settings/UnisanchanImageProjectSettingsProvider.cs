#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

public static class UnisanchanImageProjectSettingsProvider
{
    private const string ImageRegistryPath =
        "Assets/unisanchan/Data/ImageRegistry.asset";

    private static SerializedObject serializedRegistry;
    private static SerializedProperty sourceFoldersProperty;
    private static SerializedProperty entriesProperty;

    [SettingsProvider]
    public static SettingsProvider CreateProvider()
    {
        var provider = new SettingsProvider(
            "Project/unisanchan/Images",
            SettingsScope.Project)
        {
            label = "Images",

            guiHandler = _ =>
            {
                DrawSettings();
            }
        };

        return provider;
    }

    private static void DrawSettings()
    {
        ImageRegistry registry =
            AssetDatabase.LoadAssetAtPath<ImageRegistry>(
                ImageRegistryPath);

        if (registry == null)
        {
            EditorGUILayout.HelpBox(
                "ImageRegistry.assetが見つかりません。\n" +
                ImageRegistryPath,
                MessageType.Error);

            return;
        }

        PrepareSerializedProperties(registry);

        serializedRegistry.Update();

        EditorGUILayout.LabelField(
            "Image Root Folders",
            EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "登録したフォルダと、その配下にあるSpriteを再帰的に検索します。\n" +
            "Excelや将来のノードでは、ファイルパスではなくImageIDを使用します。",
            MessageType.Info);

        EditorGUILayout.Space();

        DrawFolderList();

        EditorGUILayout.Space();

        if (GUILayout.Button("+ Add Image Root Folder"))
        {
            int newIndex =
                sourceFoldersProperty.arraySize;

            sourceFoldersProperty.InsertArrayElementAtIndex(
                newIndex);

            sourceFoldersProperty
                .GetArrayElementAtIndex(newIndex)
                .objectReferenceValue = null;
        }

        serializedRegistry.ApplyModifiedProperties();

        EditorGUILayout.Space(12);

        if (GUILayout.Button(
                "Rebuild Image Registry",
                GUILayout.Height(28)))
        {
            serializedRegistry.ApplyModifiedProperties();

            ImageRegistryBuilder.Rebuild();

            serializedRegistry.Update();
        }

        EditorGUILayout.Space();

        int imageCount =
            entriesProperty != null
                ? entriesProperty.arraySize
                : 0;

        EditorGUILayout.LabelField(
            $"Registered Images: {imageCount}");
    }

    private static void PrepareSerializedProperties(
        ImageRegistry registry)
    {
        if (serializedRegistry != null &&
            serializedRegistry.targetObject == registry)
        {
            return;
        }

        serializedRegistry =
            new SerializedObject(registry);

        sourceFoldersProperty =
            serializedRegistry.FindProperty(
                "sourceFolders");

        entriesProperty =
            serializedRegistry.FindProperty(
                "Entries");
    }

    private static void DrawFolderList()
    {
        if (sourceFoldersProperty == null)
        {
            EditorGUILayout.HelpBox(
                "ImageRegistryのsourceFoldersを取得できません。",
                MessageType.Error);

            return;
        }

        for (int i = 0;
             i < sourceFoldersProperty.arraySize;
             i++)
        {
            SerializedProperty element =
                sourceFoldersProperty
                    .GetArrayElementAtIndex(i);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            UnityEngine.Object currentFolder =
                element.objectReferenceValue;

            UnityEngine.Object newFolder =
                EditorGUILayout.ObjectField(
                    currentFolder,
                    typeof(DefaultAsset),
                    false);

            if (newFolder != currentFolder)
            {
                element.objectReferenceValue =
                    newFolder;
            }

            if (GUILayout.Button(
                    "-",
                    GUILayout.Width(24)))
            {
                RemoveFolderAt(i);

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                break;
            }

            EditorGUILayout.EndHorizontal();

            DrawFolderValidation(
                i,
                element.objectReferenceValue);

            EditorGUILayout.EndVertical();
        }
    }

    private static void DrawFolderValidation(
        int index,
        UnityEngine.Object folder)
    {
        if (folder == null)
        {
            EditorGUILayout.HelpBox(
                "Image Root Folderが設定されていません。",
                MessageType.Warning);

            return;
        }

        string folderPath =
            AssetDatabase.GetAssetPath(folder);

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            EditorGUILayout.HelpBox(
                "フォルダを登録してください。",
                MessageType.Error);

            return;
        }

        EditorGUILayout.LabelField(
            folderPath,
            EditorStyles.miniLabel);

        for (int i = 0;
             i < sourceFoldersProperty.arraySize;
             i++)
        {
            if (i == index)
                continue;

            UnityEngine.Object otherFolder =
                sourceFoldersProperty
                    .GetArrayElementAtIndex(i)
                    .objectReferenceValue;

            if (otherFolder == null)
                continue;

            string otherPath =
                AssetDatabase.GetAssetPath(
                    otherFolder);

            if (string.Equals(
                    folderPath,
                    otherPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                EditorGUILayout.HelpBox(
                    "同じImage Root Folderが重複しています。",
                    MessageType.Error);

                return;
            }
        }
    }

    private static void RemoveFolderAt(int index)
    {
        int previousSize =
            sourceFoldersProperty.arraySize;

        sourceFoldersProperty
            .DeleteArrayElementAtIndex(index);

        // Object参照の配列は、最初の削除でnullになる場合がある。
        if (sourceFoldersProperty.arraySize ==
            previousSize)
        {
            sourceFoldersProperty
                .DeleteArrayElementAtIndex(index);
        }
    }
}

#endif