using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ScenarioEntry
{
    public string Name;
    public TextAsset Source;
}

[CreateAssetMenu(
    fileName = "ScenarioRegistry",
    menuName = "unisanchan/Scenario Registry")]
public class ScenarioRegistry : ScriptableObject
{
    [SerializeField]
    private List<ScenarioEntry> scenarios = new();

    public IReadOnlyList<ScenarioEntry> Scenarios
        => scenarios;

    public void SetScenariosFromJsonFiles(List<TextAsset> files)
    {
        scenarios = new List<ScenarioEntry>();

        if (files == null)
            return;

        foreach (TextAsset file in files)
        {
            if (file == null)
                continue;

            scenarios.Add(new ScenarioEntry
            {
                Name = file.name,
                Source = file
            });
        }
    }
}