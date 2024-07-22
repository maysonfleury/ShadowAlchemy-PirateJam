using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabManager : PersistentSingleton<PrefabManager>
{
    [field: SerializeField] public Projectile Projectile { get; private set; }
}
