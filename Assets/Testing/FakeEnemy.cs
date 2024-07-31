using System.Collections;
using System.Collections.Generic;
using Effect;
using UnityEngine;

public class FakeEnemy : MonoBehaviour, IEffectable, IMovable, IPossessable
{
    [SerializeField] PossessionType enemyType;
    [SerializeField] float hitPoints = 3;
    [SerializeField] bool canDie = true;
    [SerializeField] ParticleSystem deathParticles;
    private bool hurtPlayerOnTouch;

    void Start()
    {
        hurtPlayerOnTouch = true;
    }

    public void ApplyEffect(EffectSO effectSO)
    {
        Debug.Log($"[FakeEnemy]: {name} hit : {effectSO.Damage}dmg : stunned {effectSO.StunData.Duration}s : slowed {effectSO.SlowData.Duration}s / {effectSO.SlowData.Percent}%");
        hitPoints -= effectSO.Damage;
        if (hitPoints <= 0 && canDie)
        {
            GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll; // Stop the corpse from flying away
            GetComponentInChildren<MeshRenderer>().enabled = false;
            GetComponent<Collider2D>().isTrigger = true;
            hurtPlayerOnTouch = false;
            SFXManager.Instance.Play("kill");
            deathParticles.Play();
            Invoke("KillSelf", 5f); // 5 second timer to possess before corpse dissappears
        }
    }

    public void ApplyForce(ForceSO forceSO)
    {
        Debug.Log("[FakeEnemy]: ApplyForce called on " + name + " from " + forceSO.name );
        float new_x = 1 * forceSO.Direction.x;
        Vector2 velocity = new Vector2(new_x, forceSO.Direction.y).normalized * forceSO.Speed;
        GetComponent<Rigidbody2D>().AddForce(velocity, forceSO.ForceMode);
    }

    public void ApplyRelativeForce(float forward, ForceSO forceSO)
    {
        Debug.Log("[FakeEnemy]: ApplyRelativeForce called on " + name + " from " + forceSO.name + " with forward " + forward);
        if(forward == 0)
        {
            forward = 1;
        }
        float new_x = forward * forceSO.Direction.x;
        Vector2 velocity = new Vector2(new_x, forceSO.Direction.y).normalized * forceSO.Speed;
        GetComponent<Rigidbody2D>().AddForce(velocity, forceSO.ForceMode);
    }

    public bool IsPossessable()
    {
        Debug.Log("[FakeEnemy]: IsPossessable called, " + (hitPoints <= 0f));
        return hitPoints <= 0f;
    }

    private void KillSelf()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    public bool TryPossession(out PossessionType type, out Vector3 pos)
    {
        type = PossessionType.None;
        pos = transform.position;
        if (hitPoints <= 0f)
        {
            type = enemyType;
            Invoke("KillSelf", 0.5f);
            return true;
        }

        else return false;
    }

    // Hurt the player on touch, even without an attack
    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player") && hurtPlayerOnTouch)
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
