using System.Collections.Generic;
using UnityEngine;
using rinCore;
namespace FumoShmup2
{
    public class ShmupCameraWorldMatcher : MonoBehaviour
    {
        [SerializeField] List<Transform> matchObjects = new();
        Vector3 offset;
        [SerializeField, Range(0.1f, 5f)] float ratio = .2f;
        private void LateUpdate()
        {
            ShmupCamera.ApplyOffset(ref offset);
            foreach (Transform t in matchObjects)
            {
                if (t == null || t.gameObject == null || !t.gameObject.activeInHierarchy)
                    continue;
                t.localPosition = offset * ratio;
            }
        }
    }
}