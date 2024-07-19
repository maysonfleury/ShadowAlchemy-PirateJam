using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyController : MonoBehaviour
{

    public enum EnemyState
    {
        Inactive,
        Patrolling,
        Chasing,
    }

    public Rigidbody2D Rigidbody2D { get; private set; }
    public Collider2D RigidbodyCollider { get; private set; }

    [field: Space]
    [field: Header("Enemy Components")]
    [field: SerializeField] public EnemyData EnemyData { get; private set; }

    [field: Space]
    [field: Header("Detection Components")]
    [field: SerializeField] public Transform SensorPivot { get; private set; }
    [field: SerializeField] public Collider2D DetectionArea { get; private set; }
    [field: SerializeField] public Collider2D LedgeSensor { get; private set; }

    private ContactFilter2D ledgeSensorFilter;
    private readonly Collider2D[] ledgeSensorResults = new Collider2D[50];

    public float EnemySpeed { get; private set; } = 3.0f;
    public int HorizontalFacing { get; private set; } = 1;

    public EnemyState EnemyNavState { get; private set; } = EnemyState.Inactive;

    protected virtual void Start()
    {
        Rigidbody2D = GetComponent<Rigidbody2D>();
        RigidbodyCollider = GetComponent<Collider2D>();

        EnemyNavState = EnemyState.Patrolling;

        ledgeSensorFilter = new ContactFilter2D
        {
            useTriggers = true
        };

        ledgeSensorFilter.SetLayerMask(LayerMask.GetMask("Ground"));
    }

    protected void FixedUpdate()
    {
        switch (EnemyNavState)
        {
            case EnemyState.Patrolling:
                if (!LedgeCheck())
                    FlipCharacter();
                UpdateVelocity(new Vector2(HorizontalFacing, 0), EnemyData.DefaultSpeed);
                break;

            case EnemyState.Chasing:
                break;
            default: //EnemyState.Inactive:
                break;
        }
    }

    protected void UpdateVelocity(Vector2 direction, float speed)
    {
        Rigidbody2D.velocity = direction * speed;
    }

    protected void FlipCharacter()
    {
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;

        HorizontalFacing = -HorizontalFacing;
        Debug.Log($"Flipping horizontal direction to: {HorizontalFacing}");
    }

    private bool LedgeCheck()
    {
        int numColliders = LedgeSensor.OverlapCollider(ledgeSensorFilter, ledgeSensorResults);

        for (int i = 0; i < numColliders; i++)
        {
            Collider2D collider = ledgeSensorResults[i];
            if (collider != null)
            {
                return true;
            }
        }
;
        return false;
    }
}
