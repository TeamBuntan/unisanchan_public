using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace unisanchan
{
    public class ParticleCommandHandler
    {
        private readonly ParticleRegistry particleRegistry;
        private readonly List<ParticlePresetRow> particlePresets;
        private readonly ActorList actorList;
        private readonly ScenarioCameraController cameraController;
        private readonly Action<IEnumerator> startCoroutine;

        private readonly List<ActiveParticleInstance>
            activeParticles =
                new List<ActiveParticleInstance>();

        private int nextAutoInstanceId = 1;

        private class ParticleEmissionState
        {
            public ParticleSystem System;
            public float RateOverTimeMultiplier;
            public float RateOverDistanceMultiplier;
        }

        private class ParticleExecutionOptions
        {
            public bool? FollowOverride;
            public string InstanceId;

            public ParticleTransitionMode FadeInMode =
                ParticleTransitionMode.None;

            public float FadeInSeconds;
            public float Lifetime;
        }

        private class ActiveParticleInstance
        {
            public GameObject Instance;
            public string ParticleName;
            public string SpawnPresetName;
            public string InstanceId;
            public bool IsStopping;
        }

        public ParticleCommandHandler(
            ParticleRegistry particleRegistry,
            List<ParticlePresetRow> particlePresets,
            ActorList actorList,
            ScenarioCameraController cameraController,
            Action<IEnumerator> startCoroutine = null)
        {
            this.particleRegistry = particleRegistry;

            this.particlePresets =
                particlePresets ??
                new List<ParticlePresetRow>();

            this.actorList = actorList;
            this.cameraController = cameraController;
            this.startCoroutine = startCoroutine;
        }

        public bool TryExecute(ScenarioRow row)
        {
            if (row == null)
                return false;

            string command =
                row.Command?.Trim() ?? "";

            if (string.Equals(
                    command,
                    "Particle",
                    StringComparison.OrdinalIgnoreCase))
            {
                Play(
                    row.Arg1,
                    row.Arg2,
                    row.Arg3,
                    row.Arg4,
                    row.Arg5,
                    row.Arg6,
                    row.Arg7,
                    row.Arg8,
                    row.Arg9,
                    row.Arg10);

                return true;
            }

            if (string.Equals(
                    command,
                    "StopParticle",
                    StringComparison.OrdinalIgnoreCase))
            {
                Stop(
                    row.Arg1,
                    row.Arg2,
                    row.Arg3,
                    row.Arg4,
                    row.Arg5);

                return true;
            }

            return false;
        }

        public void Stop(
            string particleName,
            string spawnPresetName,
            string instanceId,
            string fadeOutSeconds,
            string fadeOutMode)
        {
            string normalizedParticleName =
                particleName?.Trim() ?? "";

            string normalizedSpawnPresetName =
                spawnPresetName?.Trim() ?? "";

            string normalizedInstanceId =
                instanceId?.Trim() ?? "";
            
            ParticleTransitionMode parsedFadeOutMode =
                ParticleTransitionMode.None;

            if (!string.IsNullOrWhiteSpace(
                    fadeOutMode) &&
                !Enum.TryParse(
                    fadeOutMode.Trim(),
                    true,
                    out parsedFadeOutMode))
            {
                Debug.LogWarning(
                    $"[unisanchan] FadeOutModeを読み取れません: {fadeOutMode}");

                return;
            }

            float parsedFadeOutSeconds =
                Mathf.Max(
                    0f,
                    ParseFloat(
                        fadeOutSeconds));
            
            if (string.IsNullOrWhiteSpace(
                    fadeOutMode) &&
                parsedFadeOutSeconds > 0f)
            {
                parsedFadeOutMode =
                    ParticleTransitionMode.Emission;
            }

            for (int i =
                    activeParticles.Count - 1;
                i >= 0;
                i--)
            {
                ActiveParticleInstance active =
                    activeParticles[i];

                if (active == null ||
                    active.Instance == null)
                {
                    activeParticles.RemoveAt(i);
                    continue;
                }

                if (active.IsStopping)
                {
                    continue;
                }

                bool matchesParticle =
                    string.IsNullOrEmpty(
                        normalizedParticleName) ||
                    string.Equals(
                        active.ParticleName,
                        normalizedParticleName,
                        StringComparison.OrdinalIgnoreCase);

                bool matchesSpawnPreset =
                    string.IsNullOrEmpty(
                        normalizedSpawnPresetName) ||
                    string.Equals(
                        active.SpawnPresetName,
                        normalizedSpawnPresetName,
                        StringComparison.OrdinalIgnoreCase);

                bool matchesInstance =
                    string.IsNullOrEmpty(
                        normalizedInstanceId) ||
                    string.Equals(
                        active.InstanceId,
                        normalizedInstanceId,
                        StringComparison.OrdinalIgnoreCase);

                if (!matchesParticle ||
                    !matchesSpawnPreset ||
                    !matchesInstance)
                {
                    continue;
                }

                active.IsStopping = true;

                bool useFadeOut =
                    parsedFadeOutMode !=
                        ParticleTransitionMode.None &&
                    parsedFadeOutSeconds > 0f &&
                    startCoroutine != null;

                if (useFadeOut)
                {
                    startCoroutine(
                        StopRoutine(
                            active,
                            parsedFadeOutMode,
                            parsedFadeOutSeconds));
                }
                else
                {
                    UnityEngine.Object.Destroy(
                        active.Instance);

                    activeParticles.RemoveAt(i);
                }
            }
        }

        private IEnumerator StopRoutine(
            ActiveParticleInstance active,
            ParticleTransitionMode fadeOutMode,
            float fadeOutSeconds)
        {
            if (active == null ||
                active.Instance == null)
            {
                yield break;
            }

            GameObject instance =
                active.Instance;

            if (fadeOutMode ==
                    ParticleTransitionMode.Scale &&
                fadeOutSeconds > 0f)
            {
                yield return ScaleRoutine(
                    instance,
                    instance.transform.localScale,
                    Vector3.zero,
                    fadeOutSeconds);
            }
            else if (fadeOutMode ==
                        ParticleTransitionMode.Emission &&
                    fadeOutSeconds > 0f)
            {
                List<ParticleEmissionState> emissionStates =
                    CaptureEmissionStates(
                        instance);

                yield return EmissionRoutine(
                    emissionStates,
                    1f,
                    0f,
                    fadeOutSeconds);

                StopEmission(
                    emissionStates);

                while (HasAliveParticles(
                    emissionStates))
                {
                    yield return null;
                }
            }

            if (instance != null)
            {
                UnityEngine.Object.Destroy(
                    instance);
            }

            activeParticles.Remove(
                active);
        }

        public void Play(
            string particleName,
            string spawnPresetName,
            string offsetX,
            string offsetY,
            string offsetZ,
            string followOverride,
            string instanceId,
            string fadeInSeconds,
            string fadeInMode,
            string lifetime)
        {
            ParticlePresetRow spawnPreset = null;

            if (!string.IsNullOrWhiteSpace(
                    spawnPresetName))
            {
                spawnPreset =
                    FindSpawnPreset(
                        spawnPresetName);

                if (spawnPreset == null)
                {
                    Debug.LogWarning(
                        $"[unisanchan] Particle Spawn Presetが見つかりません: {spawnPresetName}");

                    return;
                }
            }

            Vector3 commandOffset =
                ParseVector3(
                    offsetX,
                    offsetY,
                    offsetZ,
                    Vector3.zero);

            ParticleTransitionMode parsedFadeInMode =
                ParticleTransitionMode.None;

            if (!string.IsNullOrWhiteSpace(
                    fadeInMode) &&
                !Enum.TryParse(
                    fadeInMode.Trim(),
                    true,
                    out parsedFadeInMode))
            {
                Debug.LogWarning(
                    $"[unisanchan] FadeInModeを読み取れません: {fadeInMode}");

                return;
            }

            ParticleExecutionOptions options =
                new ParticleExecutionOptions
                {
                    FollowOverride =
                        string.IsNullOrWhiteSpace(
                            followOverride)
                            ? null
                            : ParseBool(
                                followOverride),

                    InstanceId =
                        instanceId?.Trim() ?? "",

                    FadeInMode =
                        parsedFadeInMode,

                    FadeInSeconds =
                        Mathf.Max(
                            0f,
                            ParseFloat(
                                fadeInSeconds)),

                    Lifetime =
                        Mathf.Max(
                            0f,
                            ParseFloat(
                                lifetime))
                };
            
            if (string.IsNullOrWhiteSpace(
                    fadeInMode) &&
                options.FadeInSeconds > 0f)
            {
                options.FadeInMode =
                    ParticleTransitionMode.Emission;
            }

            PlayInternal(
                particleName,
                spawnPreset,
                commandOffset,
                options);
        }

        private void PlayInternal(
            string presetName,
            ParticlePresetRow spawnPreset,
            Vector3 commandOffset,
            ParticleExecutionOptions options)
        {
            if (particleRegistry == null)
            {
                Debug.LogWarning(
                    "[unisanchan] ParticleRegistryが設定されていません。");

                return;
            }

            ParticleRegistryEntry preset =
                particleRegistry.FindPreset(presetName);

            if (preset == null)
            {
                Debug.LogWarning(
                    $"[unisanchan] Particle Presetが見つかりません: {presetName}");

                return;
            }

            if (preset.Prefab == null)
            {
                Debug.LogWarning(
                    $"[unisanchan] Particle Preset「{presetName}」にPrefabが設定されていません。");

                return;
            }

             ParticleOriginMode originMode =
                ParticleOriginMode.World;

            Vector3 basePosition =
                Vector3.zero;

            Vector3 rotationOffset =
                Vector3.zero;

            Vector3 scaleMultiplier =
                Vector3.one;

            if (spawnPreset != null)
            {
                if (!Enum.TryParse(
                        spawnPreset.OriginMode?.Trim(),
                        true,
                        out originMode))
                {
                    Debug.LogWarning(
                        $"[unisanchan] OriginModeを読み取れません: {spawnPreset.OriginMode}");

                    return;
                }

                basePosition =
                    ParseVector3(
                        spawnPreset.PosX,
                        spawnPreset.PosY,
                        spawnPreset.PosZ,
                        Vector3.zero);

                rotationOffset =
                    ParseVector3(
                        spawnPreset.RotX,
                        spawnPreset.RotY,
                        spawnPreset.RotZ,
                        Vector3.zero);

                scaleMultiplier =
                    ParseVector3(
                        spawnPreset.ScaleX,
                        spawnPreset.ScaleY,
                        spawnPreset.ScaleZ,
                        Vector3.one);
            }

            Vector3 additionalOffset =
                commandOffset;

            GameObject instance;

            if (originMode ==
                ParticleOriginMode.World)
            {
                Vector3 spawnPosition =
                    basePosition +
                    additionalOffset;

                Quaternion spawnRotation =
                    preset.Prefab.transform.rotation *
                    Quaternion.Euler(
                        rotationOffset);

                instance =
                    UnityEngine.Object.Instantiate(
                        preset.Prefab,
                        spawnPosition,
                        spawnRotation);
            }
            else if (originMode ==
                ParticleOriginMode.Target)
            {
                if (actorList == null)
                {
                    Debug.LogWarning(
                        "[unisanchan] Particle用のActorListが設定されていません。");

                    return;
                }

                if (spawnPreset == null ||
                    string.IsNullOrWhiteSpace(
                        spawnPreset.TargetId))
                {
                    Debug.LogWarning(
                        "[unisanchan] TargetモードですがTargetIdが空欄です。");

                    return;
                }

                GameObject targetObject =
                    actorList.FindActor(
                        spawnPreset.TargetId);

                if (targetObject == null)
                {
                    Debug.LogWarning(
                        $"[unisanchan] Particle Targetが見つかりません: {spawnPreset.TargetId}");

                    return;
                }

                Transform anchor =
                    FindAnchorTransform(
                        targetObject.transform,
                        spawnPreset.AnchorId);

                if (anchor == null)
                {
                    Debug.LogWarning(
                        $"[unisanchan] Particle Anchorが見つかりません: Target={spawnPreset.TargetId}, Anchor={spawnPreset.AnchorId}");

                    return;
                }

                Vector3 localPosition =
                    basePosition +
                    additionalOffset;

                Quaternion localRotation =
                    preset.Prefab.transform.localRotation *
                    Quaternion.Euler(
                        rotationOffset);

                bool follow =
                    options?.FollowOverride ??
                    ParseBool(
                        spawnPreset.Follow);

                if (follow)
                {
                    instance =
                        UnityEngine.Object.Instantiate(
                            preset.Prefab,
                            anchor);

                    instance.transform.localPosition =
                        localPosition;

                    instance.transform.localRotation =
                        localRotation;
                }
                else
                {
                    Vector3 worldPosition =
                        anchor.TransformPoint(
                            localPosition);

                    Quaternion worldRotation =
                        anchor.rotation *
                        localRotation;

                    instance =
                        UnityEngine.Object.Instantiate(
                            preset.Prefab,
                            worldPosition,
                            worldRotation);
                }
            }
            else if (originMode ==
                ParticleOriginMode.Camera)
            {
                if (cameraController == null ||
                    cameraController.CameraTransform == null)
                {
                    Debug.LogWarning(
                        "[unisanchan] Particle用のCameraが設定されていません。");

                    return;
                }

                Transform cameraTransform =
                    cameraController.CameraTransform;

                Vector3 localPosition =
                    basePosition +
                    additionalOffset;

                Quaternion localRotation =
                    preset.Prefab.transform.localRotation *
                    Quaternion.Euler(
                        rotationOffset);

                bool presetFollow =
                    spawnPreset != null &&
                    ParseBool(
                        spawnPreset.Follow);

                bool follow =
                    options?.FollowOverride ??
                    presetFollow;

                if (follow)
                {
                    instance =
                        UnityEngine.Object.Instantiate(
                            preset.Prefab,
                            cameraTransform);

                    instance.transform.localPosition =
                        localPosition;

                    instance.transform.localRotation =
                        localRotation;
                }
                else
                {
                    Vector3 worldPosition =
                        cameraTransform.TransformPoint(
                            localPosition);

                    Quaternion worldRotation =
                        cameraTransform.rotation *
                        localRotation;

                    instance =
                        UnityEngine.Object.Instantiate(
                            preset.Prefab,
                            worldPosition,
                            worldRotation);
                }
            }
            else
            {
                Debug.LogWarning(
                    $"[unisanchan] Particle Origin Mode「{originMode}」を処理できません。");

                return;
            }

            string resolvedInstanceId =
                !string.IsNullOrWhiteSpace(
                    options?.InstanceId)
                    ? options.InstanceId.Trim()
                    : $"Particle_{nextAutoInstanceId++}";

            ActiveParticleInstance activeInstance =
                new ActiveParticleInstance
                {
                    Instance = instance,
                    ParticleName =
                        presetName?.Trim() ?? "",

                    SpawnPresetName =
                        spawnPreset?.PresetName?.Trim() ?? "",

                    InstanceId =
                        resolvedInstanceId,

                    IsStopping = false
                };

            activeParticles.Add(
                activeInstance);

            Vector3 targetScale =
                Vector3.Scale(
                    instance.transform.localScale,
                    scaleMultiplier);

            if (startCoroutine != null)
            {
                startCoroutine(
                    LifecycleRoutine(
                        instance,
                        targetScale,
                        options));
            }
            else
            {
                instance.transform.localScale =
                    targetScale;

                if (options != null &&
                    options.Lifetime > 0f)
                {
                    UnityEngine.Object.Destroy(
                        instance,
                        options.Lifetime);
                }
            }
        }
        
        private static List<ParticleEmissionState>
            CaptureEmissionStates(
                GameObject instance)
        {
            List<ParticleEmissionState> states =
                new List<ParticleEmissionState>();

            if (instance == null)
                return states;

            ParticleSystem[] particleSystems =
                instance.GetComponentsInChildren
                    <ParticleSystem>(true);

            foreach (ParticleSystem particleSystem
                in particleSystems)
            {
                if (particleSystem == null)
                    continue;

                ParticleSystem.EmissionModule emission =
                    particleSystem.emission;

                states.Add(
                    new ParticleEmissionState
                    {
                        System = particleSystem,
                        RateOverTimeMultiplier =
                            emission.rateOverTimeMultiplier,
                        RateOverDistanceMultiplier =
                            emission.rateOverDistanceMultiplier
                    });
            }

            return states;
        }

        private static void SetEmissionFactor(
            List<ParticleEmissionState> states,
            float factor)
        {
            if (states == null)
                return;

            float clampedFactor =
                Mathf.Clamp01(factor);

            foreach (ParticleEmissionState state
                in states)
            {
                if (state == null ||
                    state.System == null)
                {
                    continue;
                }

                ParticleSystem.EmissionModule emission =
                    state.System.emission;

                emission.rateOverTimeMultiplier =
                    state.RateOverTimeMultiplier *
                    clampedFactor;

                emission.rateOverDistanceMultiplier =
                    state.RateOverDistanceMultiplier *
                    clampedFactor;
            }
        }

        private IEnumerator LifecycleRoutine(
            GameObject instance,
            Vector3 targetScale,
            ParticleExecutionOptions options)
        {
            if (instance == null)
                yield break;

            if (options == null)
            {
                options =
                    new ParticleExecutionOptions();
            }

            List<ParticleEmissionState> emissionStates =
                CaptureEmissionStates(instance);

            ParticleTransitionMode fadeInMode =
                options.FadeInMode;

            float fadeInSeconds =
                fadeInMode !=
                    ParticleTransitionMode.None
                    ? Mathf.Max(
                        0f,
                        options.FadeInSeconds)
                    : 0f;

            float lifetime =
                Mathf.Max(
                    0f,
                    options.Lifetime);

            if (fadeInMode ==
                    ParticleTransitionMode.Scale &&
                fadeInSeconds > 0f)
            {
                instance.transform.localScale =
                    Vector3.zero;
            }
            else
            {
                instance.transform.localScale =
                    targetScale;
            }

            if (fadeInMode ==
                    ParticleTransitionMode.Emission &&
                fadeInSeconds > 0f)
            {
                SetEmissionFactor(
                    emissionStates,
                    0f);
            }

            if (fadeInSeconds > 0f)
            {
                if (fadeInMode ==
                    ParticleTransitionMode.Scale)
                {
                    yield return ScaleRoutine(
                        instance,
                        Vector3.zero,
                        targetScale,
                        fadeInSeconds);
                }
                else if (fadeInMode ==
                    ParticleTransitionMode.Emission)
                {
                    yield return EmissionRoutine(
                        emissionStates,
                        0f,
                        1f,
                        fadeInSeconds);
                }
            }

            if (instance == null)
                yield break;

            // Lifetimeが0ならStopParticleまで残す。
            if (lifetime <= 0f)
                yield break;

            float holdSeconds =
                Mathf.Max(
                    0f,
                    lifetime -
                    fadeInSeconds);

            if (holdSeconds > 0f)
            {
                yield return new WaitForSeconds(
                    holdSeconds);
            }

            if (instance == null)
                yield break;

            // 新しい粒の放出を止め、
            // 既に出ている粒が消えるのを待つ。
            StopEmission(
                emissionStates);

            while (HasAliveParticles(
                emissionStates))
            {
                yield return null;
            }

            if (instance != null)
            {
                RemoveActiveInstance(
                    instance);

                UnityEngine.Object.Destroy(
                    instance);
            }
        }

        private IEnumerator EmissionRoutine(
            List<ParticleEmissionState> states,
            float startFactor,
            float endFactor,
            float duration)
        {
            if (duration <= 0f)
            {
                SetEmissionFactor(
                    states,
                    endFactor);

                yield break;
            }

            float elapsed = 0f;

            SetEmissionFactor(
                states,
                startFactor);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float progress =
                    Mathf.Clamp01(
                        elapsed / duration);

                float factor =
                    Mathf.Lerp(
                        startFactor,
                        endFactor,
                        progress);

                SetEmissionFactor(
                    states,
                    factor);

                yield return null;
            }

            SetEmissionFactor(
                states,
                endFactor);
        }

        private static void StopEmission(
            List<ParticleEmissionState> states)
        {
            if (states == null)
                return;

            foreach (ParticleEmissionState state
                in states)
            {
                if (state == null ||
                    state.System == null)
                {
                    continue;
                }

                state.System.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private static bool HasAliveParticles(
            List<ParticleEmissionState> states)
        {
            if (states == null)
                return false;

            foreach (ParticleEmissionState state
                in states)
            {
                if (state != null &&
                    state.System != null &&
                    state.System.IsAlive(true))
                {
                    return true;
                }
            }

            return false;
        }

        private void RemoveActiveInstance(
            GameObject instance)
        {
            for (int i =
                    activeParticles.Count - 1;
                i >= 0;
                i--)
            {
                ActiveParticleInstance active =
                    activeParticles[i];

                if (active == null ||
                    active.Instance == null ||
                    active.Instance == instance)
                {
                    activeParticles.RemoveAt(i);
                }
            }
        }

        private ParticlePresetRow FindSpawnPreset(
            string presetName)
        {
            if (particlePresets == null ||
                string.IsNullOrWhiteSpace(presetName))
            {
                return null;
            }

            string normalizedName =
                presetName.Trim();

            foreach (ParticlePresetRow preset
                in particlePresets)
            {
                if (preset == null)
                    continue;

                if (string.Equals(
                        preset.PresetName?.Trim(),
                        normalizedName,
                        StringComparison.Ordinal))
                {
                    return preset;
                }
            }

            return null;
        }

        private static float ParseFloat(
            string value,
            float defaultValue = 0f)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if (float.TryParse(
                    value.Trim(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out float result))
            {
                return result;
            }

            Debug.LogWarning(
                $"[unisanchan] 数値を読み取れません: {value}");

            return defaultValue;
        }

        private static Vector3 ParseVector3(
            string x,
            string y,
            string z,
            Vector3 defaultValue)
        {
            return new Vector3(
                ParseFloat(x, defaultValue.x),
                ParseFloat(y, defaultValue.y),
                ParseFloat(z, defaultValue.z));
        }

        private static bool ParseBool(
            string value,
            bool defaultValue = false)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            string normalized =
                value.Trim();

            if (bool.TryParse(
                    normalized,
                    out bool result))
            {
                return result;
            }

            if (normalized == "1")
                return true;

            if (normalized == "0")
                return false;

            Debug.LogWarning(
                $"[unisanchan] TRUE/FALSEを読み取れません: {value}");

            return defaultValue;
        }

        private static Transform FindAnchorTransform(
            Transform target,
            string anchorId)
        {
            if (target == null)
                return null;

            if (string.IsNullOrWhiteSpace(anchorId))
                return target;

            string normalizedAnchorId =
                anchorId.Trim();

            Transform[] transforms =
                target.GetComponentsInChildren<Transform>(
                    true);

            foreach (Transform child in transforms)
            {
                if (child == null)
                    continue;

                if (string.Equals(
                        child.name,
                        normalizedAnchorId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }

            return null;
        }

        private IEnumerator ScaleRoutine(
            GameObject instance,
            Vector3 startScale,
            Vector3 endScale,
            float duration)
        {
            if (instance == null)
                yield break;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (instance == null)
                    yield break;

                elapsed += Time.deltaTime;

                float progress =
                    Mathf.Clamp01(
                        elapsed / duration);

                instance.transform.localScale =
                    Vector3.Lerp(
                        startScale,
                        endScale,
                        progress);

                yield return null;
            }

            if (instance != null)
            {
                instance.transform.localScale =
                    endScale;
            }
        }

    }
}