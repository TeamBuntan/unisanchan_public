using UnityEngine;

[System.Serializable]
public class ScenarioInputBinding
{
    [SerializeField]
    private ScenarioInputAction action;

    [SerializeField]
    private KeyCode[] keys;

    public ScenarioInputAction Action => action;
    public KeyCode[] Keys => keys;
}