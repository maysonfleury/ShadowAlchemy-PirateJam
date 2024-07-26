using Attacks;
using Effect;
using Projectiles;
using System;
using UnityEngine;

namespace Enemy
{
    public enum EnemyState
    {
        Inactive,
        Sleeping,
        Patrolling,
        Evading,
        Chasing,
        Sweeping,
        Watching,
        Attacking,
        Dying,
    }

    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class EnemyController : MonoBehaviour, IEffectable, IMovable, IPossessable
    {

        public Rigidbody2D Rigidbody { get; private set; }
        public Collider2D RigidbodyCollider { get; private set; }
        public Animator Animator { get; private set; }

        [field: Space]
        [field: SerializeField] public EnemyData EnemyData { get; private set; }


        [field: Space]
        [field: Header("Enemy Pivots")]
        [field: SerializeField] public Transform FirePivot { get; private set; }
        [field: SerializeField] public Transform FirePoint { get; private set; }
        [field: SerializeField] public Transform SightPivot { get; private set; }

        [field: Space]
        [field: Header("Enemy Sensors")]
        //[field: SerializeField] public Collider2D NearbySensor { get; private set; }
        [field: SerializeField] public Collider2D PatrolSensor { get; private set; }
        [field: SerializeField] public Collider2D AlertSensor { get; private set; }
        [field: SerializeField] public Collider2D SweepSensor { get; private set; }
        [field: SerializeField] public Collider2D AttackSensor { get; private set; }
        [field: SerializeField] public Collider2D LedgeSensor { get; private set; }
        [field: SerializeField] public Collider2D WallSensor { get; private set; }
        [field: SerializeField] public Collider2D GroundSensor { get; private set; }
        [field: SerializeField] public LayerMask[] SightOcclusionMasks { get; private set; }

        [field: Space]
        [field: Header("Attack Components")]
        [field: SerializeField] public Collider2D AttackBox { get; private set; }
        [field: SerializeField] public ParticleSystem AttackParticles { get; private set; }

        public EnemyState EnemyState { get; private set; } = EnemyState.Patrolling;
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

        const float stabilizeTime = 0.5f;
        private float currentStabilizingTime = 0;

        private bool attackBoxEnabled = false;
        private AttackSO currentAttackSO = null;

        private (Slow SlowData, float Duration) currentSlow = (null, 0f);
        private (Stun StunData, float Duration) currentStun = (null, 0f);

        int _currentHealth = 0;
        public int CurrentHealth
        {
            get
            {
                return _currentHealth;
            }

            private set
            {
                _currentHealth = value;

                if(_currentHealth <= 0)
                {
                    ChangeState(EnemyState.Dying);
                }

                else if(_currentHealth > EnemyData.DefaultHealth)
                {
                    _currentHealth = EnemyData.DefaultHealth;
                }
            }
        }

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

        protected void Awake()
        {
            Rigidbody = GetComponent<Rigidbody2D>();
            RigidbodyCollider = GetComponent<Collider2D>();
            Animator = GetComponent<Animator>();

            ChangeState(EnemyState.Patrolling);

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

        protected virtual void Start()
        {
            CurrentHealth = EnemyData.DefaultHealth;
        }

        protected void FixedUpdate()
        {
            Collider2D target;

            switch (EnemyState)
            {
                case EnemyState.Sleeping:
                    if(!IsStabilized())
                    {
                        return;
                    }

                    if(currentStateDuration < Time.time)
                    {
                        ChangeState(EnemyState.Patrolling);
                    }

                    else if (NearbyProximityCheck(out target)
                    && SightCheck(target.transform))
                    {
                        ChangeState(EnemyState.Patrolling);
                    }

                    return;

                case EnemyState.Patrolling:
                    if (!IsStabilized())
                    {
                        return;
                    }

                    if (EnemyData.HasSleepState 
                        && currentStateDuration < Time.time)
                    {
                        ChangeState(EnemyState.Sleeping);
                    }

                    if (PatrolProximityCheck(out target)
                        && SightCheck(target.transform))
                    {
                        if (EnemyData.EnemyBehaviour == EnemyBehaviourType.Aggressive)
                        {
                            ChangeState(EnemyState.Chasing);
                        }

                        else if(EnemyData.EnemyBehaviour == EnemyBehaviourType.Evasive)
                        {
                            FlipCharacter();
                            ChangeState(EnemyState.Evading);
                        }
                    }

                    else if(LedgeCheck() || WallCheck())
                    {

                        PrepareFlipCharacter(EnemyData.PatrolFlipTime);
                    }

                    else
                    {
                        UpdateVelocity(new(HorizontalFacing, 0), EnemyData.PatrolSpeed);                       
                    }

                    return;

                case EnemyState.Evading:
                    if (!IsStabilized())
                    {
                        return;
                    }

                    if (AlertedProximityCheck(out target)
                       && SightCheck(target.transform))
                    {
                        currentStateDuration = EnemyData.EvasiveDuration + Time.time;

                    }

                    else if (currentStateDuration < Time.time)
                    {
                        ChangeState(EnemyState.Patrolling);
                    }

                    if (LedgeCheck() || WallCheck())
                    {
                        PrepareFlipCharacter(EnemyData.PatrolFlipTime);
                    }

                    else
                    {
                        UpdateVelocity(new(HorizontalFacing, 0), EnemyData.EvasiveSpeed);
                    }


                    return;

                case EnemyState.Chasing:
                    if (!IsStabilized())
                    {
                        return;
                    }

                    if (currentAttackCooldown >= Time.time)
                    {
                        return;
                    }

                    else if (AlertedProximityCheck(out target)
                        && SightCheck(target.transform))
                    {
                        bool targetIsInFront = IsPointInFront(target.transform.position);

                        if(targetIsInFront)
                        {
                            RotateAttackPivotTowards(target.transform.position);
                        }

                        if (AttackProximityCheck(out _) 
                            && currentAttackCooldown < Time.time)
                        {
                            ChangeState(EnemyState.Attacking);
                        }

                        else
                        {

                            if (LedgeCheck() || WallCheck())
                            {
                                if (EnemyData.HasWatchState && targetIsInFront)
                                {
                                    ChangeState(EnemyState.Watching);
                                }

                                else
                                {
                                    PrepareFlipCharacter(EnemyData.ChaseFlipTime);
                                }
                            }

                            else if(!targetIsInFront)
                            {
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
                        ChangeState(EnemyState.Sweeping);
                    }

                    return;

                case EnemyState.Sweeping:
                    if (!IsStabilized())
                    {
                        return;
                    }

                    if (SweepProximityCheck(out target)
                        && SightCheck(target.transform))
                    {
                        ChangeState(EnemyState.Chasing);
                    }

                    else if (currentStateDuration < Time.time)
                    {
                        ChangeState(EnemyState.Patrolling);
                    }

                    else if (LedgeCheck() || WallCheck())
                    {
                        PrepareFlipCharacter(EnemyData.SweepFlipTime);
                    }

                    else
                    {
                        UpdateVelocity(new(HorizontalFacing, 0), EnemyData.SweepSpeed);
                    }

                    return;

                case EnemyState.Attacking:
                    if (attackBoxEnabled
                        && AttackHitboxProximityCheck(out target)
                        && SightCheck(target.transform))
                    {
                        currentAttackSO.ApplyAttack(HorizontalFacing, target.transform);
                    }

                    if (!IsStabilized())
                    {
                        return;
                    }

                    if (currentAttackAnimationTime < Time.time)
                    {
                        OnAttackComplete();
                        ChangeState(EnemyState.Chasing);
                    }

                    return;
                case EnemyState.Watching:
                    if (!IsStabilized())
                    {
                        return;
                    }

                    if (AlertedProximityCheck(out target)
                        && SightCheck(target.transform))
                    {

                        if (AttackProximityCheck(out _) 
                            && currentAttackCooldown < Time.time)
                        {
                            ChangeState(EnemyState.Attacking);
                        }

                        else if (!IsPointInFront(target.transform.position))
                        {
                            ChangeState(EnemyState.Chasing);
                        }
                    }

                    else
                    {
                        ChangeState(EnemyState.Sweeping);
                    }
                    return;

                case EnemyState.Dying:
                    if (currentStateDuration < Time.time)
                    {
                        KillEnemy();
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
                    RemoveVelocity();
                    currentStateDuration = EnemyData.SleepDuration + Time.time;
                    break;

                case EnemyState.Patrolling:
                    ResetAttackPivot();
                    currentStateDuration = EnemyData.PatrolDuration + Time.time;
                    break;

                case EnemyState.Evading:
                    ResetAttackPivot();
                    currentStateDuration = EnemyData.EvasiveDuration + Time.time;
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

                case EnemyState.Dying:
                    if (EnemyState == EnemyState.Dying)
                    {
                        return;
                    }
                    currentStateDuration = EnemyData.DyingDuration + Time.time;
                    break;
                default: //EnemyState.Inactive:
                    break;
            }

            EnemyState = state;
        }


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
            //Rigidbody.AddForce(direction * (speed - Rigidbody.velocity.magnitude));
        }

        protected void RemoveVelocity()
        {
            Rigidbody.velocity = Vector2.zero;
        }

        private bool GroundCheck()
        {
            if(!EnemyData.IsGrounded)
            {
                return true;
            }

            return ColliderCheck(GroundSensor, groundFilter, out _);
        }

        private bool LedgeCheck()
        {
            if (!EnemyData.IsGrounded)
            {
                return false;
            }

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

        private bool AlertedProximityCheck(out Collider2D hitCollider)
        {
            return ColliderCheck(AlertSensor, targetFilter, out hitCollider);
        }

        private bool SweepProximityCheck(out Collider2D hitCollider)
        {
            return ColliderCheck(SweepSensor, targetFilter, out hitCollider);
        }

        private bool AttackProximityCheck(out Collider2D hitCollider)
        {
            return ColliderCheck(AttackSensor, targetFilter, out hitCollider);
        }

        private bool AttackHitboxProximityCheck(out Collider2D hitCollider)
        {
            return ColliderCheck(AttackBox, targetFilter, out hitCollider);
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
            Vector2 direction = GetDirection(FirePivot.position, targetPoint) * HorizontalFacing;

            const float angle = 75;

            if (direction != Vector2.zero)
            {
                float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                float clampedAngle = Mathf.Clamp(targetAngle, -angle, angle);
                Quaternion targetRotation = Quaternion.Euler(0, 0, clampedAngle);
                FirePivot.rotation = targetRotation;
            }
        }

        private void ResetAttackPivot()
        {
            RotateAttackPivotTowards(new Vector2(HorizontalFacing, 0) + (Vector2)FirePivot.position);
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
            DisableAttackBox();
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

        private void EnableAttackBox(AttackSO attackSO)
        {
            attackBoxEnabled = true;
            currentAttackSO = attackSO;
        }

        private void DisableAttackBox()
        {
            attackBoxEnabled = false;
            currentAttackSO = null;
        }

        public void FireProjectile(ProjectileSO projectileSO)
        {
            Vector2 dir = FirePivot.right * HorizontalFacing;
            ProjectileManager.Instance.Request(projectileSO, FirePoint.transform.position, dir);
        }

        public void DamageHealth(int damageAmount)
        {
            Debug.Log("Enemy Hit");
            CurrentHealth -= damageAmount;
        }

        public void HealHealth(float healAmount)
        {
            throw new NotImplementedException();
        }

        public void ApplyEffect(EffectSO effectSO)
        {
            if (effectSO == null)
                return;

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

        private bool IsStabilized()
        {
            if (!GroundCheck() || Stunned)
            {
                currentStabilizingTime = stabilizeTime + Time.time;
                return false;
            }

            else if (currentStabilizingTime > Time.time)
            {
                return false;
            }

            return true;
        }

        public void ApplyRelativeForce(float forward, ForceSO forceSO)
        {
            if(forward == 0)
            {
                forward = 1;
            }

            float new_x = forward * forceSO.Direction.x;
            Vector2 velocity = new Vector2(new_x, forceSO.Direction.y).normalized * forceSO.Speed;
            Rigidbody.AddForce(velocity, forceSO.ForceMode);
        }

        private void KillEnemy()
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }


        public bool TryPossession(out PossessionType type)
        {
            type = PossessionType.None;

            if (EnemyState == EnemyState.Dying 
                || EnemyState == EnemyState.Sleeping)
            {
                type = EnemyData.PossessionType;
                KillEnemy();
                return true;
            }

            else return false;
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

    }
}