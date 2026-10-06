using UnityEngine;
using System.Collections;

namespace unisanchan
{
    public class VoiceCommandHandler
    {
        private readonly AudioSource audioSource;
        private readonly VoiceRegistry voiceRegistry;
        private readonly System.Action<IEnumerator> startCoroutine;
        private int voicePlaySerial = 0;
        private bool isFadingOut = false;

        public bool IsFadingOut => isFadingOut;

        public VoiceCommandHandler(
            AudioSource audioSource,
            VoiceRegistry voiceRegistry,
            System.Action<IEnumerator> startCoroutine)
        {
            this.audioSource = audioSource;
            this.voiceRegistry = voiceRegistry;
            this.startCoroutine = startCoroutine;
        }

        public bool IsPlaying
        {
            get
            {
                return audioSource != null &&
                       audioSource.isPlaying;
            }
        }

        public void Play(AudioClip clip)
        {
            if (audioSource == null || clip == null)
                return;

            voicePlaySerial++;
            isFadingOut = false;

            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.volume = 1f;
            audioSource.Play();
        }

        public void Play(string voicePath)
        {
            if (string.IsNullOrWhiteSpace(voicePath))
                return;

            if (voiceRegistry == null)
                return;

            AudioClip clip = voiceRegistry.FindClip(voicePath);

            if (clip == null)
            {
                Debug.LogWarning(
                    $"[unisanchan] Voiceが見つかりません: {voicePath}");
                return;
            }

            Play(clip);
        }

        public void Stop()
        {
            if (audioSource == null)
                return;

            audioSource.Stop();
            audioSource.clip = null;
        }

        public void StopVoice(string fadeValue)
        {
            float fadeSeconds = 0f;

            if (!string.IsNullOrWhiteSpace(fadeValue) &&
                float.TryParse(
                    fadeValue,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out float fadeUnits))
            {
                fadeSeconds = Mathf.Max(0f, fadeUnits * 0.1f);
            }

            if (fadeSeconds <= 0f)
            {
                Stop();
                return;
            }

            if (startCoroutine != null)
            {
                isFadingOut = true;

                startCoroutine(
                    FadeOutAndStop(fadeSeconds)
                );
            }
        }

        public IEnumerator FadeOutAndStop(float fadeSeconds)
        {
            if (audioSource == null)
                yield break;

            if (fadeSeconds <= 0f)
            {
                Stop();
                yield break;
            }

            float startVolume = audioSource.volume;
            float elapsed = 0f;
            int fadeStartSerial = voicePlaySerial;

            while (elapsed < fadeSeconds &&
                audioSource.isPlaying)
            {
                if (voicePlaySerial != fadeStartSerial)
                    yield break;

                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(
                    elapsed / fadeSeconds);

                audioSource.volume =
                    Mathf.Lerp(
                        startVolume,
                        0f,
                        t);

                yield return null;
            }

            if (voicePlaySerial != fadeStartSerial)
            {
                isFadingOut = false;
                yield break;
            }

            Stop();
            isFadingOut = false;
        }

        public bool TryExecute(ScenarioRow row)
        {
            if (row == null)
                return false;

            string command =
                string.IsNullOrWhiteSpace(row.Command)
                    ? ""
                    : row.Command.Trim();

            if (!string.Equals(
                    command,
                    "StopVoice",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            StopVoice(row.Arg1);
            return true;
        }
    }
}