using Effect;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Projectiles
{
    [CreateAssetMenu(menuName = "ProjectileSO")]
    public class ProjectileSO : ScriptableObject
    {
        [field: SerializeField] public float Speed { get; private set; } = 5f;
        [field: SerializeField, Min(0.1f)] public float Duration { get; private set; } = 5f;
        [field: SerializeField] public float AngleOffset { get; private set; } = 5f;
        [field: SerializeField] public Hit Hit { get; private set; }
        [field: SerializeField] public Collision Collision { get; private set; }
        [field: SerializeField] public Homing Homing { get; private set; }
        [field: SerializeField] public Multi Multi { get; private set; }
    }


    [Serializable]
    public class Hit
    {
        [field: SerializeField] public bool Enabled { get; private set; } = false;
        [field: SerializeField] public EffectSO EffectOnHit { get; private set; }
        [field: SerializeField] public ForceSO ForceOnHit { get; private set; }
        [field: SerializeField] public LayerMask[] HitMasks { get; private set; }

    }

    [Serializable]
    public class Collision
    {
        [field: SerializeField] public bool Enabled { get; private set; } = false;
        [field: SerializeField] public EffectSO EffectOnCollision { get; private set; }
        [field: SerializeField] public ForceSO ForceOnCollision { get; private set; }
        [field: SerializeField] public LayerMask[] CollisionMasks { get; private set; }
    }

    [Serializable]
    public class Homing
    {
        [field: SerializeField] public bool Enabled { get; private set; } = false;
        [field: SerializeField, Min(1)] public float AngularSpeed { get; private set; } = 1;
        [field: SerializeField] public LayerMask[] HomingMasks { get; private set; }
    }

    [Serializable]
    public class Multi
    {
        [field: SerializeField] public bool Enabled { get; private set; } = false;
        [field: SerializeField, Min(1)] public int NumProjectiles { get; private set; } = 1;
        [field: SerializeField, Range(0, 360)] public float SpreadAngle { get; private set; } = 90;
    }
}