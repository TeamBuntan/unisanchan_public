using System.Collections.Generic;

[System.Serializable]
public class ScenarioTriggerGroup
{
    public string Trigger;

    public List<ScenarioRow> Rows =
        new List<ScenarioRow>();
}