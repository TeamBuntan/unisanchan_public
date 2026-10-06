using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace unisanchan
{
    public sealed class GameSceneLauncher : MonoBehaviour
    {
        [SerializeField] private string firstScenePath;
        [SerializeField] private ScenarioController scenarioController;
        public const int ManualSlotCount = 30;

        public void StartNewGame()
        {
            if (string.IsNullOrWhiteSpace(firstScenePath))
            {
                Debug.LogWarning(
                    "[unisanchan] ゲーム開始シーンが設定されていません。");
                return;
            }

            ScenarioLoadRequest.Clear();
            SceneManager.LoadScene(firstScenePath);
        }

        public void LoadAutoSave()
        {
            if (!GameSaveFileStore.TryLoadAuto(out GameSaveData data))
            {
                Debug.LogWarning(
                    "[unisanchan] 読み込めるオートセーブがありません。");
                return;
            }

            LoadSavedGame(data);
        }

        public void LoadManualSlot(int slotIndex)
        {
            if (!GameSaveFileStore.TryLoadManual(
                    slotIndex, out GameSaveData data))
            {
                Debug.LogWarning(
                    $"[unisanchan] スロット {slotIndex} を読み込めません。");
                return;
            }

            LoadSavedGame(data);
        }

        public void SaveManualSlot(int slotIndex)
        {
            if (scenarioController == null)
            {
                Debug.LogError(
                    "[unisanchan] SaveLoadSystem に ScenarioController が設定されていません。");
                return;
            }

            scenarioController.SaveManualSlot(slotIndex);
        }

        private void LoadSavedGame(GameSaveData data)
        {
            ScenarioLoadRequest.Queue(data);

            try
            {
                SceneManager.LoadScene(data.ScenePath);
            }
            catch (Exception exception)
            {
                ScenarioLoadRequest.Clear();
                Debug.LogError(
                    $"[unisanchan] シーンを開けません: " +
                    $"{data.ScenePath}\n{exception}");
            }
        }
    }
}