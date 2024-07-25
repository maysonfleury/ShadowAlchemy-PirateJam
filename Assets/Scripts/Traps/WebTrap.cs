using System.Collections;
using System.Collections.Generic;
using Effect;
using UnityEngine;

public class WebTrap : MonoBehaviour
{
    [SerializeField] EffectSO slowEffect;
    [SerializeField] float playerSlowPercent = 20f;
    [SerializeField] ParticleSystem trapParticle;

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            Debug.Log("Player walked into a web!");
            
            if (col.gameObject.TryGetComponent(out IPlayerController playerController))
            {
                playerController.OnWebEnter(playerSlowPercent);
            }

            if (trapParticle)
                if (!trapParticle.isPlaying) {trapParticle.Play();}
        }
        else if (col.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Enemy walked into a web!");
        }
    }
    
    void OnTriggerExit2D(Collider2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            Debug.Log("Player exited a web!");

            if (col.gameObject.TryGetComponent(out IPlayerController playerController))
            {
                playerController.OnWebExit();
            }

            if (trapParticle)
            {
                trapParticle.Clear();
                trapParticle.Stop();
            }
        }
        else if (col.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Enemy exited a web!");
        }
    }
    
    void OnTriggerStay2D(Collider2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            if (trapParticle)
            {
                Vector3 pos = new Vector3(col.transform.position.x, col.transform.position.y, -0.5f);
                trapParticle.gameObject.transform.position = pos;
            }
        }
        else if (col.gameObject.CompareTag("Enemy"))
        {
            if (slowEffect && col.TryGetComponent(out IEffectable effectable))
            {
                effectable.ApplyEffect(slowEffect);
                Debug.Log("[WebTrap]: Slow Sent to Enemy " + col.name);
            }
        }
    }
}