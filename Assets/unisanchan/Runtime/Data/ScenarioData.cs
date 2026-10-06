using System.Collections.Generic;

[System.Serializable]
public class ScenarioData
{
    public List<ScenarioRow> rows;
    public List<CameraPresetRow> cameraPresets;
    public List<ParticlePresetRow> particlePresets;
    public List<ImageRow> images;
    public List<ModalPresetRow> modalPresets;
}