using UnityEditor;
using UnityEngine;

public static class UnisanchanParticleProjectSettingsProvider
{
    private const string ParticleRegistryPath =
        "Assets/unisanchan/Data/ParticleRegistry.asset";

    private static SerializedObject serializedRegistry;

    [SettingsProvider]
    public static SettingsProvider CreateParticleAssetsProvider()
    {
        return new SettingsProvider(
            "Project/unisanchan/Particle/Assets",
            SettingsScope.Project)
        {
            label = "Assets",
            guiHandler = _ =>
                DrawParticleAssetsSettings()
        };
    }

    private static void DrawParticleAssetsSettings()
    {
        if (!TryPrepareRegistry())
            return;

        SerializedProperty entriesProperty =
            serializedRegistry.FindProperty(
                "entries");

        EditorGUILayout.PropertyField(
            entriesProperty,
            new GUIContent("Particle Assets"),
            true);

        serializedRegistry.ApplyModifiedProperties();
    }

    private static void DrawSpawnPresetsSettings()
    {
        if (!TryPrepareRegistry())
            return;

        SerializedProperty spawnPresetsProperty =
            serializedRegistry.FindProperty(
                "spawnPresets");

        EditorGUILayout.PropertyField(
            spawnPresetsProperty,
            new GUIContent("Spawn Presets"),
            true);

        serializedRegistry.ApplyModifiedProperties();
    }

    private static bool TryPrepareRegistry()
    {
        ParticleRegistry registry =
            AssetDatabase.LoadAssetAtPath
                <ParticleRegistry>(
                    ParticleRegistryPath);

        if (registry == null)
        {
            EditorGUILayout.HelpBox(
                "ParticleRegistry.assetが見つかりません。",
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