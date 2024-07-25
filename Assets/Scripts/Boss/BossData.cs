using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BossData
{
    [field: SerializeField] public float AttackCooldown { get; private set; } = 1;
}
