using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ScenarioPlayModeValidator
{
    static ScenarioPlayModeValidator()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(
        PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
            return;

        ScenarioController controller =
            Object.FindFirstObjectByType<ScenarioController>(
                FindObjectsInactive.Include);

        if (controller == null)
            return;

        SerializedObject serializedController =
            new SerializedObject(controller);

        SerializedProperty registryProperty =
            serializedController.FindProperty("scenarioRegistry");

        ScenarioRegistry registry =
            registryProperty?.objectReferenceValue as ScenarioRegistry;

        if (registry == null)
            return;

        foreach (ScenarioEntry entry in registry.Scenarios)
        {
            if (entry == null || entry.Source == null)
                continue;

            ScenarioData data =
                JsonScenarioDataLoader.Load(entry.Source);

            if (data == null || data.rows == null)
                continue;

            List<ScenarioEvent> scenarioEvents =
                new List<ScenarioEvent>();

            if (ScenarioStructureBuilder.TryBuild(
                    data.rows,
                    scenarioEvents))
            {
                continue;
            }

            Debug.LogError(
                "[unisanchan] シナリオデータに構造エラーがあるため、" +
                "Play Modeへの移行を中止しました。\n" +
                $"対象Source: {entry.Source.name}"
            );

            EditorApplication.ExitPlaymode();
            return;
        }
    }
}