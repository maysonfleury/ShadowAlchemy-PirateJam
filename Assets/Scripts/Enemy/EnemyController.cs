using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

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
        [field: SerializeField] public ParticleSystem AttackParticles { get; private set; }

        [field: Space]
        [field: Header("Enemy Pivots")]
        [field: SerializeField] public Transform AttackPivot { get; private set; }
        [field: SerializeField] public Transform SightPivot { get; private set; }

        [field: Space]
        [field: Header("Enemy Sensors")]
        [field: SerializeField] public Collider2D PatrolSensor { get; private set; }
        [field: SerializeField] public Collider2D ChaseSensor { get; private set; }
        [field: SerializeField] public Collider2D AttackSensor { get; private set; }
        [field: SerializeField] public Collider2D LedgeSensor { get; private set; }
        [field: SerializeField] public Collider2D GroundSensor { get; private set; }
        [field: SerializeField] public LayerMask[] SightOcclusionMasks { get; private set; }

        public EnemyState EnemyState { get; private set; } = EnemyState.Inactive;
        public float EnemySpeed { get; private set; } = 1.0f;
        public int HorizontalFacing { get; private set; } = 1;


        private ContactFilter2D groundFilter;
        private ContactFilter2D targetFilter;
  
        private readonly Collider2D[] sensorResults = new Collider2D[50];
        private float currentStateDuration = 0.0f;
        private LayerMask sightOcclusionMask;
        private float attackLength = 1;
        private float currentAttackCooldown = 0;
        private float currentFlipCooldown = 0;

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
            attackLength = GetClipLength("Attack");
        }

        protected void FixedUpdate()
        {
            switch (EnemyState)
            {
                case EnemyState.Patrolling:
                    if (PatrolProximityCheck(out Collider2D collider)
                        && SightCheck(collider.transform))
                    {
                        ChangeState(EnemyState.Chasing);
                        break;
                    }

                    else gizmoTarget = Vector3.zero;

                    if (GroundCheck())
                    {
                        if(LedgeCheck())
                        {
                            FlipCharacter();
                        }
                    }

                    UpdateVelocity(new Vector2(HorizontalFacing, 0), EnemyData.PatrolSpeed);
                    break;

                case EnemyState.Chasing:
                    if (ChaseProximityCheck(out collider)
                        && SightCheck(collider.transform))
                    {
                        //float distance = GetDistance(collider.transform.position, transform.position);
                        //if (distance < EnemyData.AttackRange)
                        //{
                        //    ChangeState(EnemyState.Attacking);
                        //    break;
                        //}

                        if (AttackProximityCheck(out _))
                        {
                            currentFlipCooldown = 0;
                            ChangeState(EnemyState.Attacking);
                            return;
                        }

                        else if (GroundCheck())
                        {

                            if (LedgeCheck())
                            {
                                if (IsPointInFront(collider.transform.position))
                                {
                                    ChangeState(EnemyState.Watching);
                                    return;
                                }

                                else
                                {
                                    FlipCharacter();
                                    return;
                                }
                            }

                            else if(!IsPointInFront(collider.transform.position)
                                && currentFlipCooldown < Time.time)
                            {
                                FlipCharacter();
                                return;
                            }
                        }

                        UpdateVelocity(new Vector2(HorizontalFacing, 0), EnemyData.ChaseSpeed);
                    }

                    else
                    {
                        gizmoTarget = Vector3.zero;
                        ChangeState(EnemyState.Sweeping);
                        return;
                    }

                    break;

                case EnemyState.Sweeping:
                    if (ChaseProximityCheck(out collider)
                        && SightCheck(collider.transform))
                    {
                        ChangeState(EnemyState.Chasing);
                        return;
                    }

                    else if (currentStateDuration < Time.time)
                    {
                        ChangeState(EnemyState.Patrolling);
                        return;
                    }

                    else if (GroundCheck())
                    {

                        if (LedgeCheck())
                        {
                            FlipCharacter();
                        }
                    }

                    UpdateVelocity(new Vector2(HorizontalFacing, 0), EnemyData.SweepSpeed);

                    break;

                case EnemyState.Attacking:
                    if (currentAttackCooldown < Time.time)
                    {
                        OnAttackComplete();
                        ChangeState(EnemyState.Chasing);
                        return;
                    }

                    break;
                case EnemyState.Watching:
                    if (ChaseProximityCheck(out collider)
                        && SightCheck(collider.transform))
                    {
                        if (!IsPointInFront(collider.transform.position))
                        {
                            ChangeState(EnemyState.Chasing);
                            return;
                        }
                    }

                    else
                    {
                        ChangeState(EnemyState.Sweeping);
                    }

                    break;

                default: //EnemyState.Inactive:
                    break;
            }
        }

        private void ChangeState(EnemyState state)
        {
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
                    currentAttackCooldown = attackLength + Time.time;
                    UpdateVelocity(new Vector3(HorizontalFacing, 0), 0);
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



        protected void FlipCharacter()
        {
            Vector3 localScale = transform.localScale;
            localScale.x *= -1;
            transform.localScale = localScale;

            HorizontalFacing = -HorizontalFacing;
            //Debug.Log($"Flipping horizontal direction to: {HorizontalFacing}");
            currentFlipCooldown = EnemyData.FlipCooldown + Time.time;

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

        private bool GroundCheck()
        {
            return ColliderCheck(GroundSensor, groundFilter, out _);
        }

        private bool LedgeCheck()
        {
            return !ColliderCheck(LedgeSensor, groundFilter, out _);
        }

        private bool PatrolProximityCheck(out Collider2D hitCollider)
        {
            return ColliderCheck(PatrolSensor, targetFilter, out hitCollider);
        }

        private bool ChaseProximityCheck(out Collider2D hitCollider)
        {
            return ColliderCheck(ChaseSensor, targetFilter, out hitCollider);
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
    }
}