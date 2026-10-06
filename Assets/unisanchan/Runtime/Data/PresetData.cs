using System;
using System.Collections.Generic;

[Serializable]
public class PresetData
{
    public List<CameraPresetRow> cameraPresets;
    public List<ParticlePresetRow> particlePresets;
    public List<ModalPresetRow> modalPresets;
}