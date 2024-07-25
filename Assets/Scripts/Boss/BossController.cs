using Effect;
using Enemy;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Boss
{
    public enum BossState
    {
        Idle,
        Attacking,
        Dying,
    }

    public class BossController : MonoBehaviour, IEffectable
    {

        public Rigidbody2D Rigidbody { get; private set; }
        public Collider2D RigidbodyCollider { get; private set; }
        public Animator Animator { get; private set; }

        [field: Space]
        [field: SerializeField] public BossData BossData { get; private set; }


        [field: Space]
        [field: Header("Boss Pivots")]
        [field: SerializeField] public Transform AttackPivot { get; private set; }
        [field: SerializeField] public Transform FirePoint { get; private set; }
        [field: SerializeField] public Transform SightPivot { get; private set; }

        [field: Space]
        [field: Header("Boss Sensors")]

        [field: SerializeField] public Collider2D GroundSensor { get; private set; }
        [field: SerializeField] public Collider2D AttackSensor { get; private set; }

        [field: Space]
        [field: Header("Attack Components")]
        [field: SerializeField] public ParticleSystem AttackParticles { get; private set; }

        public BossState BossState { get; private set; } = BossState.Idle;
        public int HorizontalFacing { get; private set; } = 1;


        private ContactFilter2D groundFilter;
        private ContactFilter2D targetFilter;

        private readonly Collider2D[] sensorResults = new Collider2D[50];
        private float currentAttackCooldown = 0;
        private float attackAnimationLength = 0;
        private float currentAttackAnimationTime = 0;

        private int currentAttackPattern = -1;
        private int currentAttackIndex = -1;
        private int lastAttackPattern = -1;

        public string[][] AttackPatterns = new string[][]
        {
            new string[] { "Stab", "Slash", "Charge" },
            new string[] { "Slash", "Fissure", "Charge" },
            new string[] { "BattleShout", "Slash", "Stab" },
            new string[] { "LeapSlam", "Charge", "LeapSlam" },
            new string[] { "Battleshout", "Stomp", "Charge" },
            new string[] { "Fireball", "LeapSlam", "Stab" },
        };


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

                if (_currentHealth <= 0)
                {
                    //ChangeState(BossState.Dying);
                }

                //else if (_currentHealth > EnemyData.DefaultHealth)
                //{
                //    _currentHealth = EnemyData.DefaultHealth;
                //}
            }
        }

        //debug
        private Vector3 gizmoTarget;

        protected void Awake()
        {
            Rigidbody = GetComponent<Rigidbody2D>();
            RigidbodyCollider = GetComponent<Collider2D>();
            Animator = GetComponent<Animator>();

            targetFilter = new ContactFilter2D
            {
                useTriggers = true
            };

            groundFilter.SetLayerMask(LayerMask.GetMask("Ground"));
            targetFilter.SetLayerMask(LayerMask.GetMask("Player"));


            //attackAnimationLength = GetClipLength("Attack");
        }

        protected virtual void Start()
        {
            //CurrentHealth = EnemyData.DefaultHealth;
        }

        private void FixedUpdate()
        {
            if (!GroundCheck())
                return;

            switch (BossState)
            {
                case BossState.Idle:
                    break;
                case BossState.Attacking:
                    break;
                case BossState.Dying:
                    break;
            }
        }

        private void ChangeState(BossState state)
        {
            switch (state)
            {
                case BossState.Idle:
                    break;
                case BossState.Attacking:
                    break;
                case BossState.Dying:
                    break;
            }

            BossState = state;
        }

        private void Attack()
        {
            if (currentAttackPattern < 0 || currentAttackPattern - 1 > AttackPatterns.Length)
            {
                int randomNumber;

                do
                {
                    randomNumber = UnityEngine.Random.Range(0, AttackPatterns.Length - 1);
                }
                while (lastAttackPattern != randomNumber);
            }

            else
            {
                currentAttackIndex++;
                if (currentAttackIndex > AttackPatterns[currentAttackPattern].Length)
                {
                    lastAttackPattern = currentAttackPattern;
                    currentAttackPattern = -1;
                    return;
                }

                else
                {
                    Animator.SetTrigger(AttackPatterns[currentAttackPattern][currentAttackIndex]);
                }
            }
        }

        private bool GroundCheck()
        {
            return ColliderCheck(GroundSensor, groundFilter, out _);
        }

        private bool AttackProximityCheck(out Collider2D hitCollider)
        {
            return ColliderCheck(AttackSensor, targetFilter, out hitCollider);
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

        public void ApplyEffect(EffectSO effectSO)
        {
            throw new System.NotImplementedException();
        }


    }
}