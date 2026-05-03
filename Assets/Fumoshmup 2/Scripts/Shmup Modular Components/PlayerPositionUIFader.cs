using rinCore;
using UnityEngine;

namespace FumoShmup2
{
    [RequireComponent(typeof(CanvasGroup))]
    public class PlayerPositionUIFader : MonoBehaviour
    {
        CanvasGroup fadingGroup;
        [SerializeField, Range(0f, 1f)] float maxFade01 = 0.8f;
        [SerializeField] float fadeLerpSpeed = 12f;
        float currentAlpha = 1f;
        void Awake()
        {
            fadingGroup = GetComponent<CanvasGroup>();
            if (fadingGroup != null)
                currentAlpha = fadingGroup.alpha;
        }
        void Update()
        {
            if (!FumoUnit.PlayerAs(out FumoUnit p))
                return;

            ShmupWorldspace.MapWorldspaceToNormalized(p.CurrentPosition, out Vector2 n, true);
            float lerp = n.y.MapTo01(0.25f, 0.75f).Clamp(0f, 1f).MapFrom01(0f, maxFade01);
            float targetAlpha = 1f - lerp;
            currentAlpha = currentAlpha.MoveTowards(targetAlpha, Time.deltaTime * fadeLerpSpeed);
            fadingGroup.alpha = currentAlpha;
        }
    }
}