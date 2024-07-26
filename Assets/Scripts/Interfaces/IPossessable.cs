using Enemy;
using UnityEngine;

public interface IPossessable
{
    public bool TryPossession(out PossessionType type);
}