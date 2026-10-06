using System.Collections.Generic;

[System.Serializable]
public class ScenarioEvent
{
    public string EventID;

    public List<ScenarioBlock> Blocks =
        new List<ScenarioBlock>();
}