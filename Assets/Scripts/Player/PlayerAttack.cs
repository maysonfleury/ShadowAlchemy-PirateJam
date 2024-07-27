using System.Collections;
using System.Collections.Generic;
using Effect;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] EffectSO attackEffect;
    [SerializeField] ForceSO attackKnockback;
    [SerializeField] float hitStunLength;
    [HideInInspector] public float forward;
    Collider2D attackHitbox;

    void Start()
    {
        attackHitbox = GetComponent<Collider2D>();
    }
    
    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.CompareTag("Enemy"))
        {
            if (gameObject.transform.parent.TryGetComponent(out IPlayerController controller))
            {
                controller.OnHitEnemy(hitStunLength);
            }

            Debug.Log("[PlayerAttack]: Hit enemy");
            if (attackEffect && col.TryGetComponent(out IEffectable effectable))
            {
                effectable.ApplyEffect(attackEffect);
                Debug.Log("[PlayerAttack]: Effect sent");
            }
            if (attackKnockback && col.TryGetComponent(out IMovable movable))
            {
                movable.ApplyRelativeForce(forward, attackKnockback);
                Debug.Log("[PlayerAttack]: Knockback sent");
            }

            attackHitbox.enabled = false;
        }
    }
}