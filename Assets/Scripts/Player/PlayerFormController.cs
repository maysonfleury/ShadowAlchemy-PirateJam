using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class PlayerFormController : MonoBehaviour
{
    public enum PlayerForm
    {
        Shade,
        Bat,
        Rat,
        Spider,
        Skeleton
    }

    public PlayerForm currentForm;
    private Transform currentTransform;
    public int currentHP;

    [SerializeField] GameObject shadeObject;
    [SerializeField] GameObject batObject;
    [SerializeField] GameObject ratObject;
    [SerializeField] GameObject spiderObject;
    [SerializeField] GameObject skeletonObject;
    [SerializeField] ParticleSystem possessParticle;
    
    private CinemachineVirtualCamera virtualCamera;
    private bool canTakeDamage;

    // Start is called before the first frame update
    void Start()
    {
        virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
        currentForm = PlayerForm.Shade;
        currentTransform = shadeObject.transform;
        currentHP = 1;
        canTakeDamage = true;
        //StartCoroutine(ChangeFormRoutine(PlayerForm.Spider, 5f));
        //StartCoroutine(ChangeFormRoutine(PlayerForm.Bat, 10f));
        StartCoroutine(ChangeFormRoutine(PlayerForm.Rat, 5f));
        //StartCoroutine(ChangeFormRoutine(PlayerForm.Skeleton, 20f));
        //StartCoroutine(ChangeFormRoutine(PlayerForm.Shade, 25f));
    }

    IEnumerator ChangeFormRoutine(PlayerForm form, float delay)
    {
        yield return new WaitForSeconds(delay);
        ChangeForm(form);
        yield return null;
    }

    public void ChangeForm(PlayerForm form)
    {
        if (form == currentForm)
            return;

        possessParticle.transform.position = currentTransform.position;
        possessParticle.Play();

        switch (form)
        {
            case PlayerForm.Shade:
                shadeObject.SetActive(true);
                shadeObject.transform.position = currentTransform.position;
                ChangeCameraTarget(shadeObject.transform);
                currentForm = PlayerForm.Shade;
                currentTransform.gameObject.SetActive(false);
                currentTransform = shadeObject.transform;
                currentHP = 1;
            break;

            case PlayerForm.Bat:
                batObject.SetActive(true);
                batObject.transform.position = currentTransform.position;
                ChangeCameraTarget(batObject.transform);
                currentForm = PlayerForm.Bat;
                currentTransform.gameObject.SetActive(false);
                currentTransform = batObject.transform;
                currentHP = 1;
            break;

            case PlayerForm.Rat:
                ratObject.SetActive(true);
                ratObject.transform.position = currentTransform.position;
                ChangeCameraTarget(ratObject.transform);
                currentForm = PlayerForm.Rat;
                currentTransform.gameObject.SetActive(false);
                currentTransform = ratObject.transform;
                currentHP = 2;
            break;

            case PlayerForm.Spider:
                spiderObject.SetActive(true);
                spiderObject.transform.position = currentTransform.position;
                ChangeCameraTarget(spiderObject.transform);
                currentForm = PlayerForm.Spider;
                currentTransform.gameObject.SetActive(false);
                currentTransform = spiderObject.transform;
                currentHP = 2;
            break;

            case PlayerForm.Skeleton:
                skeletonObject.SetActive(true);
                skeletonObject.transform.position = currentTransform.position;
                ChangeCameraTarget(skeletonObject.transform);
                currentForm = PlayerForm.Skeleton;
                currentTransform.gameObject.SetActive(false);
                currentTransform = skeletonObject.transform;
                currentHP = 4;
            break;
        }
    }

    public void TakeDamage()
    {
        if (!canTakeDamage)
            return;
            
        if (currentForm == PlayerForm.Shade && currentHP == 1)
        {
            // Die
            currentHP--;
            Debug.Log("dead");
        }
        else if (currentForm != PlayerForm.Shade && currentHP == 1)
        {
            ChangeForm(PlayerForm.Shade);
        }
        else if (currentHP > 1)
        {
            switch (currentForm)
            {
                case PlayerForm.Shade:
                break;

                case PlayerForm.Bat:
                    // Bat hurt VFX/SFX
                break;

                case PlayerForm.Rat:
                    // Rat hurt VFX/SFX
                break;

                case PlayerForm.Spider:
                    // Spider hurt VFX/SFX
                break;

                case PlayerForm.Skeleton:
                    // Skeleton hurt VFX/SFX
                break;
            }
            currentHP--;
        }
    }

    public void giveInvulnerability(float time)
    {
        StartCoroutine(iFrameRoutine(time));
    }

    IEnumerator iFrameRoutine(float iFrameTime)
    {
        canTakeDamage = false;
        yield return new WaitForSeconds(iFrameTime);
        canTakeDamage = true;
    }

    private void ChangeCameraTarget(Transform newTarget)
    {
        virtualCamera.m_Follow = newTarget;
        virtualCamera.m_LookAt = newTarget;
    }
}
