using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum PossessionType
{
    None = 0,
    SkeletonArcher = 1,
    SkeletonWarrior = 2,
    Spider = 3,
    Rat = 4,
    Bat = 5,
    Alchemist = 6,
}

namespace Enemy
{
    public enum EnemyBehaviourType
    {
        Aggressive = 0,
        Evasive = 1,
    }

    [Serializable]
    public class EnemyData
    {
        [field: SerializeField] public EnemyBehaviourType EnemyBehaviour { get; private set; }
        [field: SerializeField] public PossessionType PossessionType { get; private set; }
        [field: SerializeField, Min(1)] public int DefaultHealth { get; private set; } = 1;
        [field: SerializeField] public bool IsGrounded { get; private set; } = true;

        [field: Space]
        [field: Header("Sleep State")]
        [field: SerializeField] public bool HasSleepState { get; private set; } = false;
        [field: SerializeField] public float SleepDuration { get; private set; } = 5.0f;

        [field: Space]
        [field: Header("Patrol State")]
        [field: SerializeField] public float PatrolSpeed { get; private set; } = 5.0f;
        [field: SerializeField] public float PatrolDuration { get; private set; } = 5.0f;
        [field: SerializeField] public float PatrolFlipCooldown { get; private set; } = 1f;

        [field: Space]
        [field: Header("Evasive State")]
        [field: SerializeField] public float EvasiveSpeed { get; private set; } = 10.0f;
        [field: SerializeField] public float EvasiveDuration { get; private set; } = 5.0f;
        [field: SerializeField] public float EvasiveFlipCooldown { get; private set; } = 1f;

        [field: Space]
        [field: Header("Chase State")]
        [field: SerializeField] public float ChaseSpeed { get; private set; } = 10.0f;
        [field: SerializeField] public float ChaseFlipCooldown { get; private set; } = 1f;

        [field: Space]
        [field: Header("Attack State")]
        [field: SerializeField] public bool HasMeleeAttack { get; private set; } = true;
        [field: SerializeField] public bool HasRangeAttack { get; private set; } = true;
        [field: SerializeField] public float AttackCooldown { get; private set; } = 0.5f;

        [field: Space]
        [field: Header("Dying State")]
        [field: SerializeField] public float DyingDuration { get; private set; } = 2.0f;
    }
}