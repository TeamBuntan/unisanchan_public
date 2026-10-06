using System.Collections.Generic;
using UnityEngine;

public static class JsonPresetDataLoader
{
    public static PresetData Load(TextAsset jsonAsset)
    {
        if (jsonAsset == null)
        {
            Debug.LogError(
                "[unisanchan] Preset JSON TextAsset が設定されていません。");

            return null;
        }

        PresetData data =
            JsonUtility.FromJson<PresetData>(
                jsonAsset.text);

        if (data == null)
        {
            Debug.LogError(
                "[unisanchan] Preset JSONの読み込みに失敗しました: "
                + jsonAsset.name);

            return null;
        }

        if (data.cameraPresets == null)
        {
            data.cameraPresets =
                new List<CameraPresetRow>();
        }

        if (data.particlePresets == null)
        {
            data.particlePresets =
                new List<ParticlePresetRow>();
        }

        if (data.modalPresets == null)
        {
            data.modalPresets =
                new List<ModalPresetRow>();
        }

        return data;
    }
}