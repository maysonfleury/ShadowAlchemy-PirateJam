using System.Collections;
using System.Collections.Generic;
using Effect;
using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    [Header("Player Values")]
    [SerializeField] float knockForward = 5f;
    [SerializeField] float knockUp = 10f;
    [SerializeField] ParticleSystem trapParticle;

    [Space]
    [Header("Enemy Values")]
    [SerializeField] EffectSO damageEffect;
    [SerializeField] ForceSO knockbackEffect;

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            Debug.Log("Player stepped on spikes!");

            PlayerFormController form = col.gameObject.GetComponentInParent<PlayerFormController>();
            
            if (form.currentForm != PlayerFormController.PlayerForm.Shade)
            {
                form.DamagePlayer();
                form.giveInvulnerability(1f);

                Rigidbody2D rb = col.gameObject.GetComponent<Rigidbody2D>();
                rb.velocity = new Vector2(rb.velocity.x, 0);
                if (rb.velocity.x > 0)
                    rb.velocity += (Vector2.up * knockUp) + (Vector2.right * knockForward);
                else
                    rb.velocity += (Vector2.up * knockUp) + (Vector2.left * knockForward);

                trapParticle.gameObject.transform.position = col.transform.position;
                trapParticle.Play();
            }
        }
        else if (col.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Enemy stepped on spikes!");
            if (damageEffect && col.collider.TryGetComponent(out IEffectable effectable))
            {
                effectable.ApplyEffect(damageEffect);
            }
            if (knockbackEffect && col.collider.TryGetComponent(out IMovable movable))
            {
                movable.ApplyForce(knockbackEffect);
            }
        }
    }
}