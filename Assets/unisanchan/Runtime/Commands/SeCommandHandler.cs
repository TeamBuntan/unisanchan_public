using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace unisanchan
{
    public class SeCommandHandler
    {
        private class SeChannel
        {
            public AudioSource Source;
            public long PlayOrder;
            public string SeName;
            public int PlaySerial;
        }

        private readonly GameObject audioSourceHost;
        private readonly AudioRegistry audioRegistry;
        private readonly System.Action<IEnumerator> startCoroutine;

        private readonly List<SeChannel> channels =
            new List<SeChannel>();

        private long nextPlayOrder = 0;

        public SeCommandHandler(
            GameObject audioSourceHost,
            AudioRegistry audioRegistry,
            System.Action<IEnumerator> startCoroutine = null)
        {
            this.audioSourceHost = audioSourceHost;
            this.audioRegistry = audioRegistry;
            this.startCoroutine = startCoroutine;

            CreateChannels();
        }

        private void CreateChannels()
        {
            if (audioSourceHost == null)
            {
                Debug.LogWarning(
                    "[unisanchan] SEAudioが設定されていません。");

                return;
            }

            int channelCount =
                audioRegistry != null
                    ? audioRegistry.MaxSimultaneousSe
                    : 1;

            for (int i = 0; i < channelCount; i++)
            {
                AudioSource source =
                    audioSourceHost.AddComponent<AudioSource>();

                source.playOnAwake = false;
                source.loop = false;
                source.volume = 1f;
                source.spatialBlend = 0f;

                channels.Add(
                    new SeChannel
                    {
                        Source = source,
                        PlayOrder = 0
                    });
            }
        }

        public void Play(
            string seName,
            float volume,
            bool loop)
        {
            if (audioRegistry == null)
            {
                Debug.LogWarning(
                    "[unisanchan] AudioRegistryが設定されていません。");

                return;
            }

            AudioClip clip =
                audioRegistry.FindSe(seName);

            if (clip == null)
            {
                Debug.LogWarning(
                    $"[unisanchan] SEが見つかりません: {seName}");

                return;
            }

            SeChannel channel =
                FindAvailableChannel();

            if (channel == null ||
                channel.Source == null)
            {
                Debug.LogWarning(
                    "[unisanchan] 利用可能なSE AudioSourceがありません。");

                return;
            }

            channel.PlaySerial++;

            channel.SeName =
                string.IsNullOrWhiteSpace(seName)
                    ? ""
                    : seName.Trim();

            channel.Source.Stop();
            channel.Source.clip = clip;
            channel.Source.volume =
                Mathf.Clamp01(volume);
            channel.Source.loop = loop;
            channel.Source.Play();

            nextPlayOrder++;
            channel.PlayOrder = nextPlayOrder;
        }

        public void Stop(
            float fadeOutSeconds,
            string seName)
        {
            string targetName =
                string.IsNullOrWhiteSpace(seName)
                    ? ""
                    : seName.Trim();

            foreach (SeChannel channel in channels)
            {
                if (channel.Source == null ||
                    !channel.Source.isPlaying)
                {
                    continue;
                }

                bool stopAll =
                    string.IsNullOrEmpty(targetName);

                bool nameMatches =
                    string.Equals(
                        channel.SeName,
                        targetName,
                        System.StringComparison.OrdinalIgnoreCase);

                if (!stopAll && !nameMatches)
                    continue;

                channel.PlaySerial++;

                if (fadeOutSeconds <= 0f ||
                    startCoroutine == null)
                {
                    StopImmediately(channel);
                    continue;
                }

                startCoroutine(
                    FadeOutAndStop(
                        channel,
                        fadeOutSeconds,
                        channel.PlaySerial)
                );
            }
        }

        private void StopImmediately(
            SeChannel channel)
        {
            if (channel == null ||
                channel.Source == null)
            {
                return;
            }

            channel.Source.Stop();
            channel.Source.clip = null;
            channel.Source.loop = false;
            channel.Source.volume = 1f;
            channel.SeName = "";
        }

        private IEnumerator FadeOutAndStop(
            SeChannel channel,
            float fadeOutSeconds,
            int stopSerial)
        {
            if (channel == null ||
                channel.Source == null)
            {
                yield break;
            }

            float startVolume =
                channel.Source.volume;

            float elapsed = 0f;

            while (elapsed < fadeOutSeconds &&
                channel.Source.isPlaying)
            {
                if (channel.PlaySerial != stopSerial)
                    yield break;

                elapsed += Time.deltaTime;

                float progress =
                    Mathf.Clamp01(
                        elapsed / fadeOutSeconds);

                channel.Source.volume =
                    Mathf.Lerp(
                        startVolume,
                        0f,
                        progress);

                yield return null;
            }

            if (channel.PlaySerial != stopSerial)
                yield break;

            StopImmediately(channel);
        }

        private SeChannel FindAvailableChannel()
        {
            foreach (SeChannel channel in channels)
            {
                if (channel.Source != null &&
                    !channel.Source.isPlaying)
                {
                    return channel;
                }
            }

            SeChannel oldestChannel = null;

            foreach (SeChannel channel in channels)
            {
                if (channel.Source == null)
                    continue;

                if (oldestChannel == null ||
                    channel.PlayOrder <
                    oldestChannel.PlayOrder)
                {
                    oldestChannel = channel;
                }
            }

            return oldestChannel;
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
                    "StopSE",
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
                        $"[unisanchan] StopSEのFadeOut秒数が不正です: {row.Arg1}");

                    return true;
                }

                if (fadeOutSeconds < 0f)
                {
                    Debug.LogWarning(
                        $"[unisanchan] StopSEのFadeOut秒数は0以上で指定してください: {fadeOutSeconds}");

                    return true;
                }

                string targetSeName =
                    string.IsNullOrWhiteSpace(row.Arg2)
                        ? ""
                        : row.Arg2.Trim();

                Stop(
                    fadeOutSeconds,
                    targetSeName);

                return true;
            }

            if (!string.Equals(
                    command,
                    "SE",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string seName =
                string.IsNullOrWhiteSpace(row.Arg1)
                    ? ""
                    : row.Arg1.Trim();

            if (string.IsNullOrEmpty(seName))
            {
                Debug.LogWarning(
                    "[unisanchan] SEのArg1に音源名を入力してください。");

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
                    $"[unisanchan] SEのVolumeが不正です: {row.Arg2}");

                return true;
            }

            if (volume < 0f || volume > 1f)
            {
                Debug.LogWarning(
                    $"[unisanchan] SEのVolumeは0～1で指定してください: {volume}");

                return true;
            }

            bool loop = false;

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
                        $"[unisanchan] SEのLoopはTRUEまたはFALSEで指定してください: {row.Arg3}");

                    return true;
                }
            }

            Play(
                seName,
                volume,
                loop);

            return true;
        }
    }
}