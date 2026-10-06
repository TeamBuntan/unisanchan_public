using System;
using UnityEngine;

namespace unisanchan
{
    public static class EasingUtility
    {
        public static bool TryParse(
            string value,
            out EasingType easingType)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                easingType = EasingType.Linear;
                return true;
            }

            return Enum.TryParse(
                value.Trim(),
                true,
                out easingType);
        }

        public static float Evaluate(
            EasingType easingType,
            float progress)
        {
            float t = Mathf.Clamp01(progress);

            switch (easingType)
            {
                case EasingType.EaseInSine:
                    return 1f -
                        Mathf.Cos(t * Mathf.PI * 0.5f);

                case EasingType.EaseOutSine:
                    return Mathf.Sin(
                        t * Mathf.PI * 0.5f);

                case EasingType.EaseInOutSine:
                    return -(
                        Mathf.Cos(Mathf.PI * t) - 1f
                    ) * 0.5f;

                case EasingType.EaseInQuad:
                    return t * t;

                case EasingType.EaseOutQuad:
                    return 1f -
                        (1f - t) * (1f - t);

                case EasingType.EaseInOutQuad:
                    if (t < 0.5f)
                        return 2f * t * t;

                    return 1f -
                        Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;

                case EasingType.Linear:
                default:
                    return t;
            }
        }
    }
}