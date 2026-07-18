using rinCore;
using TMPro;
using UnityEngine;

namespace FumoQuake
{
    public interface IQuakeTextName
    {
        public string TextName { get; }
    }
    public class QuakeTextInfoUI : MonoBehaviour
    {
        static QuakeTextInfoUI instance;
        [SerializeField] TMP_Text textTemplate;
        RectTransform textAnchor;
        private void Awake()
        {
            instance = this;
            textAnchor = textTemplate.transform.parent.GetComponent<RectTransform>();
            textTemplate.gameObject.SetActive(false);
        }
        public static void AddText(string text)
        {
            if (instance is not QuakeTextInfoUI i)
            {
                return;
            }
            TMP_Text item = Instantiate(i.textTemplate, i.textAnchor);
            item.gameObject.SetActive(true);
            Destroy(item.gameObject, 3f);
            item.text = text;
        }
    }
}
