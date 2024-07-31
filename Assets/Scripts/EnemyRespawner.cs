using Enemy;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class EnemyRespawner : MonoBehaviour
{
    public EnemyController EnemyPrefab;
    public float RespawnTime = 10;
    public float lerpTime = 3;
    public AnimationCurve heightCurve;
    public Vector3 startOffset;
    public Vector3 endOffset;
    public float maxLinkedDistance = 10;
    private float currentTime = 0;
    private EnemyController linkedEnemy;
    private bool respawnEnemy = true;
    private bool enemyMarkedForDeletion = false;

    private void Start()
    {
        RespawnEnemy();
    }


    private void FixedUpdate()
    {
        if(linkedEnemy != null && !enemyMarkedForDeletion)
        {
            float distance = Vector2.Distance(new Vector2(linkedEnemy.transform.position.x, linkedEnemy.transform.position.y),
                new Vector2(transform.position.x, transform.position.y));

            if (distance > maxLinkedDistance)
            {
                linkedEnemy.ExpireEnemy();
                enemyMarkedForDeletion = true;
            }
 
        }

        if (respawnEnemy && currentTime < Time.time)
        {
            RespawnEnemy();
        }

        else return;
    }

    public void RespawnCallback()
    {
        respawnEnemy = true;
        currentTime = Time.time + RespawnTime;
    }



    private void RespawnEnemy()
    {
        respawnEnemy = false;
        EnemyController enemy = Instantiate(EnemyPrefab, transform.position, Quaternion.identity, transform);
        linkedEnemy = enemy.RespawnLink(this);
        StartCoroutine(RespawnCoroutine(enemy));
    }

    private IEnumerator RespawnCoroutine(EnemyController enemy)
    {
        Transform enemyTransform = enemy.transform;
        enemy.Rigidbody.isKinematic = true;

        enemyMarkedForDeletion = false;
        Vector3 startPosition = enemyTransform.position + startOffset;
        Vector3 endPosition = new(startPosition.x + endOffset.x, startPosition.y + endOffset.y, endOffset.z);
        float timeElapsed = 0;

        while (timeElapsed < lerpTime)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / lerpTime;
            float z = Mathf.Lerp(startPosition.z, endPosition.z, Mathf.SmoothStep(0, 1, t));
            float y = startPosition.y + heightCurve.Evaluate(t);

            enemyTransform.position = new Vector3(startPosition.x, y, z);
            yield return null;
        }

        enemy.Rigidbody.isKinematic = false;
    }
}




