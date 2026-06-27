using rinCore;
using System.Collections;
using TMPro;
using UnityEngine;

namespace FumoShmup2
{
    public class ShmupTextBuilderUI : MonoBehaviour
    {
        #region Text Packet
        [System.Serializable]
        public class textPacket
        {
            public float duration;
            public Color32 color;
            public Vector2 position01;
            public Vector2 size01;
            public float fontSize;

            public HorizontalAlignmentOptions horizontalAlignment;
            public VerticalAlignmentOptions verticalAlignment;
        }
        #endregion

        [SerializeField] TMP_Text cloneable;
        [SerializeField] RectTransform textSpaceAnchor;

        static ShmupTextBuilderUI instance;

        private void Awake()
        {
            instance = this;
            cloneable.gameObject.SetActive(false);
        }

        public static void CreateText(string text, textPacket packet)
        {
            if (!RinHelper.ValidGameObjects(instance))
                return;

            CO_Text(text, packet).RunRoutine();

            IEnumerator CO_Text(string text, textPacket packet)
            {
                const float fade = 0.5f;

                TMP_Text clone = Instantiate(instance.cloneable, instance.textSpaceAnchor);
                RectTransform rt = clone.rectTransform;
                Rect parentRect = instance.textSpaceAnchor.rect;

                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);

                rt.anchoredPosition = new Vector2(
                    (packet.position01.x - 0.5f) * parentRect.width,
                    (packet.position01.y - 0.5f) * parentRect.height);

                if (packet.size01 != Vector2.zero)
                {
                    rt.sizeDelta = new Vector2(
                        packet.size01.x * parentRect.width,
                        packet.size01.y * parentRect.height);
                }

                clone.enableAutoSizing = true;
                clone.fontSizeMax = packet.fontSize;
                clone.fontSizeMin = packet.fontSize * 0.25f;

                clone.horizontalAlignment = packet.horizontalAlignment;
                clone.verticalAlignment = packet.verticalAlignment;

                clone.text = text;
                clone.color = packet.color.Opacity(0);
                clone.gameObject.SetActive(true);

                float entry = fade;
                while (entry > 0)
                {
                    float lerp01 = entry.MapTo01(fade, 0f, true);

                    clone.color = clone.color.Opacity(
                        lerp01.MapFrom01(0f, 255f).ToByte());

                    entry -= Time.deltaTime;
                    yield return null;
                }

                clone.color = packet.color;

                yield return packet.duration.WaitForSeconds();

                float exit = fade;
                while (exit > 0)
                {
                    float lerp01 = exit.MapTo01(fade, 0f, true);

                    clone.color = clone.color.Opacity(
                        lerp01.MapFrom01(255f, 0f).ToByte());

                    exit -= Time.deltaTime;
                    yield return null;
                }

                Destroy(clone.gameObject);
            }
        }
    }
}