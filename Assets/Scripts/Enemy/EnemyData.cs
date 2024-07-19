using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyData
{
    public enum EnemyBehaviourType
    {
        Neutral = 0,
        Aggressive = 1,
        Evasive = 2,
    }

    [field: SerializeField] public EnemyBehaviourType EnemyBehaviour { get; private set; }
    [field: SerializeField] public float DefaultSpeed { get; private set; } = 5.0f;
    [field: SerializeField] public float ChaseSpeed { get; private set; } = 10.0f;



}
