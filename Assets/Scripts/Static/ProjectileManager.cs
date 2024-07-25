using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Projectiles
{
    public class ProjectileManager : PersistentSingleton<ProjectileManager>
    {
        [SerializeField] private Projectile projectilePrefab;

        public void Request(ProjectileSO projectileSO, Vector2 startPosition, Vector2 direction)
        {
            int numProjectiles = 1;
            float spreadAngle = 0;

            if (projectileSO.Multi.Enabled)
            {
                numProjectiles = projectileSO.Multi.NumProjectiles;
                spreadAngle = projectileSO.Multi.SpreadAngle;
            }

            float heading = 0;
            if(direction.x < 0)
            {
                heading = 180;
            }

            float angleIncrement = 0;
            if (numProjectiles > 1)
            {
                angleIncrement = spreadAngle / (numProjectiles - 1);
            }


            float initialAngleOffset = -spreadAngle / 2;

            Quaternion rotation;
            for (int i = 0; i < numProjectiles; i++)
            {
                float angle = initialAngleOffset + i * angleIncrement;
                Debug.Log(direction);
                Vector3 rotatedDirection = Quaternion.Euler(0, 0, 90) * direction;
                rotation = Quaternion.LookRotation(Vector3.forward, rotatedDirection);
                rotation *= Quaternion.AngleAxis(angle, Vector3.forward);
                //rotation *= Quaternion.AngleAxis(heading, Vector3.up);
                Projectile projectile = Instantiate(projectilePrefab, startPosition, rotation, transform);
                projectile.Initialize(projectileSO);
            }

        }


    }
}