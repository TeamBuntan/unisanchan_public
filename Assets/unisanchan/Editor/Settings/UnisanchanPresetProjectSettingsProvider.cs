using UnityEditor;
using UnityEngine;

public static class UnisanchanPresetProjectSettingsProvider
{
    private const string PresetRegistryPath =
        "Assets/unisanchan/Data/PresetRegistry.asset";

    private static SerializedObject serializedRegistry;
    private static SerializedProperty excelSourcesProperty;
    private static SerializedProperty sourcesProperty;

    [SettingsProvider]
    public static SettingsProvider CreateProvider()
    {
        SettingsProvider provider =
            new SettingsProvider(
                "Project/unisanchan/Presets",
                SettingsScope.Project)
            {
                label = "Presets",

                guiHandler = _ =>
                {
                    PresetRegistry registry =
                        AssetDatabase.LoadAssetAtPath<PresetRegistry>(
                            PresetRegistryPath);

                    if (registry == null)
                    {
                        EditorGUILayout.HelpBox(
                            "PresetRegistry.asset が見つかりません。",
                            MessageType.Error);

                        return;
                    }

                    if (serializedRegistry == null ||
                        serializedRegistry.targetObject != registry)
                    {
                        serializedRegistry =
                            new SerializedObject(registry);

                        excelSourcesProperty =
                            serializedRegistry.FindProperty(
                                "excelSources");

                        sourcesProperty =
                            serializedRegistry.FindProperty(
                                "sources");
                    }

                    serializedRegistry.Update();

                    EditorGUILayout.LabelField(
                        "Preset Excel Files",
                        EditorStyles.boldLabel);

                    EditorGUILayout.HelpBox(
                        "プリセットを定義した.xlsxファイルを登録します。" +
                        "保存するとJSONへ自動変換されます。",
                        MessageType.Info);

                    EditorGUILayout.PropertyField(
                        excelSourcesProperty,
                        new GUIContent("Excel Sources"),
                        true);

                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField(
                        "Generated Preset Data",
                        EditorStyles.boldLabel);

                    EditorGUI.BeginDisabledGroup(true);

                    EditorGUILayout.PropertyField(
                        sourcesProperty,
                        new GUIContent("Generated Sources"),
                        true);

                    EditorGUI.EndDisabledGroup();

                    serializedRegistry.ApplyModifiedProperties();
                }
            };

        return provider;
    }
}