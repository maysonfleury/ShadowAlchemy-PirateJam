using UnityEngine;

public interface IMovable
{
    public void ApplyForce(ForceSO forceSO);
    public void ApplyRelativeForce(float forward, ForceSO forceSO);
}
