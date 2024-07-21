using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ForceSO")]
public class ForceSO : ScriptableObject
{
    [field: SerializeField] public ForceMode2D ForceMode { get; private set; }
    [field: SerializeField] public Vector2 Direction { get; private set; }
    [field: SerializeField] public float Speed { get; private set; }
}
