using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CameraProjectSettingsProvider
{
    private const string CameraAssetRegistryPath =
        "Assets/Resources/Camera/CameraAssetRegistry.asset";

    private static SerializedObject serializedRegistry;

    [SettingsProvider]
    public static SettingsProvider CreateCameraAssetsProvider()
    {
        return new SettingsProvider(
            "Project/unisanchan/Camera/Assets",
            SettingsScope.Project)
        {
            label = "Assets",
            guiHandler = _ =>
                DrawCameraAssetsSettings()
        };
    }

    private static void DrawCameraAssetsSettings()
    {
        if (!TryPrepareRegistry())
            return;

        SerializedProperty entriesProperty =
            serializedRegistry.FindProperty(
                "entries");

        EditorGUILayout.PropertyField(
            entriesProperty,
            new GUIContent("Camera Assets"),
            true);

        serializedRegistry.ApplyModifiedProperties();

        DrawValidationMessages(
            (CameraAssetRegistry)serializedRegistry.targetObject);
    }

    private static void DrawValidationMessages(
        CameraAssetRegistry registry)
    {
        if (registry == null)
            return;

        HashSet<string> usedIds =
            new HashSet<string>(
                StringComparer.Ordinal);

        foreach (CameraAssetRegistryEntry entry
                in registry.Entries)
        {
            if (entry == null)
                continue;

            string cameraAssetId =
                entry.CameraAssetId?.Trim() ?? "";

            if (string.IsNullOrEmpty(cameraAssetId))
            {
                EditorGUILayout.HelpBox(
                    "Camera Asset IDが空の登録があります。",
                    MessageType.Error);

                continue;
            }

            if (!usedIds.Add(cameraAssetId))
            {
                EditorGUILayout.HelpBox(
                    $"Camera Asset IDが重複しています: {cameraAssetId}",
                    MessageType.Error);
            }

            if (entry.Prefab == null)
            {
                EditorGUILayout.HelpBox(
                    $"Prefabが設定されていません: {cameraAssetId}",
                    MessageType.Error);

                continue;
            }

            Camera camera =
                entry.Prefab.GetComponentInChildren<Camera>(
                    true);

            if (camera == null)
            {
                EditorGUILayout.HelpBox(
                    $"Prefab内にCameraコンポーネントがありません: {cameraAssetId}",
                    MessageType.Error);
            }
        }
    }

    private static bool TryPrepareRegistry()
    {
        CameraAssetRegistry registry =
            AssetDatabase.LoadAssetAtPath
                <CameraAssetRegistry>(
                    CameraAssetRegistryPath);

        if (registry == null)
        {
            EditorGUILayout.HelpBox(
                "CameraAssetRegistry.assetが見つかりません。",
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