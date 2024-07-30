using Enemy;
using UnityEngine;

public interface IPossessable
{
    public bool IsPossessable();
    public bool TryPossession(out PossessionType type, out Vector3 pos);
}