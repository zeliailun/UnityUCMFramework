using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnknownCreator.Modules
{
    public static class UITKGlobals
    {
        public const string OnRefreshUIComp = nameof(OnRefreshUIComp);


        private sealed class ShakeState
        {
            public StyleTranslate originalTranslate;
            public Translate resolvedTranslate;
            public IVisualElementScheduledItem task;
            public EventCallback<DetachFromPanelEvent> detachCallback;
        }

        private static readonly ConditionalWeakTable<VisualElement, ShakeState> ShakeStates = new();

        public static void Shake(
            this VisualElement element,
            float duration = 0.5f,
            float intensity = 10f)
        {
            if (element == null)
                return;

            bool isRestart = ShakeStates.TryGetValue(
                element,
                out ShakeState previous);

            StyleTranslate originalTranslate = isRestart
                ? previous.originalTranslate
                : default;

            Translate resolvedTranslate = isRestart
                ? previous.resolvedTranslate
                : default;

            if (isRestart)
                StopShake(element, previous);

            if (element.resourcesReleased ||
                element.panel == null ||
                duration <= 0f ||
                intensity <= 0f)
                return;

            if (!isRestart)
            {
                originalTranslate = element.style.translate;
                resolvedTranslate = element.resolvedStyle.translate;
            }

            var state = new ShakeState
            {
                originalTranslate = originalTranslate,
                resolvedTranslate = resolvedTranslate
            };

            ShakeStates.Add(element, state);

            double startTime = Time.realtimeSinceStartupAsDouble;

            float seed =
                (RuntimeHelpers.GetHashCode(element) & 0xFFFF) * 0.01f;

            state.detachCallback = _ => StopShake(element, state);
            element.RegisterCallback(state.detachCallback);

            state.task = element.schedule.Execute(() =>
            {
                if (!ShakeStates.TryGetValue(element, out ShakeState current) ||
                    !ReferenceEquals(current, state))
                    return;

                if (element.resourcesReleased || element.panel == null)
                {
                    StopShake(element, state);
                    return;
                }

                float elapsed =
                    (float)(Time.realtimeSinceStartupAsDouble - startTime);

                float progress = Mathf.Clamp01(elapsed / duration);

                if (progress >= 1f)
                {
                    StopShake(element, state);
                    return;
                }

                float strength = Mathf.SmoothStep(
                    intensity,
                    0f,
                    progress);

                float noiseTime = elapsed * 30f;

                float offsetX =
                    (Mathf.PerlinNoise(seed, noiseTime) * 2f - 1f) *
                    strength;

                float offsetY =
                    (Mathf.PerlinNoise(seed + 100f, noiseTime) * 2f - 1f) *
                    strength;

                float baseX = ResolveTranslateLength(
                    state.resolvedTranslate.x,
                    element.layout.width);

                float baseY = ResolveTranslateLength(
                    state.resolvedTranslate.y,
                    element.layout.height);

                element.style.translate = new Translate(
                    new Length(baseX + offsetX, LengthUnit.Pixel),
                    new Length(baseY + offsetY, LengthUnit.Pixel),
                    state.resolvedTranslate.z);
            }).Every(16);
        }

        public static void StopShake(this VisualElement element)
        {
            if (element == null ||
                !ShakeStates.TryGetValue(element, out ShakeState state))
                return;

            StopShake(element, state);
        }

        private static void StopShake(VisualElement element, ShakeState state)
        {
            if (!ShakeStates.TryGetValue(element, out ShakeState current) ||
                !ReferenceEquals(current, state))
                return;

            ShakeStates.Remove(element);
            state.task?.Pause();

            if (!element.resourcesReleased)
            {
                if (state.detachCallback != null)
                    element.UnregisterCallback(state.detachCallback);

                element.style.translate = state.originalTranslate;
            }

            state.task = null;
            state.detachCallback = null;
        }

        private static float ResolveTranslateLength(Length length, float size)
        {
            if (length.unit != LengthUnit.Percent)
                return length.value;

            if (float.IsNaN(size) || float.IsInfinity(size))
                return 0f;

            return size * length.value * 0.01f;
        }

        public static bool TryClickCooldown(this Button btn, int cooldownMs = 200)
        {
            if (btn == null)
                return false;

            // 按钮自身或父节点被禁用时，不允许触发。
            if (!btn.enabledInHierarchy)
                return false;

            // 不需要冷却。
            if (cooldownMs <= 0)
                return true;

            // 立即禁用，避免连续点击。
            btn.SetEnabled(false);

            // 冷却结束后重新启用。
            btn.schedule
                .Execute(() => btn.SetEnabled(true))
                .StartingIn(cooldownMs);

            return true;
        }

        public static void ToggleClass(this VisualElement element, string add, string remove)
        {
            if (element == null)
                return;

            if (!string.IsNullOrEmpty(remove))
                element.RemoveFromClassList(remove);

            if (!string.IsNullOrEmpty(add))
                element.AddToClassList(add);
        }

        public static void SetTransition(this VisualElement element, string propertyName, float duration, EasingMode easingMode = EasingMode.EaseInOut, float delay = 0f)
        {
            if (element == null) return;

            element.style.transitionProperty =
                new StyleList<StylePropertyName>(
                    new List<StylePropertyName>
                    {
                    new StylePropertyName(propertyName)
                    });

            element.style.transitionDuration =
                new StyleList<TimeValue>(
                    new List<TimeValue>
                    {
                    new TimeValue(duration, TimeUnit.Second)
                    });

            element.style.transitionTimingFunction =
                new StyleList<EasingFunction>(
                    new List<EasingFunction>
                    {
                    new EasingFunction(easingMode)
                    });

            element.style.transitionDelay =
                new StyleList<TimeValue>(
                    new List<TimeValue>
                    {
                    new TimeValue(delay, TimeUnit.Second)
                    });
        }

        public static void SetScale(this VisualElement element, Vector3 scale, float duration = 0.25F, EasingMode easingMode = EasingMode.EaseInOut, float delay = 0f)
        {
            if (element == null) return;

            element.SetTransition("scale", duration, easingMode, delay);
            element.style.scale = scale;
        }

        public static void SetVerticalSlide(this VisualElement element, float y, float opacity, float duration = 0.25f, EasingMode easingMode = EasingMode.EaseInOut, float delay = 0f)
        {
            if (element == null || element.resourcesReleased) return;

            duration = Mathf.Max(0f, duration);
            delay = Mathf.Max(0f, delay);
            element.style.transitionProperty = new StyleList<StylePropertyName>(new List<StylePropertyName>
            {
                new("translate"),
                new("opacity")
            });
            element.style.transitionDuration = new StyleList<TimeValue>(new List<TimeValue>
            {
                new(duration, TimeUnit.Second),
                new(duration, TimeUnit.Second)
            });
            element.style.transitionTimingFunction = new StyleList<EasingFunction>(new List<EasingFunction>
            {
                new(easingMode),
                new(easingMode)
            });
            element.style.transitionDelay = new StyleList<TimeValue>(new List<TimeValue>
            {
                new(delay, TimeUnit.Second),
                new(delay, TimeUnit.Second)
            });

            Translate current = element.resolvedStyle.translate;
            element.style.translate = new Translate(current.x, new Length(y, LengthUnit.Pixel), current.z);
            element.style.opacity = Mathf.Clamp01(opacity);
        }

        public static void SetOpacity(this VisualElement element, float opacity, float duration = 0.25F, EasingMode easingMode = EasingMode.EaseInOut, float delay = 0f)
        {
            if (element == null) return;

            element.SetTransition("opacity", duration, easingMode, delay);
            element.style.opacity = opacity;
        }
    }
}
