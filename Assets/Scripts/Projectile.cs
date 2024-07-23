using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{

    public Rigidbody2D Rigidbody { get; private set; }
    public Collider2D Collider { get; private set; }
    public ProjectileSO ProjectileSO { get; private set; }
    public int HorizontalFacing { get; private set; } = 1;

    ContactFilter2D hitMask;
    ContactFilter2D collisionMask;

    private readonly Collider2D[] colliders = new Collider2D[50];
    private readonly HashSet<Collider2D> previousHits = new(50);
    private float currentExpireTime = 0;
    private Vector2 currentDirection;


    private void Awake()
    {
        Rigidbody = GetComponent<Rigidbody2D>();
        Collider = GetComponent<Collider2D>();

        hitMask = new ContactFilter2D
        {
            useTriggers = true
        };

        collisionMask = new ContactFilter2D
        {
            useTriggers = true
        };

        gameObject.SetActive(false);
    }

    public void Initialize(ProjectileSO projectileSO, Vector2 startPosition, Vector2 direction)
    {
        ProjectileSO = projectileSO;
        transform.position = startPosition;
        hitMask.SetLayerMask(LayerUtility.CombineMasks(ProjectileSO.HitMasks));
        collisionMask.SetLayerMask(LayerUtility.CombineMasks(ProjectileSO.CollisionMasks));

        currentExpireTime = ProjectileSO.Duration + Time.time;
        currentDirection = direction;
        gameObject.SetActive(true);
    }

    private void FixedUpdate()
    {
        Rigidbody.velocity = ProjectileSO.Speed * currentDirection;
        ResolveFacing();

        int numColliders = Collider.OverlapCollider(hitMask, colliders);

        if(numColliders > 0)
        {
            for (int i = 0; i < numColliders; i++)
            {
                if (!previousHits.Contains(colliders[i]))
                {
                    OnHit(colliders[i]);
                }

                previousHits.Add(colliders[i]);
            }
        }

        numColliders = Collider.OverlapCollider(collisionMask, colliders);

        if (numColliders > 0)
        {
            OnCollision();
        }

        if(currentExpireTime < Time.time)
        {
            ExpireProjectile();
        }
    }

    private void OnHit(Collider2D target)
    {
        if(target.TryGetComponent(out IEffectable effectable))
        {
            if (ProjectileSO.EffectOnHit != null)
            {
                effectable.ApplyEffect(ProjectileSO.EffectOnHit);
            }
        }

        if (target.TryGetComponent(out IMovable movable))
        {

            if (ProjectileSO.ForceOnHit != null)
            {
                movable.ApplyRelativeForce(Rigidbody.velocity.x, ProjectileSO.ForceOnHit);
            }
        }
    }

    private void OnCollision()
    {
        ExpireProjectile();
    }

    private void ExpireProjectile()
    {
        Destroy(gameObject);
    }

    protected void ResolveFacing()
    {
        int sign;

        if (Rigidbody.velocity.x < 0)
        {
            sign = -1;
        }

        else
        {
            sign = 1;
        }

        if (HorizontalFacing == sign)
        {
            return;
        }

        else HorizontalFacing = sign;

        Vector3 localScale = transform.localScale;
        localScale.x *= HorizontalFacing;
        transform.localScale = localScale;
    }
}
