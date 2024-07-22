using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ProjectileSO")]
public class ProjectileSO : ScriptableObject
{
    [field: SerializeField] public int Damage { get; private set; } = 1;
    [field: SerializeField] public float Speed { get; private set; } = 5f;
    [field: SerializeField] public float Duration { get; private set; } = 5f;
    [field: SerializeField] public LayerMask[] CollisionMasks { get; private set; }
    [field: SerializeField] public LayerMask[] HitMasks { get; private set; }
}
