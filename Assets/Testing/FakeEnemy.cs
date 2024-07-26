using System.Collections;
using System.Collections.Generic;
using Effect;
using UnityEngine;

public class FakeEnemy : MonoBehaviour, IEffectable, IMovable
{
    [SerializeField] float hitPoints = 3;
    [SerializeField] bool canDie = true;
    [SerializeField] ParticleSystem deathParticles;

    public void ApplyEffect(EffectSO effectSO)
    {
        Debug.Log($"[FakeEnemy]: {name} hit : {effectSO.Damage}dmg : stunned {effectSO.StunData.Duration}s : slowed {effectSO.SlowData.Duration}s / {effectSO.SlowData.Percent}%");
        hitPoints -= effectSO.Damage;
        if (hitPoints <= 0 && canDie)
        {
            GetComponent<MeshRenderer>().enabled = false;
            GetComponent<Rigidbody2D>().simulated = false;
            SFXManager.Instance.Play("kill");
            deathParticles.Play();
            Destroy(gameObject, deathParticles.main.duration);
        }
    }

    public void ApplyForce(ForceSO forceSO)
    {
        Debug.Log("[FakeEnemy]: ApplyForce called on " + name + " from " + forceSO.name);
        float new_x = 1 * forceSO.Direction.x;
        Vector2 velocity = new Vector2(new_x, forceSO.Direction.y).normalized * forceSO.Speed;
        GetComponent<Rigidbody2D>().AddForce(velocity, forceSO.ForceMode);
    }

    public void ApplyRelativeForce(float forward, ForceSO forceSO)
    {
        Debug.Log("[FakeEnemy]: ApplyRelativeForce called on " + name + " from " + forceSO.name);
        if(forward == 0)
        {
            forward = 1;
        }
        float new_x = forward * forceSO.Direction.x;
        Vector2 velocity = new Vector2(new_x, forceSO.Direction.y).normalized * forceSO.Speed;
        GetComponent<Rigidbody2D>().AddForce(velocity, forceSO.ForceMode);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            Debug.Log("Player got hit by " + name + "!");

            PlayerFormController form = col.gameObject.GetComponentInParent<PlayerFormController>();
            
            if (col.gameObject.TryGetComponent(out IPlayerController controller))
            {
                controller.OnTakeDamage(new Vector2(transform.position.x, transform.position.y));
            }
            form.DamagePlayer();
            form.giveInvulnerability(1f);
        }
    }
}
