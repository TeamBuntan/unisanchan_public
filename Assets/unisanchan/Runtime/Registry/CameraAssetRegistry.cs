using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CameraAssetRegistryEntry
{
    [Tooltip("ExcelからCamera Prefabを指定するときに使用するID")]
    public string CameraAssetId;

    [Tooltip("ゲーム中に生成するCamera Prefab")]
    public GameObject Prefab;

    [Tooltip("このCamera Assetの用途を記録する任意の説明")]
    [TextArea]
    public string Description;
}

[CreateAssetMenu(
    fileName = "CameraAssetRegistry",
    menuName = "unisanchan/Camera Asset Registry")]
public class CameraAssetRegistry : ScriptableObject
{
    [SerializeField]
    private List<CameraAssetRegistryEntry> entries = new();

    public IReadOnlyList<CameraAssetRegistryEntry> Entries
        => entries;

    public CameraAssetRegistryEntry FindAsset(
        string cameraAssetId)
    {
        if (entries == null ||
            string.IsNullOrWhiteSpace(cameraAssetId))
        {
            return null;
        }

        string normalizedId =
            cameraAssetId.Trim();

        foreach (CameraAssetRegistryEntry entry in entries)
        {
            if (entry == null)
                continue;

            if (string.Equals(
                    entry.CameraAssetId?.Trim(),
                    normalizedId,
                    StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }
}