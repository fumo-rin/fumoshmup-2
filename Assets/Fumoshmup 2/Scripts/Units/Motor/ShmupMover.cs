using UnityEngine;
using rinCore;

namespace FumoShmup2
{
    public partial class ShmupMovers
    {
        [System.Serializable]
        public class PlayerShmupMover : IShmupMover
        {
            public float maxSpeed;
            public PlayerShmupMover(float speed)
            {
                maxSpeed = speed;
            }
            public void Move(ShmupUnit owner, Vector2 input, out IShmupMover.MoveSuccess result)
            {
                if (GeneralManager.IsPaused)
                {
                    result = IShmupMover.MoveSuccess.NotRunning;
                    return;
                }
                result = IShmupMover.MoveSuccess.Default;

                bool deadzoneSuccess = GenericInput.ProcessWithDeadzone(input, out Vector2 processed);
                processed = processed.QuantizeToStepSize(45f);

                result = IShmupMover.MoveSuccess.Success;

                if (deadzoneSuccess && processed.magnitude < 0.05f)
                {
                    result = IShmupMover.MoveSuccess.Default;
                    owner.RB.linearVelocity = Vector2.zero;
                    return;
                }

                owner.RB.linearVelocity = processed.ScaleToMagnitude(maxSpeed);
            }
        }
    }
    public interface IShmupMover
    {
        public enum MoveSuccess
        {
            Default,
            Success,
            Failed,
            Stalled,
            NotRunning
        }
        public void Move(ShmupUnit owner, Vector2 input, out MoveSuccess result);
    }
}
