using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.VisualScripting;
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

    [SerializeField] ShadeController shadeController;
    [SerializeField] BatController batController;
    [SerializeField] RatController ratController;
    [SerializeField] SpiderController spiderController;
    [SerializeField] SkeletonController skeletonController;
    [SerializeField] ParticleSystem possessParticle;
    
    private CinemachineVirtualCamera virtualCamera;
    private bool canTakeDamage;

    // Start is called before the first frame update
    void Start()
    {
        virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
        InitializeForm();
        canTakeDamage = true;
        SetCameraTarget(currentTransform);
        //StartCoroutine(ChangeFormRoutine(PlayerForm.Spider, 5f));
        //StartCoroutine(ChangeFormRoutine(PlayerForm.Bat, 10f));
        //StartCoroutine(ChangeFormRoutine(PlayerForm.Rat, 5f));
        //StartCoroutine(ChangeFormRoutine(PlayerForm.Skeleton, 20f));
        //StartCoroutine(ChangeFormRoutine(PlayerForm.Shade, 25f));
    }

    //IEnumerator ChangeFormRoutine(PlayerForm form, float delay)
    //{
    //    yield return new WaitForSeconds(delay);
    //    ChangeForm(form, currentTransform.position);
    //    yield return null;
    //}

    private void InitializeForm()
    {
        if (shadeController.gameObject.activeSelf == true)
        {
            currentHP = 1;
            currentTransform = shadeController.transform;
            currentForm = PlayerForm.Shade;
        }
        else if (batController.gameObject.activeSelf == true)
        {
            currentTransform = batController.transform;
            currentHP = 1;
            currentForm = PlayerForm.Bat;
        }
        else if (ratController.gameObject.activeSelf == true)
        {
            currentTransform = ratController.transform;
            currentHP = 2;
            currentForm = PlayerForm.Rat;
        }
        else if (spiderController.gameObject.activeSelf == true)
        {
            currentTransform = spiderController.transform;
            currentHP = 2;
            currentForm = PlayerForm.Spider;
        }
        else if (skeletonController.gameObject.activeSelf == true)
        {
            currentHP = 4;
            currentTransform = skeletonController.transform;
            currentForm = PlayerForm.Skeleton;
        }
        else
        {
            Debug.LogError("No Form is active.");
        }
    }

    public void ChangeForm(PlayerForm form, Vector3 newPosition)
    {
        if (form == currentForm)
            return;

        possessParticle.transform.position = currentTransform.position;
        possessParticle.Play();

        switch (form)
        {
            case PlayerForm.Shade:
                shadeController.gameObject.SetActive(true);
                shadeController.transform.position = currentTransform.position;
                SetCameraTarget(shadeController.transform);
                currentForm = PlayerForm.Shade;
                currentTransform.gameObject.SetActive(false);
                currentTransform = shadeController.transform;
                currentHP = 1;
            break;

            case PlayerForm.Bat:
                batController.gameObject.SetActive(true);
                batController.transform.position = currentTransform.position;
                SetCameraTarget(batController.transform);
                currentForm = PlayerForm.Bat;
                currentTransform.gameObject.SetActive(false);
                currentTransform = batController.transform;
                currentHP = 1;
            break;

            case PlayerForm.Rat:
                ratController.gameObject.SetActive(true);
                ratController.transform.position = currentTransform.position;
                SetCameraTarget(ratController.transform);
                currentForm = PlayerForm.Rat;
                currentTransform.gameObject.SetActive(false);
                currentTransform = ratController.transform;
                currentHP = 2;
            break;

            case PlayerForm.Spider:
                spiderController.gameObject.SetActive(true);
                spiderController.transform.position = currentTransform.position;
                SetCameraTarget(spiderController.transform);
                currentForm = PlayerForm.Spider;
                currentTransform.gameObject.SetActive(false);
                currentTransform = spiderController.transform;
                currentHP = 2;
            break;

            case PlayerForm.Skeleton:
                skeletonController.gameObject.SetActive(true);
                skeletonController.transform.position = currentTransform.position;
                SetCameraTarget(skeletonController.transform);
                currentForm = PlayerForm.Skeleton;
                currentTransform.gameObject.SetActive(false);
                currentTransform = skeletonController.transform;
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
            ChangeForm(PlayerForm.Shade, currentTransform.position);
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

    private void SetCameraTarget(Transform newTarget)
    {
        virtualCamera.m_Follow = newTarget;
        virtualCamera.m_LookAt = newTarget;
    }
}
