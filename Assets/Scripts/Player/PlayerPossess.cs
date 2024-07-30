using System.Collections;
using System.Collections.Generic;
using Effect;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerPossess : MonoBehaviour
{
    ShadeController controller;
    [SerializeField] List<Collider2D> possessionTargets;

    void Start()
    {
        controller = GetComponentInParent<ShadeController>();
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.E))
        {
            if (possessionTargets.Count > 0 && controller.canMove)
            {
                controller.OnPossessEnemy();
                PossessNearestEnemy();
            }
        }
    }
    
    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("[PlayerPossess]: Enemy found:" + col);
            if (col.TryGetComponent(out IPossessable possessable))
            {
                Debug.Log("[PlayerPossess]: Possessable enemy found:" + possessable);
                if (possessable.IsPossessable())
                {
                    // Add to Dictionary
                    possessionTargets.Add(col);
                    Debug.Log($"[PlayerPossess]: {possessable} added to Possessables");
                }
            }
        }
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if (col.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("[PlayerPossess]: Enemy lost:" + col);
            if (col.TryGetComponent(out IPossessable possessable))
            {
                if (possessable.IsPossessable())
                {
                    // Remove Dictionary
                    possessionTargets.Remove(col);
                    Debug.Log($"[PlayerPossess]: {possessable} removed from Possessables");
                }
            }
        }
    }

    public void PossessNearestEnemy()
    {
        if (possessionTargets.Count == 0)
        {
            Debug.Log("[PlayerPossess]: No Enemies in range.");
            return;
        }

        Collider2D closestEnemy = GetClosestEnemy();
        // Possess the enemy
        if (closestEnemy.TryGetComponent(out IPossessable possessable))
        {
            if (possessable.TryPossession(out PossessionType enemyType, out Vector3 pos))
            {
                Debug.Log("[PlayerPossess]: Possessing enemy with type " + enemyType);
                PlayerFormController pfc = GetComponentInParent<PlayerFormController>();
                if (pfc) pfc.PossessEnemy(enemyType, closestEnemy.transform.position);
            }
            else
            {
                Debug.Log("[PlayerPossess]: Possession failed");
            }
        }
    }

    private Collider2D GetClosestEnemy()
    {
        Vector3 playerPos = gameObject.transform.parent.transform.position;
        float closestDist = Mathf.Infinity;
        Collider2D closestEnemy = null;

        foreach(Collider2D enemy in possessionTargets)
        {
            float distance = Vector3.Distance(playerPos, enemy.transform.position);

            if (distance < closestDist) 
            {
               closestEnemy = enemy;
               closestDist = distance;
            }
        }

        return closestEnemy;
    }
}