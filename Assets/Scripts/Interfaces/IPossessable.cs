using UnityEngine;

public interface IPossessable
{
    public bool TryPossession(out Transform transform);
}