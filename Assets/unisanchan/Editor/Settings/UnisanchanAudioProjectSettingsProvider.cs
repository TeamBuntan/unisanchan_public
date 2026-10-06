using UnityEditor;
using UnityEngine;

public static class UnisanchanAudioProjectSettingsProvider
{
    private const string AudioRegistryPath =
        "Assets/unisanchan/Data/AudioRegistry.asset";

    private static SerializedObject serializedRegistry;

    [SettingsProvider]
    public static SettingsProvider CreateBgmProvider()
    {
        return new SettingsProvider(
            "Project/unisanchan/Audio/BGM",
            SettingsScope.Project)
        {
            label = "BGM",
            guiHandler = _ => DrawBgmSettings()
        };
    }

    [SettingsProvider]
    public static SettingsProvider CreateAmbienceProvider()
    {
        return new SettingsProvider(
            "Project/unisanchan/Audio/Ambience",
            SettingsScope.Project)
        {
            label = "Ambience",
            guiHandler = _ => DrawAmbienceSettings()
        };
    }

    [SettingsProvider]
    public static SettingsProvider CreateSeProvider()
    {
        return new SettingsProvider(
            "Project/unisanchan/Audio/SE",
            SettingsScope.Project)
        {
            label = "SE",
            guiHandler = _ => DrawSeSettings()
        };
    }

    private static void DrawBgmSettings()
    {
        if (!TryPrepareRegistry())
            return;

        SerializedProperty bgmEntriesProperty =
            serializedRegistry.FindProperty("bgmEntries");

        EditorGUILayout.PropertyField(
            bgmEntriesProperty,
            new GUIContent("BGM"),
            true);

        serializedRegistry.ApplyModifiedProperties();
    }

    private static void DrawAmbienceSettings()
    {
        if (!TryPrepareRegistry())
            return;

        SerializedProperty ambienceEntriesProperty =
            serializedRegistry.FindProperty("ambienceEntries");

        EditorGUILayout.PropertyField(
            ambienceEntriesProperty,
            new GUIContent("Ambience"),
            true);

        serializedRegistry.ApplyModifiedProperties();
    }

    private static void DrawSeSettings()
    {
        if (!TryPrepareRegistry())
            return;

        SerializedProperty seEntriesProperty =
            serializedRegistry.FindProperty("seEntries");

        EditorGUILayout.PropertyField(
            seEntriesProperty,
            new GUIContent("SE"),
            true);

        EditorGUILayout.Space();

        SerializedProperty maxSimultaneousSeProperty =
            serializedRegistry.FindProperty("maxSimultaneousSe");

        EditorGUILayout.PropertyField(
            maxSimultaneousSeProperty,
            new GUIContent(
                "Max Simultaneous SE",
                "同じ瞬間に再生できるSEの最大数です。SE素材の登録可能数ではありません。"));

        serializedRegistry.ApplyModifiedProperties();
    }

    private static bool TryPrepareRegistry()
    {
        AudioRegistry registry =
            AssetDatabase.LoadAssetAtPath<AudioRegistry>(
                AudioRegistryPath);

        if (registry == null)
        {
            EditorGUILayout.HelpBox(
                "AudioRegistry.assetが見つかりません。",
                MessageType.Error);

            return false;
        }

        if (serializedRegistry == null ||
            serializedRegistry.targetObject != registry)
        {
            serializedRegistry =
                new SerializedObject(registry);
        }

        serializedRegistry.Update();

        return true;
    }
}