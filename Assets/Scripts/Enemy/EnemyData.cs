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
        [field: SerializeField] public float ChaseDuration { get; private set; } = 10.0f;
        [field: SerializeField] public float AttackRange { get; private set; } = 5;

    }
}