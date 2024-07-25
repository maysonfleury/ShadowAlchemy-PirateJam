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

    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class BossController : MonoBehaviour, IEffectable
    {

        public Rigidbody2D Rigidbody { get; private set; }
        public Collider2D RigidbodyCollider { get; private set; }
        public Animator Animator { get; private set; }

        [field: Space]
        [field: SerializeField] public BossData BossData { get; private set; }


        [field: Space]
        [field: Header("Boss Attack Points")]
        [field: SerializeField] public Transform FirePoint { get; private set; }
        [field: SerializeField] public Collider2D AttackBox { get; private set; }

        [field: Space]
        [field: Header("Boss Sensors")]

        [field: SerializeField] public Collider2D GroundSensor { get; private set; }
        [field: SerializeField] public Collider2D TargetSensor { get; private set; }

        [field: Space]
        [field: Header("Attack Components")]
        [field: SerializeField] public ParticleSystem AttackParticles { get; private set; }

        public BossState BossState { get; private set; } = BossState.Idle;
        public int HorizontalFacing { get; private set; } = 1;


        private ContactFilter2D groundFilter;
        private ContactFilter2D targetFilter;

        private readonly Collider2D[] sensorResults = new Collider2D[50];
        private float attackAnimationLength = 0;
        private float currentAttackAnimationTime = 0;

        private int currentAttackPattern = -1;
        private int currentAttackIndex = -1;

        const float stabilizeTime = 1;
        private float currentStabilizingTime = 0;

        const float attackCooldownTime = 1;
        private float currentAttackCooldown = 0;

        private bool attackBoxEnabled = false;
        private EffectSO currentAttackEffect = null;

        public string[][] AttackPatterns = new string[][]
        {
            new string[] { "Charge", "Charge", "Charge" },
            //new string[] { "Stab", "Slash", "Charge" },
            //new string[] { "Stab", "Slash", "Stab" },
            //new string[] { "Slash", "Fissure", "Charge" },
            //new string[] { "BattleShout", "Slash", "Stab" },
            //new string[] { "LeapSlam", "Charge", "LeapSlam" },
            //new string[] { "Battleshout", "Stomp", "Charge" },
            //new string[] { "Fireball", "LeapSlam", "Stab" },
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
            {
                currentStabilizingTime = stabilizeTime + Time.time;
                return;
            }

            else if(currentStabilizingTime > Time.time)
            {
                return;
            }

            switch (BossState)
            {
                case BossState.Idle:
                    if (TargetProximityCheck(out Collider2D target))
                    { 
                        if (!IsPointInFront(target.transform.position))
                        {
                            FlipCharacter();
                        }

                        if (currentAttackCooldown < Time.time)
                        {
                            ChangeState(BossState.Attacking);
                        }
                    }

                    break;
                case BossState.Attacking:
                    if(AttackProximityCheck(out target))
                    {
                        if(target.TryGetComponent(out IEffectable effectable))
                        {
                            effectable.ApplyEffect(currentAttackEffect);
                        }
                    }

                    if (currentAttackAnimationTime < Time.time)
                    {
                        OnAttackComplete();
                        ChangeState(BossState.Idle);
                    }

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
                    Attack();
                    break;
                case BossState.Dying:
                    break;
            }

            BossState = state;
        }

        private void Attack()
        {

            currentAttackIndex++;
            if ((currentAttackPattern < 0 || currentAttackPattern - 1 > AttackPatterns.Length)
                || currentAttackIndex > AttackPatterns[currentAttackPattern].Length - 1)
            {
                currentAttackPattern = UnityEngine.Random.Range(0, AttackPatterns.Length - 1);
                currentAttackIndex = 0;
            }

            attackAnimationLength = GetClipLength(AttackPatterns[currentAttackPattern][currentAttackIndex]);
            currentAttackAnimationTime = attackAnimationLength + BossData.AttackCooldown + Time.time;
            Animator.SetTrigger(AttackPatterns[currentAttackPattern][currentAttackIndex]);
        }

        private void OnAttackComplete()
        {
            DisableAttackBox();
        }

        private void EnableAttackBox(EffectSO effectSO)
        {
            attackBoxEnabled = true;
            currentAttackEffect = effectSO;
        }

        private void DisableAttackBox()
        {
            attackBoxEnabled = false;
            currentAttackEffect = null;
        }

        private bool GroundCheck()
        {
            return ColliderCheck(GroundSensor, groundFilter, out _);
        }

        private bool TargetProximityCheck(out Collider2D hitCollider)
        {
            return ColliderCheck(TargetSensor, targetFilter, out hitCollider);
        }

        private bool AttackProximityCheck(out Collider2D hitCollider)
        {
            return ColliderCheck(AttackBox, targetFilter, out hitCollider);
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

        private Vector2 GetDirection(Vector2 startPoint, Vector2 endPoint)
        {
            return endPoint - startPoint;
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

        protected void FlipCharacter()
        {
            Vector3 localScale = transform.localScale;
            localScale.x *= -1;
            transform.localScale = localScale;

            HorizontalFacing = -HorizontalFacing;
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

        public void ApplyEffect(EffectSO effectSO)
        {
            throw new System.NotImplementedException();
        }

        public void ApplyForce(ForceSO forceSO)
        {
            float new_x = HorizontalFacing * forceSO.Direction.x;
            Vector2 velocity = new Vector2(new_x, forceSO.Direction.y).normalized * forceSO.Speed;
            Rigidbody.AddForce(velocity, forceSO.ForceMode);
        }

        public void ApplyRelativeForce(float forward, ForceSO forceSO)
        {
            if (forward == 0)
            {
                forward = 1;
            }

            float new_x = forward * forceSO.Direction.x;
            Vector2 velocity = new Vector2(new_x, forceSO.Direction.y).normalized * forceSO.Speed;
            Rigidbody.AddForce(velocity, forceSO.ForceMode);
        }


    }
}