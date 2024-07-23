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
    TimeManager timeManager;
    Collider2D attackHitbox;

    void Start()
    {
        timeManager = TimeManager.Instance;
        attackHitbox = GetComponent<Collider2D>();
    }
    
    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Attack hit enemy");
            if (attackEffect && col.TryGetComponent(out IEffectable effectable))
            {
                effectable.ApplyEffect(attackEffect);
                Debug.Log("Attack Effect sent");
            }
            if (attackKnockback && col.TryGetComponent(out IMovable movable))
            {
                movable.ApplyForce(attackKnockback);
                Debug.Log("Attack Knockback sent");
            }

            timeManager.HitStopFrames(hitStunLength);

            attackHitbox.enabled = false;
        }
    }
}