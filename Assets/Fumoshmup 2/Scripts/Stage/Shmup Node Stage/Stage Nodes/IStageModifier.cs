using System.Collections;
using UnityEngine;
using UnityEngine.Search;
using rinCore;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Search;
using static rinCore.EF_Utility;
#endif

namespace FumoShmup2
{
    public interface IStageNodeModifier
    {
        List<StageNode> LinkedNodes { get; }
        public void LinkNode(StageNode node)
        {
            LinkedNodes.AddIfDoesntExist(node);
        }
        public void UnlinkNode(StageNode node)
        {
            LinkedNodes.Remove(node as StageNode);
        }
        public void RevalidateNodes()
        {
            bool bad = false;
            foreach (var item in LinkedNodes)
            {
                if (item == null)
                    bad = true;
            }
            if (bad)
            {
                var other = LinkedNodes.ToList();
                LinkedNodes.Clear();
                foreach (var item in other)
                {
                    if (item == null)
                    {
                        continue;
                    }
                    LinkedNodes.AddIfDoesntExist(item);
                }
            }
        }
        public IEnumerator ModifyNode(IStageNodeRunable runable);
    }
}