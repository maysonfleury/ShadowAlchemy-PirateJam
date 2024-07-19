using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Enemy
{
    public enum EnemyState
    {
        Inactive,
        Patrolling,
        Chasing,
    }

    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class EnemyController : MonoBehaviour
    {

        public Rigidbody2D Rigidbody2D { get; private set; }
        public Collider2D RigidbodyCollider { get; private set; }

        [field: Space]
        [field: Header("Enemy Components")]
        [field: SerializeField] public EnemyData EnemyData { get; private set; }

        [field: Space]
        [field: Header("Detection Components")]
        [field: SerializeField] public Transform SightPivot { get; private set; }
        [field: SerializeField] public Collider2D PlayerSensor { get; private set; }
        [field: SerializeField] public Collider2D LedgeSensor { get; private set; }
        [field: SerializeField] public LayerMask[] SightOcclusionMasks { get; private set; }

        private ContactFilter2D ledgeSensorFilter;
        private ContactFilter2D playerSensorFilter;
        private readonly Collider2D[] sensorResults = new Collider2D[50];

        public float EnemySpeed { get; private set; } = 1.0f;
        public int HorizontalFacing { get; private set; } = 1;

        private float currentChaseTime = 0.0f;
        private LayerMask sightOcclusionMask;

        public EnemyState EnemyState { get; private set; } = EnemyState.Inactive;

        //debug
        private Vector3 gizmoTarget;

        protected virtual void Start()
        {
            Rigidbody2D = GetComponent<Rigidbody2D>();
            RigidbodyCollider = GetComponent<Collider2D>();

            EnemyState = EnemyState.Patrolling;

            ledgeSensorFilter = new ContactFilter2D
            {
                useTriggers = true
            };

            playerSensorFilter = new ContactFilter2D
            {
                useTriggers = true
            };

            ledgeSensorFilter.SetLayerMask(LayerMask.GetMask("Ground"));
            playerSensorFilter.SetLayerMask(LayerMask.GetMask("Player"));

            sightOcclusionMask = LayerUtility.CombineMasks(SightOcclusionMasks);
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

                    if (!LedgeCheck(out _))
                    {                    
                        FlipCharacter();
                    }

                    UpdateVelocity(new Vector2(HorizontalFacing, 0), EnemyData.PatrolSpeed);
                    break;

                case EnemyState.Chasing:
                    if (PlayerProximityCheck(out collider)
                        && PlayerSightCheck(collider.transform))
                    {
                        currentChaseTime = EnemyData.ChaseDuration + Time.time;
                    }

                    else if(currentChaseTime < Time.time)
                    {
                        ChangeState(EnemyState.Patrolling);
                        break;
                    }

                    else gizmoTarget = Vector3.zero;

                    if (!LedgeCheck(out _))
                    {
                        FlipCharacter();
                    }

                    UpdateVelocity(new Vector2(HorizontalFacing, 0), EnemyData.ChaseSpeed);
                    break;
                default: //EnemyState.Inactive:
                    break;
            }
        }

        protected void UpdateVelocity(Vector2 direction, float speed)
        {
            Rigidbody2D.velocity = direction * speed;
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

        private bool LedgeCheck(out Collider2D hitCollider)
        {
            return ColliderCheck(LedgeSensor, ledgeSensorFilter, out hitCollider);
        }

        private bool PlayerProximityCheck(out Collider2D hitCollider)
        {
            return ColliderCheck(PlayerSensor, playerSensorFilter, out hitCollider);
        }

        private bool PlayerSightCheck(Transform target)
        {
            return SightCheck(SightPivot.position, target, sightOcclusionMask);
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
                default: //EnemyState.Inactive:
                    break;
            }

            EnemyState = state;
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