using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Projectiles
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        [field: SerializeField] public Collider2D HomingSensor { get; private set; }

        public Rigidbody2D Rigidbody { get; private set; }
        public Collider2D Collider { get; private set; }
        public ProjectileSO ProjectileSO { get; private set; }
        public int HorizontalFacing { get; private set; } = 1;

        ContactFilter2D hitMask;
        ContactFilter2D collisionMask;
        ContactFilter2D homingMask;

        private int numColliders = 0;
        private readonly Collider2D[] colliders = new Collider2D[50];
        private readonly HashSet<Collider2D> previousHits = new(50);
        private float currentExpireTime = 0;


        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody2D>();
            Collider = GetComponent<Collider2D>();

            hitMask = new ContactFilter2D
            {
                useTriggers = true
            };

            collisionMask = new ContactFilter2D
            {
                useTriggers = true
            };

            gameObject.SetActive(false);
        }

        public void Initialize(ProjectileSO projectileSO)
        {
            ProjectileSO = projectileSO;
            hitMask.SetLayerMask(LayerUtility.CombineMasks(ProjectileSO.Hit.HitMasks));
            collisionMask.SetLayerMask(LayerUtility.CombineMasks(ProjectileSO.Collision.CollisionMasks));
            homingMask.SetLayerMask(LayerUtility.CombineMasks(ProjectileSO.Homing.HomingMasks));

            currentExpireTime = ProjectileSO.Duration + Time.time;
            gameObject.SetActive(true);
        }

        private void FixedUpdate()
        {
            Rigidbody.velocity = ProjectileSO.Speed * transform.right;

            if (ProjectileSO.Hit.Enabled)
            {
                numColliders = Collider.OverlapCollider(hitMask, colliders);

                if (numColliders > 0)
                {
                    for (int i = 0; i < numColliders; i++)
                    {
                        if (!previousHits.Contains(colliders[i]))
                        {
                            OnHit(colliders[i]);
                        }

                        previousHits.Add(colliders[i]);
                    }
                }
            }

            if (ProjectileSO.Collision.Enabled)
            {
                numColliders = Collider.OverlapCollider(collisionMask, colliders);

                if (numColliders > 0)
                {
                    for (int i = 0; i < numColliders; i++)
                    {
                        OnCollision(colliders[i]);
                    }

                    ExpireProjectile();
                    return;
                }
            }

            if (currentExpireTime < Time.time)
            {
                ExpireProjectile();
                return;
            }

            if (ProjectileSO.Homing.Enabled)
            {
                numColliders = HomingSensor.OverlapCollider(homingMask, colliders);

                if (numColliders > 0)
                {
                    for (int i = 0; i < numColliders; i++)
                    {
                        Homing(colliders[i]);
                    }

                }
            }

        }

        private void OnHit(Collider2D target)
        {
            if (target.TryGetComponent(out IEffectable effectable))
            {
                if (ProjectileSO.Hit.EffectOnHit != null)
                {
                    effectable.ApplyEffect(ProjectileSO.Hit.EffectOnHit);
                }
            }

            if (target.TryGetComponent(out IMovable movable))
            {

                if (ProjectileSO.Hit.ForceOnHit != null)
                {
                    Vector2 direction = Rigidbody.velocity.normalized;
                    movable.ApplyRelativeForce(direction.x, ProjectileSO.Hit.ForceOnHit);
                }
            }
        }

        private void OnCollision(Collider2D target)
        {
            if (target.TryGetComponent(out IEffectable effectable))
            {
                if (ProjectileSO.Collision.EffectOnCollision != null)
                {
                    effectable.ApplyEffect(ProjectileSO.Collision.EffectOnCollision);
                }
            }

            if (target.TryGetComponent(out IMovable movable))
            {

                if (ProjectileSO.Collision.ForceOnCollision != null)
                {
                    Vector2 direction = Rigidbody.velocity.normalized;
                    movable.ApplyRelativeForce(direction.x, ProjectileSO.Collision.ForceOnCollision);
                }
            }
        }

        private void ExpireProjectile()
        {
            Destroy(gameObject);
        }

        private void Homing(Collider2D target)
        {
            Vector3 targetDirection = (target.transform.position - transform.position).normalized;
            Vector3 rotatedDirection = Quaternion.Euler(0, 0, 90) * targetDirection;
            Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, rotatedDirection);

            float angularSpeed = ProjectileSO.Homing.AngularSpeed * Time.fixedDeltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, angularSpeed);
        }
    }

}