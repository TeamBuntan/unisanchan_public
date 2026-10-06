using System.Collections.Generic;
using UnityEngine;

namespace unisanchan
{
    public sealed class ScenarioSaveCoordinator
    {
        private readonly ScenarioVariableStore variableStore;
        private readonly IReadOnlyList<VariableDefinition> definitions;
        private GameSaveData latestCheckpointData;

        public ScenarioSaveCoordinator(
            ScenarioVariableStore variableStore,
            IReadOnlyList<VariableDefinition> definitions)
        {
            this.variableStore = variableStore;
            this.definitions = definitions;
        }

        public void RecordCheckpoint(
            ScenarioRow row,
            string scenePath,
            bool autoSave)
        {
            if (row == null || string.IsNullOrWhiteSpace(scenePath))
            {
                Debug.LogError(
                    "[unisanchan] チェックポイントを記録できません。");
                return;
            }

            latestCheckpointData = new GameSaveData
            {
                CheckpointID = row.Arg1?.Trim(),
                ScenePath = scenePath,
                EventID = row.EventID?.Trim(),
                BlockID = row.BlockID?.Trim(),
                Trigger = row.Trigger?.Trim(),
                SavedAtUtc = System.DateTime.UtcNow.ToString("O"),
                GameVariables = variableStore.Capture(
                    definitions,
                    VariableSaveType.Game)
            };

            if (autoSave &&
                GameSaveFileStore.TrySaveAuto(latestCheckpointData))
            {
                Debug.Log(
                    $"[unisanchan] オートセーブしました: " +
                    $"{latestCheckpointData.CheckpointID}");
            }
        }

        public bool TryRestoreCheckpoint(GameSaveData data)
        {
            if (data == null ||
                !variableStore.Restore(
                    data.GameVariables,
                    definitions,
                    VariableSaveType.Game))
            {
                return false;
            }

            latestCheckpointData = data;
            return true;
        }

        public bool TrySaveManualSlot(int slotIndex)
        {
            if (latestCheckpointData == null)
            {
                Debug.LogWarning(
                    "[unisanchan] まだセーブポイントを通過していません。");
                return false;
            }

            var data = new GameSaveData
            {
                Version = latestCheckpointData.Version,
                CheckpointID = latestCheckpointData.CheckpointID,
                ScenePath = latestCheckpointData.ScenePath,
                EventID = latestCheckpointData.EventID,
                BlockID = latestCheckpointData.BlockID,
                Trigger = latestCheckpointData.Trigger,
                SavedAtUtc = System.DateTime.UtcNow.ToString("O"),
                GameVariables = latestCheckpointData.GameVariables
            };

            bool saved = GameSaveFileStore.TrySaveManual(
                slotIndex,
                data);

            if (saved)
            {
                Debug.Log(
                    $"[unisanchan] スロット {slotIndex + 1} にセーブしました: " +
                    $"{data.CheckpointID}");
            }

            return saved;
        }
    }
}