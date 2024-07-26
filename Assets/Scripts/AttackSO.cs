using Effect;
using Projectiles;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Attacks
{
    [CreateAssetMenu(menuName = "AttackSO")]
    public class AttackSO : ScriptableObject
    {
        [field: SerializeField, Space] public EffectSO EffectSO { get; private set; }
        [field: SerializeField, Space] public ForceSO ForceSO { get; private set; }

        public void ApplyAttack(float forward, Transform target)
        {
            if (EffectSO != null && target.TryGetComponent(out IEffectable effectable))
            {
                effectable.ApplyEffect(EffectSO);
            }

            if (ForceSO != null && target.TryGetComponent(out IMovable movable))
            {
                movable.ApplyRelativeForce(forward, ForceSO);
            }
        }
    }
}