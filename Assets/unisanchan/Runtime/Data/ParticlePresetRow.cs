[System.Serializable]
public class ParticlePresetRow
{
    public string PresetName;

    // World / Target / Camera
    public string OriginMode;

    // OriginModeがTargetの場合に使用
    public string TargetId;
    public string AnchorId;

    public string PosX;
    public string PosY;
    public string PosZ;

    public string RotX;
    public string RotY;
    public string RotZ;

    public string ScaleX;
    public string ScaleY;
    public string ScaleZ;

    public string Follow;
}