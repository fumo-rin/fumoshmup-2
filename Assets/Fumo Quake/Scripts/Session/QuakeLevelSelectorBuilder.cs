using System.Collections.Generic;
using rinCore;
using UnityEngine;

namespace FumoQuake
{
    public class QuakeLevelSelectorBuilder : MonoBehaviour, IHierarchyComponentColor
    {
        [SerializeField] List<ScenePairSO> levelCollection = new();
        [SerializeField] List<ScenePairSO> editorCollection = new();
        [SerializeField] QuakeLevelSelector template;
        public Color LabelColor => ColorHelper.PastelRed.Opacity(50);

        private void Awake()
        {
            Transform parent = template.transform.parent;
            foreach (var item in levelCollection)
            {
                Instantiate(template, parent).SetLevel(item);
            }
            foreach (var item in editorCollection)
            {
                QuakeLevelSelector.settings s = new()
                {
                    color = ColorHelper.PastelYellow
                };
                Instantiate(template, parent).SetLevel(item, s);
            }
            template.gameObject.SetActive(false);
        }
    }
}
