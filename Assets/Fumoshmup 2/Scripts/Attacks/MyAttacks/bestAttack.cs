using UnityEngine;
using rinCore;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;

namespace FumoShmup2
{
     [System.Serializable]
    public class bestAttack : MonoBehaviour
    {
         [System.Serializable]
        public class LingeringLine : UnitAttack
        {
            public ProjectileDefineSO projectile;
            private float maxSpeed = 4f;
            private float minSpeed = 10f;
            private int n_bullets = 5;
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                 // Spawn n_bullets at once, all aimed at the player with different speeds
                // such that the slower bullets linger and provide small obstacles when the player restreams

                float n_bulletsf = n_bullets-1; // cast to float once for reuse

                //input.SetOrigin(sender.CurrentPosition);
                input.ReAimWithOptionalTarget(sender.CurrentPosition);
                 for (int i = 0; i < n_bullets; i++){
                    float this_speed = (maxSpeed - minSpeed) * (i/n_bulletsf)+ minSpeed;
                    Single(0f, this_speed).Spawn(input, projectile, out Projectile p);
                 }
                 
                yield return 0.0f.WaitForSeconds();
            }
        }

        [System.Serializable]
        public class LingeringCircle : UnitAttack
        {
            public ProjectileDefineSO projectile;
            private float maxSpeed = 4f;
            private float minSpeed = 10f;
            private int n_circles = 5;
             private int n_bullets = 15;
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                 // Spawn n_bullets at once, all aimed at the player with different speeds
                // such that the slower bullets linger and provide small obstacles when the player restreams

                float n_circlesf = n_circles-1; // cast to float once for reuse

                //input.SetOrigin(sender.CurrentPosition);
                 for (int i = 0; i < n_circles; i++){
                    float this_speed = (maxSpeed - minSpeed) * (i/n_circlesf)+ minSpeed;
                    Circle(0f, n_bullets, this_speed).Spawn(input, projectile, out iterationList);
                 }
                yield return 0.0f.WaitForSeconds();
            }
        }

        [System.Serializable]
        public class Rain : UnitAttack
        {
            public ProjectileDefineSO projectile;
            private float speed = 7f;
            private float angleSpread = 5f;
             private float offsetSpread = 1f;
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
               
                Vector2 offset = new Vector2(RNG.FloatRange(-offsetSpread, offsetSpread), 0f);

                 input.SetOrigin(sender.CurrentPosition + offset);
                input.SetDirection(new Vector2(0,-1));
                Single(RNG.FloatRange(-angleSpread, angleSpread), speed).Spawn(input, projectile, out Projectile p);

                 
                yield return 0.0f.WaitForSeconds();
            }
        }
    }
}
