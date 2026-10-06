using UnityEditor;
using UnityEngine;
using unisanchan;

public static class UnisanchanSaveLoadProjectSettingsProvider
{
    private const string SettingsPath =
        "Assets/unisanchan/Data/SceneLoadingSettings.asset";

    [SettingsProvider]
    public static SettingsProvider CreateProvider()
    {
        return new SettingsProvider(
            "Project/unisanchan/Save and Load",
            SettingsScope.Project)
        {
            label = "Save and Load",

            guiHandler = _ =>
            {
                SceneLoadingSettings settings =
                    AssetDatabase.LoadAssetAtPath<SceneLoadingSettings>(
                        SettingsPath);

                if (settings == null)
                {
                    EditorGUILayout.HelpBox(
                        $"設定アセットが見つかりません: {SettingsPath}",
                        MessageType.Error);
                    return;
                }

                var serialized = new SerializedObject(settings);
                serialized.Update();

                SerializedProperty useLoadingScene =
                    serialized.FindProperty("useLoadingScene");

                SerializedProperty loadingScenePath =
                    serialized.FindProperty("loadingScenePath");

                EditorGUILayout.PropertyField(
                    useLoadingScene,
                    new GUIContent("Use Loading Scene"));

                if (useLoadingScene.boolValue)
                {
                    SceneAsset currentScene =
                        AssetDatabase.LoadAssetAtPath<SceneAsset>(
                            loadingScenePath.stringValue);

                    SceneAsset selectedScene =
                        (SceneAsset)EditorGUILayout.ObjectField(
                            "Loading Scene",
                            currentScene,
                            typeof(SceneAsset),
                            false);

                    if (selectedScene != currentScene)
                    {
                        loadingScenePath.stringValue =
                            selectedScene == null
                                ? ""
                                : AssetDatabase.GetAssetPath(selectedScene);
                    }

                    if (string.IsNullOrEmpty(
                            loadingScenePath.stringValue))
                    {
                        EditorGUILayout.HelpBox(
                            "ロードシーンを指定してください。",
                            MessageType.Warning);
                    }
                }

                serialized.ApplyModifiedProperties();
            }
        };
    }
}