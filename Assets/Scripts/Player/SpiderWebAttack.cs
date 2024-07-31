using System.Collections;
using System.Collections.Generic;
using Effect;
using Unity.VisualScripting;
using UnityEngine;

public class SpiderWebAttack : MonoBehaviour
{
    [SerializeField] EffectSO attackEffect;
    [SerializeField] ForceSO attackKnockback;
    [SerializeField] ParticleSystem explosionPS;
    [SerializeField] float hitStunLength;
    [HideInInspector] public float forward;
    Collider2D attackHitbox;

    void Start()
    {
        attackHitbox = GetComponent<Collider2D>();
        forward = FindObjectOfType<SpiderController>().side;
    }
    
    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("[WebAttack]: Hit enemy");
            if (attackEffect && col.gameObject.TryGetComponent(out IEffectable effectable))
            {
                effectable.ApplyEffect(attackEffect);
                Debug.Log("[WebAttack]: Effect sent");
            }
            if (attackKnockback && col.gameObject.TryGetComponent(out IMovable movable))
            {
                movable.ApplyRelativeForce(forward, attackKnockback);
                Debug.Log("[WebAttack]: Knockback sent");
            }

            attackHitbox.enabled = false;
        }
        else if (col.gameObject.CompareTag("Player"))
        {
            // Do nothing
        }
        else
        {
            explosionPS.Play();
            Invoke("DestroySelf", 0.5f);
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("[WebAttack]: Hit enemy");
            if (attackEffect && col.TryGetComponent(out IEffectable effectable))
            {
                effectable.ApplyEffect(attackEffect);
                Debug.Log("[WebAttack]: Effect sent");
            }
            if (attackKnockback && col.TryGetComponent(out IMovable movable))
            {
                movable.ApplyRelativeForce(forward, attackKnockback);
                Debug.Log("[WebAttack]: Knockback sent");
            }

            attackHitbox.enabled = false;
        }
        else if (col.gameObject.CompareTag("Player"))
        {
            // Do nothing
        }
        else
        {
            explosionPS.Play();
            Invoke("DestroySelf", 0.5f);
        }
    }

    void DestroySelf()
    {
        Destroy(gameObject);
    }
}