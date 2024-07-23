using Effect;
using System;
using UnityEngine;

namespace Enemy
{
    public enum EnemyState
    {
        Inactive,
        Sleeping,
        Patrolling,
        Chasing,
        Sweeping,
        Watching,
        Attacking,
    }

    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class EnemyController : MonoBehaviour, IEffectable, IMovable
    {

        public Rigidbody2D Rigidbody { get; private set; }
        public Collider2D RigidbodyCollider { get; private set; }
        public Animator Animator { get; private set; }

        [field: Space]
        [field: SerializeField] public EnemyData EnemyData { get; private set; }


        [field: Space]
        [field: Header("Enemy Pivots")]
        [field: SerializeField] public Transform AttackPivot { get; private set; }
        [field: SerializeField] public Transform FirePoint { get; private set; }
        [field: SerializeField] public Transform SightPivot { get; private set; }

        [field: Space]
        [field: Header("Enemy Sensors")]
        //[field: SerializeField] public Collider2D NearbySensor { get; private set; }
        [field: SerializeField] public Collider2D PatrolSensor { get; private set; }
        [field: SerializeField] public Collider2D ChaseSensor { get; private set; }
        [field: SerializeField] public Collider2D SweepSensor { get; private set; }
        [field: SerializeField] public Collider2D AttackSensor { get; private set; }
        [field: SerializeField] public Collider2D LedgeSensor { get; private set; }
        [field: SerializeField] public Collider2D WallSensor { get; private set; }
        [field: SerializeField] public Collider2D GroundSensor { get; private set; }
        [field: SerializeField] public LayerMask[] SightOcclusionMasks { get; private set; }

        [field: Space]
        [field: Header("Attack Components")]
        [field: SerializeField] public ParticleSystem AttackParticles { get; private set; }

        public EnemyState EnemyState { get; private set; } = EnemyState.Inactive;
        public int HorizontalFacing { get; private set; } = 1;


        private ContactFilter2D groundFilter;
        private ContactFilter2D targetFilter;

        private readonly Collider2D[] sensorResults = new Collider2D[50];
        private float currentStateDuration = 0.0f;
        private LayerMask sightOcclusionMask;
        private float currentAttackCooldown = 0;
        private float attackAnimationLength = 0;
        private float currentAttackAnimationTime = 0;
        private float currentFlipWaitTime = 0;
        private bool isWaitingToFlip = false;
        private Vector2 lastTargetDirection = Vector2.zero;

        private (Slow SlowData, float Duration) currentSlow = (null, 0f);
        private (Stun StunData, float Duration) currentStun = (null, 0f);

        public float Slow
        {
            get
            {
                if(currentSlow.SlowData != null 
                    && currentSlow.Duration > Time.time)
                {
                    return currentSlow.SlowData.Percent;
                }

                return 0;
            }
        }

        public bool Stunned
        {
            get
            {
                if (currentStun.StunData != null
                    && currentStun.Duration > Time.time)
                {
                    return true;
                }

                return false;
            }
        }

        //debug
        private Vector3 gizmoTarget;

        protected virtual void Start()
        {
            Rigidbody = GetComponent<Rigidbody2D>();
            RigidbodyCollider = GetComponent<Collider2D>();
            Animator = GetComponent<Animator>();

            EnemyState = EnemyState.Patrolling;

            groundFilter = new ContactFilter2D
            {
                useTriggers = true
            };

            targetFilter = new ContactFilter2D
            {
                useTriggers = true
            };

            groundFilter.SetLayerMask(LayerMask.GetMask("Ground"));
            targetFilter.SetLayerMask(LayerMask.GetMask("Player"));

            sightOcclusionMask = LayerUtility.CombineMasks(SightOcclusionMasks);
            attackAnimationLength = GetClipLength("Attack");
        }

        protected void FixedUpdate()
        {
            if(!GroundCheck() || Stunned)
                return;

            switch (EnemyState)
            {
                case EnemyState.Sleeping:
                    if (NearbyProximityCheck(out Collider2D collider)
                    && SightCheck(collider.transform))
                    {
                        RemoveVelocity();
                        ChangeState(EnemyState.Patrolling);
                    }

                    return;

                case EnemyState.Patrolling:
                    if(EnemyData.HasSleepState 
                        && currentStateDuration < Time.time)
                    {
                        RemoveVelocity();
                        ChangeState(EnemyState.Sleeping);
                    }

                    if (PatrolProximityCheck(out collider)
                        && SightCheck(collider.transform))
                    {
                        RemoveVelocity();
                        ChangeState(EnemyState.Chasing);
                    }

                    else if(LedgeCheck() || WallCheck())
                    {
                        RemoveVelocity();
                        PrepareFlipCharacter(EnemyData.PatrolFlipTime);
                    }

                    else
                    {
                        UpdateVelocity(new(HorizontalFacing, 0), EnemyData.PatrolSpeed);                       
                    }

                    return;

                case EnemyState.Chasing:
                    if (currentAttackCooldown > Time.time
                        && !EnemyData.ChaseOnAttackCooldown)
                    {
                        RemoveVelocity();
                    }

                    else if (ChaseProximityCheck(out collider)
                        && SightCheck(collider.transform))
                    {
                        bool targetIsInFront = IsPointInFront(collider.transform.position);

                        if(targetIsInFront)
                        {
                            RotateAttackPivotTowards(collider.transform.position);
                        }

                        if (AttackProximityCheck(out _) 
                            && currentAttackCooldown < Time.time)
                        {
                            RemoveVelocity();
                            ChangeState(EnemyState.Attacking);
                        }

                        else
                        {

                            if (LedgeCheck() || WallCheck())
                            {
                                if (EnemyData.HasWatchState && targetIsInFront)
                                {
                                    RemoveVelocity();
                                    ChangeState(EnemyState.Watching);
                                }

                                else
                                {
                                    RemoveVelocity();
                                    PrepareFlipCharacter(EnemyData.ChaseFlipTime);
                                }
                            }

                            else if(!targetIsInFront)
                            {
                                RemoveVelocity();
                                PrepareFlipCharacter(EnemyData.ChaseFlipTime);
                            }

                            else
                            {
                                UpdateVelocity(new(HorizontalFacing, 0), EnemyData.ChaseSpeed);
                            }
                        }
                    }

                    else
                    {
                        RemoveVelocity();
                        ChangeState(EnemyState.Sweeping);
                    }

                    return;

                case EnemyState.Sweeping:
                    if (SweepProximityCheck(out collider)
                        && SightCheck(collider.transform))
                    {
                        RemoveVelocity();
                        ChangeState(EnemyState.Chasing);
                    }

                    else if (currentStateDuration < Time.time)
                    {
                        RemoveVelocity();
                        ChangeState(EnemyState.Patrolling);
                    }

                    else if (LedgeCheck() || WallCheck())
                    {
                        RemoveVelocity();
                        PrepareFlipCharacter(EnemyData.SweepFlipTime);
                    }

                    else
                    {
                        UpdateVelocity(new(HorizontalFacing, 0), EnemyData.SweepSpeed);
                    }

                    return;

                case EnemyState.Attacking:
                    if (currentAttackAnimationTime < Time.time)
                    {
                        OnAttackComplete();
                        ChangeState(EnemyState.Chasing);
                    }

                    return;
                case EnemyState.Watching:
                    //TODO: Add state duration

                    if (ChaseProximityCheck(out collider)
                        && SightCheck(collider.transform))
                    {

                        if (AttackProximityCheck(out _) 
                            && currentAttackCooldown < Time.time)
                        {
                            ChangeState(EnemyState.Attacking);
                        }

                        else if (!IsPointInFront(collider.transform.position))
                        {
                            ChangeState(EnemyState.Chasing);
                        }

                        else
                        {
                            RemoveVelocity();
                        }
                    }

                    else
                    {
                        ChangeState(EnemyState.Sweeping);
                    }

                    return;

                default: //EnemyState.Inactive:
                    return;
            }
        }

        private void ChangeState(EnemyState state)
        {
            isWaitingToFlip = false;
            currentStateDuration = 0;

            Debug.Log($"Switching state to: {state}");

            switch (state)
            {
                case EnemyState.Sleeping:
                    ResetAttackPivot();
                    break;

                case EnemyState.Patrolling:
                    ResetAttackPivot();
                    currentStateDuration = EnemyData.PatrolDuration + Time.time;
                    break;

                case EnemyState.Chasing:
                    currentAttackCooldown = EnemyData.AttackCooldown + Time.time;
                    break;
                case EnemyState.Sweeping:
                    ResetAttackPivot();
                    currentStateDuration = EnemyData.SweepDuration + Time.time;
                    break;
                case EnemyState.Attacking:

                    currentAttackAnimationTime = attackAnimationLength
                        + EnemyData.AttackCooldown
                        + Time.time;

                    Animator.SetTrigger("Attack");
                    Debug.Log("Attacking");

                    break;
                case EnemyState.Watching:
                    ResetAttackPivot();
                    UpdateVelocity(new Vector2(HorizontalFacing, 0), 0);
                    break;
                default: //EnemyState.Inactive:
                    break;
            }

            EnemyState = state;
        }

        //private bool BeforeStateChange(EnemyState state)
        //{
        //    currentStateDuration = 0;

        //    switch (state)
        //    {
        //        case EnemyState.Patrolling:
        //            break;

        //        case EnemyState.Chasing:
        //            break;
        //        case EnemyState.Sweeping:
        //            currentStateDuration = EnemyData.SweepDuration + Time.time;
        //            break;
        //        case EnemyState.Attacking:
        //            currentAttackCooldown = EnemyData.AttackCooldown + Time.time;
        //            break;
        //        case EnemyState.Watching:
        //            UpdateVelocity(new Vector2(HorizontalFacing, 0), 0);
        //            break;
        //        default: //EnemyState.Inactive:
        //            break;
        //    }
        //}

        protected bool PrepareFlipCharacter(float flipTime)
        {
            if (!isWaitingToFlip)
            {
                isWaitingToFlip = true;
                currentFlipWaitTime = flipTime + Time.time;
            }

            else if (currentFlipWaitTime < Time.time)
            {
                FlipCharacter();
                isWaitingToFlip = false;
                return true;
            }

            return false;
        }

        protected void FlipCharacter()
        {
            Vector3 localScale = transform.localScale;
            localScale.x *= -1;
            transform.localScale = localScale;

            HorizontalFacing = -HorizontalFacing;
            //Debug.Log($"Flipping horizontal direction to: {HorizontalFacing}");
        }

        private bool ColliderCheck(Collider2D sensor, ContactFilter2D filter, out Collider2D hitCollider)
        {
            int numColliders = sensor.OverlapCollider(filter, sensorResults);
            hitCollider = null;

            for (int i = 0; i < numColliders; i++)
            {          
                Collider2D collider = sensorResults[i];
                if (collider != null)
                {
                    hitCollider = collider;
                    return true;
                }
            }
;
            return false;
        }

        private bool RaycastTransformCheck(Vector2 startPoint, Transform target, LayerMask mask)
        {
            mask = LayerUtility.CombineMasks(mask, LayerUtility.LayerToLayerMask(target.gameObject.layer));
            Vector2 direction = (Vector2)target.position - startPoint;
            RaycastHit2D hit = Physics2D.Raycast(startPoint, direction, Mathf.Infinity, mask);

            gizmoTarget = hit.point;

            if (hit.collider != null)
            {
                return hit.collider.transform == target;
            }

            else return false;
        }

        protected void UpdateVelocity(Vector2 direction, float speed)
        {

            if(Slow > 0)
            {
                speed -= Slow * speed;
            }

            Rigidbody.velocity = direction * speed;
        }

        protected void RemoveVelocity()
        {
            Rigidbody.velocity = Vector2.zero;
        }

        private bool GroundCheck()
        {
            return ColliderCheck(GroundSensor, groundFilter, out _);
        }

        private bool LedgeCheck()
        {
            return !ColliderCheck(LedgeSensor, groundFilter, out _);
        }

        private bool WallCheck()
        {
            return ColliderCheck(WallSensor, groundFilter, out _);
        }

        private bool NearbyProximityCheck(out Collider2D hitCollider)
        {
            return ColliderCheck(RigidbodyCollider, targetFilter, out hitCollider);
        }

        private bool PatrolProximityCheck(out Collider2D hitCollider)
        {
            return ColliderCheck(PatrolSensor, targetFilter, out hitCollider);
        }

        private bool ChaseProximityCheck(out Collider2D hitCollider)
        {
            return ColliderCheck(ChaseSensor, targetFilter, out hitCollider);
        }

        private bool SweepProximityCheck(out Collider2D hitCollider)
        {
            return ColliderCheck(SweepSensor, targetFilter, out hitCollider);
        }

        private bool AttackProximityCheck(out Collider2D hitCollider)
        {
            return ColliderCheck(AttackSensor, targetFilter, out hitCollider);
        }

        private bool SightCheck(Transform target)
        {
            return RaycastTransformCheck(SightPivot.position, target, sightOcclusionMask);
        }

        private bool IsPointInFront(Vector2 point)
        {
            Vector2 direction = GetDirection(transform.position, point).normalized;

            if ((direction.x > 0 && HorizontalFacing > 0)
                || direction.x < 0 && HorizontalFacing < 0)
            {
                return true;
            }

            else return false;
        }

        private void RotateAttackPivotTowards(Vector2 targetPoint)
        {
            Vector2 direction = GetDirection(AttackPivot.position, targetPoint) * HorizontalFacing;

            const float angle = 75;

            if (direction != Vector2.zero)
            {
                float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                float clampedAngle = Mathf.Clamp(targetAngle, -angle, angle);
                Quaternion targetRotation = Quaternion.Euler(0, 0, clampedAngle);
                AttackPivot.rotation = targetRotation;
            }
        }

        private void ResetAttackPivot()
        {
            RotateAttackPivotTowards(new Vector2(HorizontalFacing, 0) + (Vector2)AttackPivot.position);
        }

        private Vector2 GetDirection(Vector2 startPoint, Vector2 endPoint)
        {
            return endPoint - startPoint;
        }

        private float GetDistance(Vector2 pointA, Vector2 pointB)
        {
            return Vector2.Distance(pointA, pointB);
        }

        private void OnAttackComplete()
        {
            Debug.Log("Attack Completed");
            AttackParticles.Play();
        }

        private float GetClipLength(string clipName)
        {
            RuntimeAnimatorController ac = Animator.runtimeAnimatorController;
            foreach (AnimationClip clip in ac.animationClips)
            {
                if (clip.name == clipName)
                {
                    return clip.length;
                }
            }
            return 0f;
        }

        private void OnDrawGizmos()
        {
            if (gizmoTarget != Vector3.zero)
            {
                Gizmos.color = UnityEngine.Color.red;
                Gizmos.DrawLine(SightPivot.position, gizmoTarget);
                Gizmos.DrawSphere(gizmoTarget, 0.2f);
            }
        }

        public void FireProjectile(ProjectileSO projectileSO)
        {
            Projectile projectile = Instantiate(PrefabManager.Instance.Projectile);
            Vector2 dir = AttackPivot.right * HorizontalFacing;
            projectile.Initialize(projectileSO, AttackPivot.transform.position, dir);
        }

        public void DamageHealth(float damageAmount)
        {
            Debug.Log("Enemy Hit");
        }

        public void HealHealth(float healAmount)
        {
            throw new NotImplementedException();
        }

        public void ApplyEffect(EffectSO effectSO)
        {
            DamageHealth(effectSO.Damage);

            if(effectSO.SlowData.Enabled)
            {
                currentSlow = new(effectSO.SlowData, Time.time + effectSO.SlowData.Duration);
            }

            if(effectSO.StunData.Enabled)
            {
                currentStun = new(effectSO.StunData, Time.time + effectSO.StunData.Duration);
            }
        }

        public void ApplyForce(ForceSO forceSO)
        {
            float new_x = HorizontalFacing * forceSO.Direction.x;
            Vector2 velocity = new Vector2(new_x, forceSO.Direction.y).normalized * forceSO.Speed;
            Rigidbody.AddForce(velocity, forceSO.ForceMode);
        }
    }
}