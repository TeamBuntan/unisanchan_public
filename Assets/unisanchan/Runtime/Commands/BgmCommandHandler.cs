using System.Collections;
using UnityEngine;

namespace unisanchan
{
    public class BgmCommandHandler
    {
        private readonly AudioSource audioSource;
        private readonly AudioRegistry audioRegistry;
        private readonly System.Action<IEnumerator> startCoroutine;
        private int bgmPlaySerial = 0;

        public BgmCommandHandler(
            AudioSource audioSource,
            AudioRegistry audioRegistry,
            System.Action<IEnumerator> startCoroutine = null)
        {
            this.audioSource = audioSource;
            this.audioRegistry = audioRegistry;
            this.startCoroutine = startCoroutine;
        }

        public void Play(
            string bgmName,
            float volume,
            bool loop)
        {
            Play(
                bgmName,
                volume,
                loop,
                0f);
        }

        public void Play(
            string bgmName,
            float volume,
            bool loop,
            float fadeInSeconds)
        {
            if (audioSource == null)
            {
                Debug.LogWarning(
                    "[unisanchan] BGM用AudioSourceが設定されていません。");

                return;
            }

            if (audioRegistry == null)
            {
                Debug.LogWarning(
                    "[unisanchan] AudioRegistryが設定されていません。");

                return;
            }

            AudioClip clip =
                audioRegistry.FindBgm(bgmName);

            if (clip == null)
            {
                Debug.LogWarning(
                    $"[unisanchan] BGMが見つかりません: {bgmName}");

                return;
            }

            bgmPlaySerial++;

            float targetVolume =
                Mathf.Clamp01(volume);

            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.loop = loop;

            if (fadeInSeconds <= 0f ||
                startCoroutine == null)
            {
                audioSource.volume = targetVolume;
                audioSource.Play();
                return;
            }

            audioSource.volume = 0f;
            audioSource.Play();

            startCoroutine(
                FadeIn(
                    targetVolume,
                    fadeInSeconds,
                    bgmPlaySerial)
            );
        }

        private IEnumerator FadeIn(
            float targetVolume,
            float fadeInSeconds,
            int playSerial)
        {
            float elapsed = 0f;

            while (elapsed < fadeInSeconds &&
                audioSource.isPlaying)
            {
                if (bgmPlaySerial != playSerial)
                    yield break;

                elapsed += Time.deltaTime;

                float progress =
                    Mathf.Clamp01(
                        elapsed / fadeInSeconds);

                audioSource.volume =
                    Mathf.Lerp(
                        0f,
                        targetVolume,
                        progress);

                yield return null;
            }

            if (bgmPlaySerial == playSerial)
            {
                audioSource.volume = targetVolume;
            }
        }
        
        public void Stop(float fadeOutSeconds)
        {
            if (audioSource == null)
            {
                Debug.LogWarning(
                    "[unisanchan] BGM用AudioSourceが設定されていません。");

                return;
            }

            bgmPlaySerial++;

            if (fadeOutSeconds <= 0f ||
                !audioSource.isPlaying ||
                startCoroutine == null)
            {
                StopImmediately();
                return;
            }

            startCoroutine(
                FadeOutAndStop(
                    fadeOutSeconds,
                    bgmPlaySerial)
            );
        }

        private void StopImmediately()
        {
            audioSource.Stop();
            audioSource.clip = null;
            audioSource.volume = 1f;
        }

        private IEnumerator FadeOutAndStop(
            float fadeOutSeconds,
            int stopSerial)
        {
            float startVolume =
                audioSource.volume;

            float elapsed = 0f;

            while (elapsed < fadeOutSeconds &&
                audioSource.isPlaying)
            {
                if (bgmPlaySerial != stopSerial)
                    yield break;

                elapsed += Time.deltaTime;

                float progress =
                    Mathf.Clamp01(
                        elapsed / fadeOutSeconds);

                audioSource.volume =
                    Mathf.Lerp(
                        startVolume,
                        0f,
                        progress);

                yield return null;
            }

            if (bgmPlaySerial != stopSerial)
                yield break;

            StopImmediately();
        }

        public bool TryExecute(ScenarioRow row)
        {
            if (row == null)
                return false;

            string command =
                string.IsNullOrWhiteSpace(row.Command)
                    ? ""
                    : row.Command.Trim();
            
            if (string.Equals(
                    command,
                    "StopBGM",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                float fadeOutSeconds = 0f;

                if (!string.IsNullOrWhiteSpace(row.Arg1) &&
                    !float.TryParse(
                        row.Arg1,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out fadeOutSeconds))
                {
                    Debug.LogWarning(
                        $"[unisanchan] StopBGMのFadeOut秒数が不正です: {row.Arg1}");

                    return true;
                }

                if (fadeOutSeconds < 0f)
                {
                    Debug.LogWarning(
                        $"[unisanchan] StopBGMのFadeOut秒数は0以上で指定してください: {fadeOutSeconds}");

                    return true;
                }

                Stop(fadeOutSeconds);
                return true;
            }

            if (!string.Equals(
                    command,
                    "BGM",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string bgmName =
                string.IsNullOrWhiteSpace(row.Arg1)
                    ? ""
                    : row.Arg1.Trim();

            if (string.IsNullOrEmpty(bgmName))
            {
                Debug.LogWarning(
                    "[unisanchan] BGMのArg1に音源名を入力してください。");

                return true;
            }

            float volume = 1f;

            if (!string.IsNullOrWhiteSpace(row.Arg2) &&
                !float.TryParse(
                    row.Arg2,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out volume))
            {
                Debug.LogWarning(
                    $"[unisanchan] BGMのVolumeが不正です: {row.Arg2}");

                return true;
            }

            if (volume < 0f || volume > 1f)
            {
                Debug.LogWarning(
                    $"[unisanchan] BGMのVolumeは0～1で指定してください: {volume}");

                return true;
            }

            bool loop = true;

            string loopValue =
                string.IsNullOrWhiteSpace(row.Arg3)
                    ? ""
                    : row.Arg3.Trim();

            if (!string.IsNullOrEmpty(loopValue))
            {
                if (string.Equals(
                        loopValue,
                        "true",
                        System.StringComparison.OrdinalIgnoreCase) ||
                    loopValue == "1")
                {
                    loop = true;
                }
                else if (string.Equals(
                            loopValue,
                            "false",
                            System.StringComparison.OrdinalIgnoreCase) ||
                        loopValue == "0")
                {
                    loop = false;
                }
                else
                {
                    Debug.LogWarning(
                        $"[unisanchan] BGMのLoopはTRUEまたはFALSEで指定してください: {row.Arg3}");

                    return true;
                }
            }

            float fadeInSeconds = 0f;

            if (!string.IsNullOrWhiteSpace(row.Arg4) &&
                !float.TryParse(
                    row.Arg4,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out fadeInSeconds))
            {
                Debug.LogWarning(
                    $"[unisanchan] BGMのFadeIn秒数が不正です: {row.Arg4}");

                return true;
            }

            if (fadeInSeconds < 0f)
            {
                Debug.LogWarning(
                    $"[unisanchan] BGMのFadeIn秒数は0以上で指定してください: {fadeInSeconds}");

                return true;
            }

            Play(
                bgmName,
                volume,
                loop,
                fadeInSeconds);

            return true;
        }
    }
}