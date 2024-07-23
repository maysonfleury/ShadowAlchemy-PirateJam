using UnityEngine;

public interface IMovable
{
    public Rigidbody2D Rigidbody { get; }
    public void ApplyForce(ForceSO forceSO);
}
