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
        [field: SerializeField] public float PatrolSpeed { get; private set; } = 5.0f;
        [field: SerializeField] public float ChaseSpeed { get; private set; } = 10.0f;
        [field: SerializeField] public float SweepSpeed { get; private set; } = 8.0f;
        [field: SerializeField] public float SweepDuration { get; private set; } = 5.0f;
        [field: SerializeField] public float FlipCooldown { get; private set; } = 2.0f;
        //[field: SerializeField] public float AttackRange { get; private set; } = 5;

    }
}