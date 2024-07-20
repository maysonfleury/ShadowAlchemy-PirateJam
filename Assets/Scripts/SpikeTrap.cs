using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    [SerializeField] float knockForward = 5f;
    [SerializeField] float knockUp = 10f;
    [SerializeField] ParticleSystem trapParticle;
    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            Debug.Log("it works");

            PlayerFormController form = col.gameObject.GetComponentInParent<PlayerFormController>();
            
            if (form.currentForm != PlayerFormController.PlayerForm.Shade)
            {
                form.TakeDamage();
                form.giveInvulnerability(0.2f);

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
        else
        {
            //Debug.Log("yes it does");
        }
    }
}
