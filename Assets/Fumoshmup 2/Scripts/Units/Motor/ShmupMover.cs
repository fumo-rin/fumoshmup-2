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
                input = input.QuantizeToStepSize(45f);
                result = IShmupMover.MoveSuccess.Default;
                if (GeneralManager.IsPaused)
                {
                    result = IShmupMover.MoveSuccess.NotRunning;
                    return;
                }
                result = IShmupMover.MoveSuccess.Success;
                if (input.magnitude < 0.1f)
                {
                    owner.RB.linearVelocity = Vector2.zero;
                    return;
                }
                owner.RB.linearVelocity = input.ScaleToMagnitude(maxSpeed);
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
