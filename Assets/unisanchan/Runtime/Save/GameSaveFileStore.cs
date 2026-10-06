using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace unisanchan
{
    public static class GameSaveFileStore
    {
        public const int ManualSlotCount = 30;

        private static string SaveDirectory =>
            Path.Combine(Application.persistentDataPath, "unisanchan");

        public static bool TrySaveManual(
            int slotIndex,
            GameSaveData data)
        {
            if (slotIndex < 0 || slotIndex >= ManualSlotCount)
                return false;

            return TryWrite($"slot-{slotIndex}.json", data);
        }

        public static bool TrySaveAuto(GameSaveData data)
        {
            return TryWrite("autosave.json", data);
        }

        public static bool TryLoadManual(
            int slotIndex,
            out GameSaveData data)
        {
            data = null;

            if (slotIndex < 0 || slotIndex >= ManualSlotCount)
                return false;

            return TryRead($"slot-{slotIndex}.json", out data);
        }

        public static bool TryLoadAuto(out GameSaveData data)
        {
            return TryRead("autosave.json", out data);
        }

        private static bool TryRead(
            string fileName,
            out GameSaveData data)
        {
            string path = Path.Combine(SaveDirectory, fileName);

            if (TryReadFile(path, out data))
                return true;

            // 元ファイルを読めない場合、前回のバックアップを試す。
            return TryReadFile(path + ".bak", out data);
        }

        private static bool TryReadFile(
            string path,
            out GameSaveData data)
        {
            data = null;

            if (!File.Exists(path))
                return false;

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                GameSaveData loaded =
                    JsonUtility.FromJson<GameSaveData>(json);

                if (loaded == null ||
                    loaded.Version != 1 ||
                    string.IsNullOrWhiteSpace(loaded.CheckpointID) ||
                    string.IsNullOrWhiteSpace(loaded.ScenePath) ||
                    loaded.GameVariables?.Variables == null)
                {
                    Debug.LogWarning(
                        $"[unisanchan] セーブデータが不正です: {path}");
                    return false;
                }

                data = loaded;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[unisanchan] セーブデータを読めません: " +
                    $"{path}\n{exception}");
                return false;
            }
        }

        private static bool TryWrite(
            string fileName,
            GameSaveData data)
        {
            if (data == null)
                return false;

            string path = Path.Combine(SaveDirectory, fileName);
            string temporaryPath = path + ".tmp";

            try
            {
                Directory.CreateDirectory(SaveDirectory);

                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(
                    temporaryPath,
                    json,
                    Encoding.UTF8);

                if (File.Exists(path))
                {
                    File.Replace(
                        temporaryPath,
                        path,
                        path + ".bak");
                }
                else
                {
                    File.Move(temporaryPath, path);
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[unisanchan] セーブに失敗しました: " +
                    $"{path}\n{exception}");

                return false;
            }
        }
    }
}