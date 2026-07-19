using rinCore;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace FumoQuake
{
    [System.Serializable]
    public class StrafeController
    {
        [System.Serializable]
        public struct StrafeProfile
        {
            public float maxDistance;
            public float strafeAngle;
            public float flipMin;
            public float flipMax;
            public float wallScanDistance;
            [HideInInspector] public float nextFlipTime;
            [HideInInspector] public float currentDirectionSign;
        }
        public List<StrafeProfile> profiles = new()
            {
                new StrafeProfile { maxDistance = 6f,  strafeAngle = 90f, flipMin = 0.2f, flipMax = 0.5f, wallScanDistance = 2.75f },
                new StrafeProfile { maxDistance = 15f, strafeAngle = 80f, flipMin = 0.5f, flipMax = 1.0f, wallScanDistance = 2.75f }
            };
        public void Path_TryStrafeThenPathTowards(QuakeEnemy e, IFumoUnit other)
        {
            if (other == null || !other.IsAlive)
            {
                if (e.navigation.Nav.HasDestination) return;
                if (e.navigation.Nav.TryProjectToNavmesh(e.lastKnownTarget + RNG.SeededRandomInsideUnitSphere * 3f, out Vector3 celebration, 5f))
                {
                    e.navigation.SetNewTarget(celebration);
                }
                return;
            }

            bool planarFail = (other.CurrentPosition.y - e.CurrentPosition.y).Absolute() > 3f;
            if (!planarFail && other is ITargetting targetedInstance && e is IStrafe strafe)
            {
                Vector3 strafeDirection = Vector3.zero;

                if (strafe.TryStrafe(ref strafeDirection, targetedInstance))
                {
                    Vector3 prospectiveStrafeTarget = e.transform.position + (strafeDirection * 2.5f);
                    if (NavMesh.SamplePosition(prospectiveStrafeTarget, out NavMeshHit strafeHit, 1.5f, NavMesh.AllAreas))
                    {
                        e.navigation.SetNewTarget(strafeHit.position);
                        e.lastKnownTarget = strafeHit.position;
                        return;
                    }
                }
            }
            e.Path_DirectlyTowards(other);
        }
        public bool TryRunStrafe(Transform origin, ref Vector3 velocity, ITargetting target)
        {
            if (target == null || !target.TargetActive) return false;

            Vector3 targetPos = target.FirstTargetPosition;
            Vector3 toTarget = (targetPos - origin.position).Y(0f);
            float distance = toTarget.magnitude;

            if (distance < 0.1f) return false;

            int profileIndex = profiles.FindIndex(p => distance <= p.maxDistance);
            if (profileIndex == -1) return false;

            StrafeProfile profile = profiles[profileIndex];
            Vector3 forwardDir = toTarget / distance;

            if (profile.currentDirectionSign == 0f || Time.time >= profile.nextFlipTime)
            {
                profile.currentDirectionSign = RNG.FloatRange(0f, 1f) > 0.5f ? 1f : -1f;
                profile.nextFlipTime = Time.time + RNG.FloatRange(profile.flipMin, profile.flipMax);
            }

            float finalAngle = profile.strafeAngle * profile.currentDirectionSign;
            Vector3 finalStrafeDirection = (Quaternion.Euler(0f, finalAngle, 0f) * forwardDir).normalized;

            Vector3 rayOrigin = origin.position + new Vector3(0f, 0.5f, 0f);
            if (Physics.Raycast(rayOrigin, finalStrafeDirection, profile.wallScanDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                profile.currentDirectionSign *= -1f;
                finalAngle = profile.strafeAngle * profile.currentDirectionSign;
                finalStrafeDirection = (Quaternion.Euler(0f, finalAngle, 0f) * forwardDir).normalized;

                if (Physics.Raycast(rayOrigin, finalStrafeDirection, profile.wallScanDistance, ~0, QueryTriggerInteraction.Ignore))
                {
                    return false;
                }

                profile.nextFlipTime = Time.time + RNG.FloatRange(profile.flipMin, profile.flipMax);
            }
            profiles[profileIndex] = profile;
            velocity = finalStrafeDirection;
            return true;
        }
    }
}
