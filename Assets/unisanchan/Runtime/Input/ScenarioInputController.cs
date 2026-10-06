using System.Collections.Generic;
using UnityEngine;

public class ScenarioInputController : MonoBehaviour
{
    [Header("System")]
    [SerializeField] private bool useBuiltInInput = true;
    [SerializeField] private ScenarioController scenarioController;

    [Header("Input Bindings")]
    [SerializeField] private ScenarioInputBinding[] bindings;

    private void Update()
    {
        if (!useBuiltInInput)
            return;

        if (bindings == null)
            return;

        foreach (ScenarioInputBinding binding in bindings)
        {
            if (binding == null)
                continue;

            if (IsAnyKeyPressed(binding.Keys))
            {
                SendInput(binding.Action);
            }
        }
    }

    private bool IsAnyKeyPressed(KeyCode[] keys)
    {
        if (keys == null)
            return false;

        foreach (KeyCode key in keys)
        {
            if (Input.GetKeyDown(key))
                return true;
        }

        return false;
    }

    public void SendInput(ScenarioInputAction action)
    {
        if (!isActiveAndEnabled)
            return;

        if (scenarioController == null)
        {
            Debug.LogWarning(
                "[unisanchan] ScenarioControllerが設定されていないため、入力を送れません。"
            );

            return;
        }

        scenarioController.RequestInput(action);
    }

    /// <summary>
    /// 指定された操作に割り当てられているキー名を取得する。
    /// 複数のキーがある場合は「 / 」で連結する。
    /// </summary>
    public string GetBindingDisplayName(
        ScenarioInputAction action)
    {
        if (bindings == null)
            return "";

        List<string> names =
            new List<string>();

        foreach (ScenarioInputBinding binding in bindings)
        {
            if (binding == null ||
                binding.Action != action ||
                binding.Keys == null)
            {
                continue;
            }

            foreach (KeyCode key in binding.Keys)
            {
                string displayName =
                    FormatKeyDisplayName(key);

                if (!string.IsNullOrEmpty(displayName) &&
                    !names.Contains(displayName))
                {
                    names.Add(displayName);
                }
            }
        }

        return string.Join(" / ", names);
    }

    private static string FormatKeyDisplayName(
        KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Return:
                return "Enter";

            case KeyCode.KeypadEnter:
                return "テンキー Enter";

            case KeyCode.Escape:
                return "Esc";

            case KeyCode.Space:
                return "Space";

            case KeyCode.LeftArrow:
                return "←";

            case KeyCode.RightArrow:
                return "→";

            case KeyCode.UpArrow:
                return "↑";

            case KeyCode.DownArrow:
                return "↓";
        }

        string keyName = key.ToString();

        if (keyName.StartsWith("Alpha"))
        {
            return keyName.Substring(
                "Alpha".Length);
        }

        if (keyName.StartsWith("JoystickButton"))
        {
            return "ゲームパッド ボタン" +
                keyName.Substring(
                    "JoystickButton".Length);
        }

        return keyName;
    }
}