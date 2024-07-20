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
        [field: SerializeField] public Collider2D PlayerSensor { get; private set; }
        [field: SerializeField] public Collider2D LedgeSensor { get; private set; }
        [field: SerializeField] public Collider2D GroundSensor { get; private set; }
        [field: SerializeField] public LayerMask[] SightOcclusionMasks { get; private set; }

        public EnemyState EnemyState { get; private set; } = EnemyState.Inactive;
        public float EnemySpeed { get; private set; } = 1.0f;
        public int HorizontalFacing { get; private set; } = 1;


        private ContactFilter2D groundSensorFilter;
        private ContactFilter2D playerSensorFilter;
        private readonly Collider2D[] sensorResults = new Collider2D[50];
        private float currentChaseTime = 0.0f;
        private LayerMask sightOcclusionMask;
        private float attackLength = 1;
        private float currentAttackDuration = 0;

        //debug
        private Vector3 gizmoTarget;

        protected virtual void Start()
        {
            Rigidbody2D = GetComponent<Rigidbody2D>();
            RigidbodyCollider = GetComponent<Collider2D>();

            EnemyState = EnemyState.Patrolling;

            groundSensorFilter = new ContactFilter2D
            {
                useTriggers = true
            };

            playerSensorFilter = new ContactFilter2D
            {
                useTriggers = true
            };

            groundSensorFilter.SetLayerMask(LayerMask.GetMask("Ground"));
            playerSensorFilter.SetLayerMask(LayerMask.GetMask("Player"));

            sightOcclusionMask = LayerUtility.CombineMasks(SightOcclusionMasks);
            attackLength = GetClipLength("Attack");
        }

        protected void FixedUpdate()
        {
            switch (EnemyState)
            {
                case EnemyState.Patrolling:
                    if (PlayerProximityCheck(out Collider2D collider)
                        && PlayerSightCheck(collider.transform))
                    {
                        ChangeState(EnemyState.Chasing);
                        break;
                    }

                    else gizmoTarget = Vector3.zero;

                    if (GroundCheck(out _) && LedgeCheck(out _))
                    {
                        FlipCharacter();
                    }

                    UpdateVelocity(new Vector2(HorizontalFacing, 0), EnemyData.PatrolSpeed);
                    break;

                case EnemyState.Chasing:
                    if (PlayerProximityCheck(out collider)
                        && PlayerSightCheck(collider.transform))
                    {
                        float distance = GetDistance(collider.transform.position, transform.position);
                        if (distance < EnemyData.AttackRange)
                        {
                            ChangeState(EnemyState.Attacking);
                            break;
                        }

                        else currentChaseTime = EnemyData.ChaseDuration + Time.time;
                    }

                    else if (currentChaseTime < Time.time)
                    {
                        ChangeState(EnemyState.Patrolling);
                        break;
                    }

                    else gizmoTarget = Vector3.zero;

                    if (GroundCheck(out _) && LedgeCheck(out _))
                    {
                        FlipCharacter();
                    }

                    UpdateVelocity(new Vector2(HorizontalFacing, 0), EnemyData.ChaseSpeed);
                    break;

                case EnemyState.Attacking:
                    if (currentAttackDuration < Time.time)
                    {
                        OnAttackComplete();
                        ChangeState(EnemyState.Chasing);
                    }

                    break;

                    default: //EnemyState.Inactive:
                    break;
            }
        }

        private void ChangeState(EnemyState state)
        {
            switch (state)
            {
                case EnemyState.Patrolling:
                    break;

                case EnemyState.Chasing:
                    currentChaseTime = EnemyData.ChaseDuration + Time.time;
                    break;
                case EnemyState.Attacking:
                    currentAttackDuration = attackLength + Time.time;
                    UpdateVelocity(new Vector3(HorizontalFacing, 0), 0);
                    Animator.SetTrigger("Attack");
                    Debug.Log("Attacking");
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
            Debug.Log($"Flipping horizontal direction to: {HorizontalFacing}");
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

        private bool SightCheck(Vector2 startPoint, Transform target, LayerMask mask)
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

        private bool GroundCheck(out Collider2D hitCollider)
        {
            return ColliderCheck(GroundSensor, groundSensorFilter, out hitCollider);
        }

        private bool LedgeCheck(out Collider2D hitCollider)
        {
            return !ColliderCheck(LedgeSensor, groundSensorFilter, out hitCollider);
        }

        private bool PlayerProximityCheck(out Collider2D hitCollider)
        {
            return ColliderCheck(PlayerSensor, playerSensorFilter, out hitCollider);
        }

        private bool PlayerSightCheck(Transform target)
        {
            return SightCheck(SightPivot.position, target, sightOcclusionMask);
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