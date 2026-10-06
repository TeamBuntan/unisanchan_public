using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace unisanchan
{
    public sealed class SaveLoadMenu : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private RectTransform content;
        [SerializeField] private Button rowTemplate;
        [SerializeField] private GameSceneLauncher launcher;
        [SerializeField] private GameObject overwriteConfirmPanel;
        [SerializeField] private TMP_Text overwriteMessage;
        private int pendingOverwriteSlot = -1;

        private Button autoRow;
        private readonly List<Button> rows = new List<Button>();
        private bool isSaveMode;

        public void OpenSave() => Open(true);
        public void OpenLoad() => Open(false);

        [SerializeField] private ScenarioInputController scenarioInput;
        private bool scenarioInputWasEnabled;
        private bool hasScenarioInputState;
        [SerializeField] private ScenarioController scenarioController;

        public void Close()
        {
            CancelOverwrite();

            if (panel != null)
                panel.SetActive(false);
            
            if (hasScenarioInputState && scenarioInput != null)
                scenarioInput.enabled = scenarioInputWasEnabled;

            hasScenarioInputState = false;
        }

        private void Open(bool saveMode)
        {
            CancelOverwrite();
            
            if (panel == null || content == null ||
                rowTemplate == null || launcher == null)
            {
                Debug.LogError(
                    "[unisanchan] SaveLoadMenu の参照が不足しています。");
                return;
            }

            if (scenarioInput != null && scenarioController == null)
            {
                Debug.LogError(
                    "[unisanchan] SaveLoadMenu に ScenarioController が設定されていません。");
                return;
            }

            if (scenarioController != null &&
                !scenarioController.CanOpenSaveLoadMenu())
            {
                return;
            }

            if (scenarioInput != null)
            {
                if (!hasScenarioInputState)
                {
                    scenarioInputWasEnabled = scenarioInput.enabled;
                    hasScenarioInputState = true;
                }

                scenarioInput.enabled = false;
            }

            isSaveMode = saveMode;
            CreateRowsIfNeeded();
            RefreshRows();
            panel.SetActive(true);
        }

        private void CreateRowsIfNeeded()
        {
            if (rows.Count > 0)
                return;
            
            autoRow = Instantiate(rowTemplate, content);
            autoRow.name = "AutoSave";
            autoRow.transform.SetAsFirstSibling();
            autoRow.gameObject.SetActive(true);
            autoRow.onClick.RemoveAllListeners();
            autoRow.onClick.AddListener(() =>
            {
                Close();
                launcher.LoadAutoSave();
            });

            for (int i = 0; i < GameSaveFileStore.ManualSlotCount; i++)
            {
                int slotIndex = i;
                Button row = Instantiate(rowTemplate, content);
                row.name = $"Slot_{i + 1:00}";
                row.gameObject.SetActive(true);
                row.onClick.RemoveAllListeners();
                row.onClick.AddListener(() => SelectSlot(slotIndex));
                rows.Add(row);
            }
        }

        private void RefreshRows()
        {
            bool autoSaved =
                GameSaveFileStore.TryLoadAuto(out GameSaveData autoData);

            autoRow.interactable = !isSaveMode && autoSaved;

            TMP_Text autoLabel =
                autoRow.GetComponentInChildren<TMP_Text>(true);

            if (autoLabel != null)
            {
                if (autoSaved)
                {
                    string savedAt =
                        DateTime.TryParse(
                            autoData.SavedAtUtc, out DateTime date)
                        ? date.ToLocalTime().ToString("yyyy/MM/dd HH:mm")
                        : "";

                    autoLabel.text =
                        $"オートセーブ　{savedAt}\n" +
                        $"{autoData.EventID} / {autoData.BlockID}";
                }
                else
                {
                    autoLabel.text = "オートセーブ　空き";
                }
            }

            for (int i = 0; i < rows.Count; i++)
            {
                bool saved =
                    GameSaveFileStore.TryLoadManual(
                        i, out GameSaveData data);

                Button row = rows[i];
                row.interactable = isSaveMode || saved;

                TMP_Text label =
                    row.GetComponentInChildren<TMP_Text>(true);

                if (label == null)
                {
                    Debug.LogError(
                        "[unisanchan] スロット行に TMP テキストがありません。");
                    continue;
                }

                if (!saved)
                {
                    label.text = $"スロット {i + 1}　空き";
                    continue;
                }

                string savedAt =
                    DateTime.TryParse(
                        data.SavedAtUtc, out DateTime date)
                    ? date.ToLocalTime().ToString("yyyy/MM/dd HH:mm")
                    : "";

                label.text =
                    $"スロット {i + 1}　{savedAt}\n" +
                    $"{data.EventID} / {data.BlockID}";
            }
        }

        private void SelectSlot(int slotIndex)
        {
            if (isSaveMode)
            {
                if (GameSaveFileStore.TryLoadManual(slotIndex, out _))
                {
                    pendingOverwriteSlot = slotIndex;
                    overwriteMessage.text =
                        $"スロット {slotIndex + 1} に上書きしますか？";
                    overwriteConfirmPanel.SetActive(true);
                    return;
                }

                launcher.SaveManualSlot(slotIndex);
                RefreshRows();
                return;
            }
            else
            {
                Close();
                launcher.LoadManualSlot(slotIndex);
            }
        }

        public void ConfirmOverwrite()
        {
            if (pendingOverwriteSlot < 0)
                return;

            int slotIndex = pendingOverwriteSlot;
            pendingOverwriteSlot = -1;
            overwriteConfirmPanel.SetActive(false);

            launcher.SaveManualSlot(slotIndex);
            RefreshRows();
        }

        public void CancelOverwrite()
        {
            pendingOverwriteSlot = -1;
            overwriteConfirmPanel.SetActive(false);
        }
    }
}