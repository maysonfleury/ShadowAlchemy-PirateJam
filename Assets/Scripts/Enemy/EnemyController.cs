using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Hardware;
using UnityEngine;
using static Unity.VisualScripting.Member;

namespace Enemy
{
    public enum EnemyState
    {
        Inactive,
        Patrolling,
        Chasing,
        Sweeping,
        Watching,
        Attacking,
    }

    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class EnemyController : MonoBehaviour
    {

        public Rigidbody2D Rigidbody2D { get; private set; }
        public Collider2D RigidbodyCollider { get; private set; }

        [field: Space]
        [field: SerializeField] public EnemyData EnemyData { get; private set; }

        [field: Space]
        [field: Header("Enemy Components")]
        [field: SerializeField] public Animator Animator { get; private set; }

        [field: Space]
        [field: Header("Enemy Pivots")]
        [field: SerializeField] public Transform AttackPivot { get; private set; }
        [field: SerializeField] public Transform SightPivot { get; private set; }

        [field: Space]
        [field: Header("Enemy Sensors")]
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
        public float EnemySpeed { get; private set; } = 1.0f;
        public int HorizontalFacing { get; private set; } = 1;


        private ContactFilter2D groundFilter;
        private ContactFilter2D targetFilter;
  
        private readonly Collider2D[] sensorResults = new Collider2D[50];
        private float currentStateDuration = 0.0f;
        private LayerMask sightOcclusionMask;
        private float currentAttackCooldown = 0;
        private float attackAnimationLength = 0;
        private float currentAttackAnimationTime = 0;
        private float speedThisFrame = 0;
        private float currentFlipWaitTime = 0;
        private float currentFlipCooldown = 0;
        private bool isWaitingToFlip = false;

        //debug
        private Vector3 gizmoTarget;

        protected virtual void Start()
        {
            Rigidbody2D = GetComponent<Rigidbody2D>();
            RigidbodyCollider = GetComponent<Collider2D>();

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
            if(!GroundCheck())
                return;

            switch (EnemyState)
            {
                case EnemyState.Patrolling:
                    if (PatrolProximityCheck(out Collider2D collider)
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
                    if (ChaseProximityCheck(out collider)
                        && SightCheck(collider.transform))
                    {

                        if (CanAttack(out _))
                        {
                            RemoveVelocity();
                            ChangeState(EnemyState.Attacking);
                        }

                        else
                        {

                            if (LedgeCheck() || WallCheck())
                            {
                                if (EnemyData.WatchStateEnabled && IsPointInFront(collider.transform.position))
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

                            else if(!IsPointInFront(collider.transform.position))
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
                    if (ChaseProximityCheck(out collider)
                        && SightCheck(collider.transform))
                    {

                        if (CanAttack(out _))
                        {
                            ChangeState(EnemyState.Attacking);
                        }

                        else if (!IsPointInFront(collider.transform.position))
                        {
                            ChangeState(EnemyState.Chasing);
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
                case EnemyState.Patrolling:
                    break;

                case EnemyState.Chasing:
                    break;
                case EnemyState.Sweeping:
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
            Rigidbody2D.velocity = direction * speed;
        }

        protected void RemoveVelocity()
        {
            Rigidbody2D.velocity = Vector2.zero;
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

        private bool CanAttack(out Collider2D hitCollider)
        {
            hitCollider = null;

            if (currentAttackCooldown > Time.time)
            {
                return false;
            }

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

        private Vector2 GetDirection(Vector2 pointA, Vector2 pointB)
        {
            return pointB - pointA;
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
                Gizmos.color = Color.red;
                Gizmos.DrawLine(SightPivot.position, gizmoTarget);

                Gizmos.DrawSphere(gizmoTarget, 0.2f);
            }
        }

        public void ApplyForce(ForceSO forceSO)
        {
            float new_x = HorizontalFacing * forceSO.Direction.x;
            Vector2 velocity = new Vector2(new_x, forceSO.Direction.y).normalized * forceSO.Speed;
            Rigidbody2D.AddForce(velocity, forceSO.ForceMode);
        }
    }
}