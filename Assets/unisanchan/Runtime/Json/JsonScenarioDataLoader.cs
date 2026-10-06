using System.IO;
using UnityEngine;

public static class JsonScenarioDataLoader
{
    public static ScenarioData Load(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("JSON file not found : " + path);
            return null;
        }

        Debug.Log("JSON Path : " + path);

        string json = File.ReadAllText(path);

        Debug.Log(
            "JSON contains cameraPresets : " +
            json.Contains("\"cameraPresets\"")
        );

        ScenarioData data =
            JsonUtility.FromJson<ScenarioData>(json);

        if (data == null)
        {
            Debug.LogError("JSONのデシリアライズに失敗しました。");
            return null;
        }

        Debug.Log(
            $"FromJson直後：Rows = {data.rows?.Count ?? -1}, " +
            $"CameraPresets = {data.cameraPresets?.Count ?? -1}, " +
            $"Images = {data.images?.Count ?? -1}"
        );

        if (data.rows == null)
            data.rows = new System.Collections.Generic.List<ScenarioRow>();

        if (data.cameraPresets == null)
            data.cameraPresets =
                new System.Collections.Generic.List<CameraPresetRow>();
        
        if (data.particlePresets == null)
            data.particlePresets =
                new System.Collections.Generic.List<ParticlePresetRow>();

        if (data.images == null)
            data.images =
                new System.Collections.Generic.List<ImageRow>();

        NormalizeBlockIds(data);

        Debug.Log("JSON Loaded : " + data.rows.Count);

        return data;
    }

    public static ScenarioData Load(TextAsset jsonAsset)
    {
        if (jsonAsset == null)
        {
            Debug.LogError(
                "[UnisanChi] Scenario JSON TextAsset が設定されていません。");
            return null;
        }

        ScenarioData data =
            JsonUtility.FromJson<ScenarioData>(jsonAsset.text);

        if (data == null)
        {
            Debug.LogError(
                "[UnisanChi] JSONのデシリアライズに失敗しました: " +
                jsonAsset.name);
            return null;
        }

        if (data.rows == null)
            data.rows =
                new System.Collections.Generic.List<ScenarioRow>();

        if (data.cameraPresets == null)
            data.cameraPresets =
                new System.Collections.Generic.List<CameraPresetRow>();
        
        if (data.particlePresets == null)
            data.particlePresets =
                new System.Collections.Generic.List<ParticlePresetRow>();

        if (data.images == null)
            data.images =
                new System.Collections.Generic.List<ImageRow>();

        NormalizeBlockIds(data);

        return data;
    }

    private static void NormalizeBlockIds(ScenarioData data)
    {
        string currentEventId = "";
        string currentTrigger = "";
        string currentBlockId = "";

        foreach (ScenarioRow row in data.rows)
        {
            if (row == null)
                continue;

            string eventId =
                row.EventID?.Trim() ?? "";

            // ラベル行はBlockID補完・警告の対象外
            if (eventId.StartsWith("*"))
            {
                currentEventId = eventId;
                currentTrigger = row.Trigger ?? "";
                currentBlockId = "";
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.BlockID))
            {
                if (eventId == currentEventId &&
                    row.Trigger == currentTrigger)
                {
                    row.BlockID = currentBlockId;
                }
                else
                {
                    Debug.LogWarning(
                        "[Excel Event System]\n" +
                        "【日本語】\n" +
                        "BlockID が空欄ですが、EventID または Trigger が変更されているため、" +
                        "前の BlockID は引き継がれませんでした。\n" +
                        "この行を同時実行したい場合は、BlockID を入力してください。\n\n" +

                        "[English]\n" +
                        "BlockID was omitted, but EventID or Trigger changed.\n" +
                        "The previous BlockID was not inherited.\n" +
                        "If this row should execute together with the previous one, please specify a BlockID.\n\n" +

                        $"EventID : {row.EventID}\n" +
                        $"Trigger : {row.Trigger}"
                    );
                }
            }
            else
            {
                currentBlockId = row.BlockID;
            }

            currentEventId = eventId;
            currentTrigger = row.Trigger ?? "";
        }
    }
}