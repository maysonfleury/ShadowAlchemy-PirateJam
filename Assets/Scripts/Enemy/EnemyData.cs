using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Enemy
{
    public enum EnemyBehaviourType
    {
        Neutral = 0,
        Aggressive = 1,
        Evasive = 2,
    }

    [Serializable]
    public class EnemyData
    {
        [field: SerializeField] public EnemyBehaviourType EnemyBehaviour { get; private set; }
        [field: SerializeField, Min(1)] public int DefaultHealth { get; private set; } = 1;

        [field: Space]
        [field: Header("Sleep State")]
        [field: SerializeField] public bool HasSleepState { get; private set; } = false;
        [field: SerializeField] public float SleepDuration { get; private set; } = 5.0f;

        [field: Space]
        [field: Header("Patrol State")]
        [field: SerializeField] public float PatrolSpeed { get; private set; } = 5.0f;
        [field: SerializeField] public float PatrolFlipTime { get; private set; } = 1f;
        [field: SerializeField] public float PatrolDuration { get; private set; } = 5.0f;

        [field: Space]
        [field: Header("Chase State")]
        [field: SerializeField] public float ChaseSpeed { get; private set; } = 10.0f;
        [field: SerializeField] public float ChaseFlipTime { get; private set; } = 0.2f;
        [field: SerializeField] public float ChaseFlipCooldown { get; private set; } = 0.5f;
        //[field: SerializeField] public bool ChaseOnAttackCooldown { get; private set; } = true;

        [field: Space]
        [field: Header("Sweep State")]
        [field: SerializeField] public float SweepSpeed { get; private set; } = 8.0f;
        [field: SerializeField] public float SweepDuration { get; private set; } = 5.0f;
        [field: SerializeField] public float SweepFlipTime { get; private set; } = 0.5f;

        [field: Space]
        [field: Header("Attack State")]
        [field: SerializeField] public float AttackCooldown { get; private set; } = 0.5f;

        [field: Space]
        [field: Header("Watch State")]
        [field: SerializeField] public bool HasWatchState { get; private set; } = true;

        [field: Space]
        [field: Header("Dying State")]
        [field: SerializeField] public float DyingDuration { get; private set; } = 2.0f;
    }
}