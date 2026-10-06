using System.Collections.Generic;

[System.Serializable]
public class ScenarioBlock
{
    public string EventID;
    public string BlockID;

    public List<ScenarioTriggerGroup> TriggerGroups =
        new List<ScenarioTriggerGroup>();
}