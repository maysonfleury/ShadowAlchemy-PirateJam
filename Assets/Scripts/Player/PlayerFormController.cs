using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using DG.Tweening;
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
    public Transform currentTransform;
    public int currentHP;
    public int damagedStopTime;
    public float damageSafetyTime = 0.7f;

    [SerializeField] ShadeController shadeController;
    [SerializeField] BatController batController;
    [SerializeField] RatController ratController;
    [SerializeField] SpiderController spiderController;
    [SerializeField] SkeletonController skeletonController;
    [SerializeField] ParticleSystem possessParticle;
    
    private SFXManager sfxManager;
    private CinemachineVirtualCamera virtualCamera;
    private bool canTakeDamage;

    // Start is called before the first frame update
    void Start()
    {
        virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
        sfxManager = FindObjectOfType<SFXManager>();
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
            currentHP = 2;
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
        possessParticle.transform.position = currentTransform.position;
        possessParticle.Play();
        sfxManager.Play("possess");

        switch (form)
        {
            case PlayerForm.Shade:
                shadeController.gameObject.SetActive(true);
                shadeController.transform.position = currentTransform.position;
                SetCameraTarget(shadeController.transform);
                currentForm = PlayerForm.Shade;
                if (currentTransform != shadeController.transform)
                {
                    currentTransform.gameObject.SetActive(false);
                    currentTransform = shadeController.transform;
                }
                currentHP = 2;
            break;

            case PlayerForm.Bat:
                batController.gameObject.SetActive(true);
                batController.transform.position = currentTransform.position;
                SetCameraTarget(batController.transform);
                currentForm = PlayerForm.Bat;
                if (currentTransform != batController.transform)
                {
                    currentTransform.gameObject.SetActive(false);
                    currentTransform = batController.transform;
                }
                currentHP = 1;
            break;

            case PlayerForm.Rat:
                ratController.gameObject.SetActive(true);
                ratController.transform.position = currentTransform.position;
                SetCameraTarget(ratController.transform);
                currentForm = PlayerForm.Rat;
                if (currentTransform != ratController.transform)
                {
                    currentTransform.gameObject.SetActive(false);
                    currentTransform = ratController.transform;
                }
                currentHP = 2;
            break;

            case PlayerForm.Spider:
                spiderController.gameObject.SetActive(true);
                spiderController.transform.position = currentTransform.position;
                SetCameraTarget(spiderController.transform);
                currentForm = PlayerForm.Spider;
                if (currentTransform != spiderController.transform)
                {
                    currentTransform.gameObject.SetActive(false);
                    currentTransform = spiderController.transform;
                }
                currentHP = 2;
            break;

            case PlayerForm.Skeleton:
                skeletonController.gameObject.SetActive(true);
                skeletonController.transform.position = currentTransform.position;
                SetCameraTarget(skeletonController.transform);
                currentForm = PlayerForm.Skeleton;
                if (currentTransform != skeletonController.transform)
                {
                    currentTransform.gameObject.SetActive(false);
                    currentTransform = skeletonController.transform;
                }
                currentHP = 4;
            break;
        }
    }

    public void DamagePlayer()
    {
        if (!canTakeDamage)
            return;
        Debug.Log("[PlayerFormController]: Player took damage.");

        if (currentForm == PlayerForm.Shade && currentHP == 1)
        {
            // Die
            sfxManager.Play("die");
            currentHP--;
            Debug.Log("dead");
            TimeManager.Instance.GameOver();
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
                    sfxManager.Play("die");
                break;

                case PlayerForm.Bat:
                    // TODO: Bat hurt VFX/SFX
                    sfxManager.Play("die");
                break;

                case PlayerForm.Rat:
                    // TODO: Rat hurt VFX/SFX
                    sfxManager.Play("die");
                break;

                case PlayerForm.Spider:
                    // TODO: Spider hurt VFX/SFX
                    sfxManager.Play("die");
                break;

                case PlayerForm.Skeleton:
                    // TODO: Skeleton hurt VFX/SFX
                    sfxManager.Play("die");
                break;
            }
            currentHP--;
            TimeManager.Instance.HitStopFrames(damagedStopTime);
        }

        giveInvulnerability(damageSafetyTime);
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
