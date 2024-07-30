using System.Collections;
using System.Collections.Generic;
using Effect;
using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    [Header("Player Values")]
    [SerializeField] Transform launchTarget;
    [SerializeField] float launchStrength = 40f;
    [SerializeField] ParticleSystem trapParticle;

    [Space]
    [Header("Enemy Values")]
    [SerializeField] EffectSO damageEffect;
    [SerializeField] ForceSO knockbackEffect;

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            Debug.Log("Player stepped on spikes!");

            PlayerFormController form = col.gameObject.GetComponentInParent<PlayerFormController>();
            
            if (launchTarget && col.gameObject.TryGetComponent(out IPlayerController controller))
            {
                if (form.currentForm != PlayerFormController.PlayerForm.Shade)
                {
                    Vector2 launchDir = new Vector2(launchTarget.position.x - col.transform.position.x, launchTarget.position.y - col.transform.position.y).normalized;
                    controller.OnHitSpikes(launchDir, launchStrength);

                    trapParticle.gameObject.transform.localPosition = new Vector2(col.transform.position.x, trapParticle.gameObject.transform.localPosition.y);
                    trapParticle.Play();

                    form.DamagePlayer();
                }
            }
        }
        //else if (col.gameObject.CompareTag("Enemy"))
        //{
        //    Debug.Log("Enemy stepped on spikes!");
        //    if (damageEffect && col.TryGetComponent(out IEffectable effectable))
        //    {
        //        effectable.ApplyEffect(damageEffect);
        //    }
        //    if (knockbackEffect && col.TryGetComponent(out IMovable movable))
        //    {
        //        movable.ApplyForce(knockbackEffect);
        //    }
        //}
    }
}