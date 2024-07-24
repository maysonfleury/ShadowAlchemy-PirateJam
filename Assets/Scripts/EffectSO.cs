using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Effect
{
    [CreateAssetMenu(menuName = "EffectSO")]
    public class EffectSO : ScriptableObject
    {
        [field: SerializeField, Space] public int Damage { get; private set; } = 5;
        [field: SerializeField, Space] public Stun StunData { get; private set; }
        [field: SerializeField, Space] public Slow SlowData { get; private set; }
    }

    [Serializable]
    public class Stun
    {
        [field: SerializeField] public bool Enabled { get; private set; } = false;
        [field: SerializeField] public float Duration { get; private set; } = 2;
    }

    [Serializable]
    public class Slow
    {
        [field: SerializeField] public bool Enabled { get; private set; } = false;
        [field: SerializeField, Range(0, 90)] public float Percent { get; private set; } = 20;
        [field: SerializeField] public float Duration { get; private set; } = 2;
        //Probably some particle system data if it's like spider web
    }

}