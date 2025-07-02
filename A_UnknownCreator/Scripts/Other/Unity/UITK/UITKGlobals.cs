using System;
using UnityEngine;
using UnityEngine.UIElements;
namespace UnknownCreator.Modules
{
    public static class UITKGlobals
    {
        /// <summary>
        /// �����ǰ��ť�ɵ������������ȴ������ true�����򷵻� false��
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
        /// ����Ч��
        /// </summary>
        /// <param name="element">Ҫ������UIԪ��</param>
        /// <param name="duration">��������ʱ��(��)</param>
        /// <param name="intensity">����ǿ��(����)</param>
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

                // ʹ��˥������ʹ�����𽥼���
                float decay = Mathf.Clamp01(1 - progress);
                float shake = Mathf.Sin(elapsed * frequency) * intensity * decay;

                // ���һЩ����Ե���������
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
        /// ����Ч��
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
        /// ͸���Ƚ���Ч��
        /// </summary>
        /// <param name="element">Ҫ�����UIԪ��</param>
        /// <param name="duration">����ʱ�䣨�룩</param>
        /// <param name="from">��ʼ͸���ȣ�0~1��</param>
        /// <param name="to">Ŀ��͸���ȣ�0~1��</param>
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
        /// ����Ч����֧���������ҷ���
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