using System;
using UnityEngine;
using UnityEngine.UIElements;
namespace UnknownCreator.Modules
{
    public static class UITKGlobals
    {
        /// <summary>
        /// 如果当前按钮可点击，则设置冷却并返回 true；否则返回 false。
        /// </summary>
        public static bool TryClickCooldown(this Button btn, int cooldownMs = 200)
        {
            if (btn == null || !btn.enabledSelf)
                return false;

            btn.SetEnabled(false);
            btn.schedule.Execute(() => btn?.SetEnabled(true)).StartingIn(cooldownMs);
            return true;
        }


        /// <summary>
        /// 抖动效果
        /// </summary>
        /// <param name="element">要抖动的UI元素</param>
        /// <param name="duration">抖动持续时间(秒)</param>
        /// <param name="intensity">抖动强度(像素)</param>
        public static void Shake(this VisualElement element, float duration = .5f, float intensity = 10f)
        {
            var originalPosition = Vector3.zero;
            float startTime = Time.realtimeSinceStartup;
            float frequency = 15f;

            IVisualElementScheduledItem scheduler = element.schedule.Execute(() =>
            {
                float elapsed = Time.realtimeSinceStartup - startTime;
                float progress = elapsed / duration;

                if (progress >= .9f)
                {
                    element.style.translate = originalPosition;
                    return;
                }

                // 使用衰减曲线使抖动逐渐减弱
                float decay = Mathf.Clamp01(1 - progress);
                float shake = Mathf.Sin(elapsed * frequency) * intensity * decay;

                // 添加一些随机性但保持连贯
                Vector2 offset = new(
                    shake * RVUtils.RandomFloat(-1f, 1f, true),
                    shake * RVUtils.RandomFloat(-1f, 1f, true)
                );

                element.style.translate = new StyleTranslate(new Translate(
                    originalPosition.x + offset.x,
                    originalPosition.y + offset.y,
                    0
                ));
            }).Every(16).ForDuration((long)(duration * 1000));
        }


        /// <summary>
        /// 缩放效果
        /// </summary>
        /// <param name="element"></param>
        /// <param name="duration"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public static void Scale(this VisualElement element, float duration = 0.3f, float from = 0f, float to = 1f)
        {
            float startTime = Time.realtimeSinceStartup;

            element.style.scale = UnityGlobals.NewV3(from);

            element.schedule.Execute(() =>
            {
                float elapsed = Time.realtimeSinceStartup - startTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float currentScale = Mathf.Lerp(from, to, progress);
                element.style.scale = UnityGlobals.NewV3(currentScale);

            }).Every(16).ForDuration((long)(duration * 1000));
        }


        /// <summary>
        /// 透明度渐变效果
        /// </summary>
        /// <param name="element">要渐变的UI元素</param>
        /// <param name="duration">持续时间（秒）</param>
        /// <param name="from">起始透明度（0~1）</param>
        /// <param name="to">目标透明度（0~1）</param>
        public static void Fade(this VisualElement element, float duration = 0.3f, float from = 0f, float to = 1f)
        {
            float startTime = Time.realtimeSinceStartup;

            element.style.opacity = from;

            element.schedule.Execute(() =>
            {
                float elapsed = Time.realtimeSinceStartup - startTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float currentOpacity = Mathf.Lerp(from, to, progress);
                element.style.opacity = currentOpacity;

            }).Every(16).ForDuration((long)(duration * 1000));
        }



        /// <summary>
        /// 滑动效果（支持上下左右方向）
        /// </summary>
        public static void Slide(this VisualElement element, float duration, Vector2 from, Vector2 to)
        {
            float startTime = Time.realtimeSinceStartup;
            element.style.translate = new Translate(from.x, from.y, 0);

            element.schedule.Execute(() =>
            {
                float elapsed = Time.realtimeSinceStartup - startTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float x = Mathf.Lerp(from.x, to.x, progress);
                float y = Mathf.Lerp(from.y, to.y, progress);
                element.style.translate = new Translate(x, y, 0);

            }).Every(16).ForDuration((long)(duration * 1000));
        }

    }
}