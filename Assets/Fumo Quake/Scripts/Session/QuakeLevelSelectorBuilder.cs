using System.Collections.Generic;
using rinCore;
using UnityEngine;

namespace FumoQuake
{
    public class QuakeLevelSelectorBuilder : MonoBehaviour, IHierarchyComponentColor
    {
        [SerializeField] List<ScenePairSO> levelCollection = new();
        [SerializeField] QuakeLevelSelector template;
        public Color LabelColor => ColorHelper.PastelRed.Opacity(50);

        private void Awake()
        {
            Transform parent = template.transform.parent;
            foreach (var item in levelCollection)
            {
                Instantiate(template, parent).SetLevel(item);
            }
            template.gameObject.SetActive(false);
        }
    }
}
