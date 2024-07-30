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
        SkeletonArcher,
        SkeletonWarrior
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
    [SerializeField] SkeletonController skeletonWarriorController;
    [SerializeField] SkeletonController skeletonArcherController;
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

    void Update()
    {
        if (Input.GetKey("[0]")) ChangeForm(PlayerForm.Shade, currentTransform.position);
        if (Input.GetKey("[1]")) ChangeForm(PlayerForm.Rat, currentTransform.position);
        if (Input.GetKey("[2]")) ChangeForm(PlayerForm.Bat, currentTransform.position);
        if (Input.GetKey("[3]")) ChangeForm(PlayerForm.Spider, currentTransform.position);
        if (Input.GetKey("[4]")) ChangeForm(PlayerForm.SkeletonWarrior, currentTransform.position);
        if (Input.GetKey("[5]")) ChangeForm(PlayerForm.SkeletonArcher, currentTransform.position);
    }

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
            currentHP = 3;
            currentForm = PlayerForm.Spider;
        }
        else if (skeletonWarriorController.gameObject.activeSelf == true)
        {
            currentHP = 4;
            currentTransform = skeletonWarriorController.transform;
            currentForm = PlayerForm.SkeletonWarrior;
        }
        else if (skeletonArcherController.gameObject.activeSelf == true)
        {
            currentHP = 4;
            currentTransform = skeletonArcherController.transform;
            currentForm = PlayerForm.SkeletonArcher;
        }
        else
        {
            Debug.LogError("No Form is active.");
        }
    }

    public void ChangeForm(PlayerForm form, Vector3 newPosition)
    {
        switch (form)
        {
            case PlayerForm.Shade:
                shadeController.gameObject.SetActive(true);
                shadeController.transform.position = newPosition;
                SetCameraTarget(shadeController.transform);
                currentForm = PlayerForm.Shade;
                if (currentTransform != shadeController.transform)
                {
                    shadeController.GetComponent<Rigidbody2D>().velocity += currentTransform.GetComponent<Rigidbody2D>().velocity;
                    currentTransform.gameObject.SetActive(false);
                    currentTransform = shadeController.transform;
                }
                currentHP = 2;
            break;

            case PlayerForm.Bat:
                batController.gameObject.SetActive(true);
                batController.transform.position = newPosition;
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
                ratController.transform.position = newPosition;
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
                spiderController.transform.position = newPosition;
                SetCameraTarget(spiderController.transform);
                currentForm = PlayerForm.Spider;
                if (currentTransform != spiderController.transform)
                {
                    currentTransform.gameObject.SetActive(false);
                    currentTransform = spiderController.transform;
                }
                currentHP = 3;
            break;

            case PlayerForm.SkeletonWarrior:
                skeletonWarriorController.gameObject.SetActive(true);
                skeletonWarriorController.transform.position = newPosition;
                SetCameraTarget(skeletonWarriorController.transform);
                currentForm = PlayerForm.SkeletonWarrior;
                if (currentTransform != skeletonWarriorController.transform)
                {
                    currentTransform.gameObject.SetActive(false);
                    currentTransform = skeletonWarriorController.transform;
                }
                currentHP = 4;
            break;

            case PlayerForm.SkeletonArcher:
                skeletonArcherController.gameObject.SetActive(true);
                skeletonArcherController.transform.position = newPosition;
                SetCameraTarget(skeletonArcherController.transform);
                currentForm = PlayerForm.SkeletonArcher;
                if (currentTransform != skeletonArcherController.transform)
                {
                    currentTransform.gameObject.SetActive(false);
                    currentTransform = skeletonArcherController.transform;
                }
                currentHP = 4;
            break;
        }
    }

    IEnumerator ChangeFormRoutine(PlayerForm form, Vector3 newPos, float delay)
    {
        yield return new WaitForSeconds(delay);
        ChangeForm(form, newPos);
        yield return null;
    }

    public void DamagePlayer()
    {
        if (!canTakeDamage)
            return;
        Debug.Log("[PlayerFormController]: Player took damage.");
        sfxManager.Play("die");

        if (currentForm == PlayerForm.Shade && currentHP == 1)
        {
            // Die
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
                break;

                case PlayerForm.Bat:
                    // TODO: Bat hurt VFX/SFX
                break;

                case PlayerForm.Rat:
                    // TODO: Rat hurt VFX/SFX
                break;

                case PlayerForm.Spider:
                    // TODO: Spider hurt VFX/SFX
                break;

                case PlayerForm.SkeletonWarrior:
                    // TODO: Skeleton hurt VFX/SFX
                break;

                case PlayerForm.SkeletonArcher:
                    // TODO: Skeleton hurt VFX/SFX
                break;
            }
            currentHP--;
            TimeManager.Instance.HitStopFrames(damagedStopTime);
        }

        giveInvulnerability(damageSafetyTime);
    }

    public void PossessEnemy(PossessionType form, Vector2 newPosition)
    {
        giveInvulnerability(2f);

        currentTransform.DOMove(newPosition, 1.5f).SetEase(Ease.InExpo);

        possessParticle.transform.position = new Vector3(newPosition.x, newPosition.y, -0.5f);
        possessParticle.Play();
        sfxManager.Play("possess");

        switch (form)
        {
            case PossessionType.SkeletonArcher:
                StartCoroutine(ChangeFormRoutine(PlayerForm.SkeletonArcher, newPosition, 1.65f));
            break;

            case PossessionType.SkeletonWarrior:
                StartCoroutine(ChangeFormRoutine(PlayerForm.SkeletonWarrior, newPosition, 1.65f));
            break;

            case PossessionType.Spider:
                StartCoroutine(ChangeFormRoutine(PlayerForm.Spider, newPosition, 1.65f));
            break;

            case PossessionType.Rat:
                StartCoroutine(ChangeFormRoutine(PlayerForm.Rat, newPosition, 1.65f));
                //ChangeForm(PlayerForm.Rat, newPosition);
            break;

            case PossessionType.Bat:
                StartCoroutine(ChangeFormRoutine(PlayerForm.Bat, newPosition, 1.65f));
            break;

            case PossessionType.Alchemist:
            break;

            case PossessionType.None:
            break;
        }
    }

    public void giveInvulnerability(float time)
    {
        StartCoroutine(iFrameRoutine(time));
    }

    public void DisableMovement(IPlayerController controller, float seconds)
    {
        StopCoroutine(DisableMovementRoutine(controller, 0.01f));
        StartCoroutine(DisableMovementRoutine(controller, seconds));
    }

    IEnumerator iFrameRoutine(float iFrameTime)
    {
        canTakeDamage = false;
        yield return new WaitForSeconds(iFrameTime);
        canTakeDamage = true;
    }

    IEnumerator DisableMovementRoutine(IPlayerController controller, float seconds)
    {
        controller.DisableMovement();
        yield return new WaitForSeconds(seconds);
        controller.EnableMovement();
    }

    private void SetCameraTarget(Transform newTarget)
    {
        virtualCamera.m_Follow = newTarget;
        virtualCamera.m_LookAt = newTarget;
    }
}
