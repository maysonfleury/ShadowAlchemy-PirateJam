using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Effect;
using UnityEngine;

public class BatController : MonoBehaviour, IPlayerController, IEffectable, IMovable
{
    private Collision coll;
    [HideInInspector]
    public Rigidbody2D rb;

    [Space]
    [Header("Stats")]
    
    public float walkSpeed = 2.5f;
    public float flySpeed = 6;
    public float jumpForce = 10f;
    public float jumpCooldown = 0.4f;
    public float movementSnappiness = 8.5f;
    public float maxFallSpeed = 10f;
    public float gravityStrength = 4.5f;

    [Space]
    [Header("Booleans")]
    public bool canMove = true;
    public bool canJump = true;
    public bool isSlowed;

    [Space]

    public int side = 1;

    [Space]
    [Header("Polish")]
    public Animator animator;
    public ParticleSystem jumpParticle;
    public GameObject batModel;
    public float rotateTime = 0.1f;

    // Private values
    private SFXManager sfxManager;
    private PlayerFormController playerFormController;
    private float xRaw, yRaw;
    public float slowPercent;
    private float xAxis;
    private float fallSpeedYDampingChangeThreshold;
    private float wallJumpXDampingChangeThreshold;
    private bool hasKilledSelf = false;

    // Start is called before the first frame update
    void Start()
    {
        coll = GetComponent<Collision>();
        rb = GetComponent<Rigidbody2D>();
        sfxManager = FindObjectOfType<SFXManager>();
        playerFormController = GetComponentInParent<PlayerFormController>();
        fallSpeedYDampingChangeThreshold = CameraManager.instance.fallSpeedYDampingThreshold * 0.5f;
        wallJumpXDampingChangeThreshold = CameraManager.instance.wallJumpXDampingThreshold;
        hasKilledSelf = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!canMove)
            return;
        if (TimeManager.Instance.IsGamePaused)
            return;
        if (GameManager.Instance.State != GameManager.GameState.GameState)
            return;
        
        float y = Input.GetAxis("Vertical");
        xRaw = Input.GetAxisRaw("Horizontal");
        yRaw = Input.GetAxisRaw("Vertical");
        xAxis = Mathf.Lerp(xAxis, xRaw, Time.deltaTime * movementSnappiness);
        Vector2 dir = new Vector2(xAxis, y);

        Movement(dir);

        // Gravity
        rb.velocity += Vector2.up * Physics2D.gravity.y * (gravityStrength - 1) * Time.deltaTime;

        if (Input.GetButtonDown("Jump"))
        {
            //anim.SetTrigger("jump");

            if (canJump)
            {
                Jump(Vector2.up, false);
                Invoke("ResetJump", jumpCooldown);
            }
            else
            {
                JumpBuffer(8f);
            }
        }

        if (Input.GetButtonDown("Fire3") && !hasKilledSelf)
        {
            hasKilledSelf = true;
            playerFormController.ReturnToShade();
        }

        // Dampen the camera's YDamping depending on fall velocity/time
        if (rb.velocity.y < fallSpeedYDampingChangeThreshold && !CameraManager.instance.IsLerpingYDamping && !CameraManager.instance.LerpedFromPlayerFalling)
            CameraManager.instance.LerpYDamping(true);
        if (rb.velocity.y >= 0f && !CameraManager.instance.IsLerpingYDamping && CameraManager.instance.LerpedFromPlayerFalling)
        {
            CameraManager.instance.LerpedFromPlayerFalling = false;
            CameraManager.instance.LerpYDamping(false);
        }

        // Rotate and flip the model depending on which direction the player is moving
        if (xRaw == 0 || coll.onWall)
        {
            if(!DOTween.IsTweening(batModel.transform))
                batModel.transform.DOLocalRotate(Vector3.zero, rotateTime).SetEase(Ease.OutExpo);
        }
        else if(xRaw > 0)
        {
            side = 1;
            batModel.transform.localScale = new Vector3(side, 1, 1);
            if (!DOTween.IsTweening(batModel.transform))
                batModel.transform.DOLocalRotate(new Vector3(0, 0, -7), rotateTime);
        }
        else if (xRaw < 0)
        {
            side = -1;
            batModel.transform.localScale = new Vector3(side, 1, 1);
            if (!DOTween.IsTweening(batModel.transform))
                batModel.transform.DOLocalRotate(new Vector3(0, 0, 7), rotateTime);
        }
    }

    void FixedUpdate()
    {
        if (!coll.onGround)
            rb.velocity = new Vector3(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -maxFallSpeed, maxFallSpeed * 5f));
    }

    public void ResetForm()
    {
        canMove = true;
        canJump = true;
        isSlowed = false;
        hasKilledSelf = false;
        slowPercent = 0f;
    }

    //*****************************
    //*         Movement          *
    //*****************************

    private void Movement(Vector2 dir)
    {
        if (!canMove)
            return;

        if (coll.onGround)
            rb.velocity = new Vector2(dir.x * walkSpeed, rb.velocity.y);
        else
            rb.velocity = new Vector2(dir.x * flySpeed, rb.velocity.y);

        if (isSlowed)
        {
            rb.velocity = new Vector2(rb.velocity.x * slowPercent, rb.velocity.y * slowPercent);
        }
    }

    private void Jump(Vector2 dir, bool wall)
    {
        //slideParticle.transform.parent.localScale = new Vector3(ParticleSide(), 1, 1);
        //ParticleSystem particle = wall ? wallJumpParticle : jumpParticle;
        animator.SetTrigger("flap");
        sfxManager.Play("jump");

        canJump = false;

        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.velocity += dir * jumpForce;

        //particle.Play();
        jumpParticle.Play(); // remove when adding wallJumpParticle
    }

    private void ResetJump()
    {
        canJump = true;
    }

    IEnumerator JumpBuffer(float frameAmount)
    {
        for (int i = 0; i < frameAmount; i++)
        {
            if (canJump)
            {
                Debug.Log("Jump was buffered for " + i + " frames.");
                Jump(Vector2.up, false);
                Invoke("ResetJump", jumpCooldown);
                break;
            }
            else
                yield return new WaitForEndOfFrame();
        }
        //Debug.Log("Jump buffer frameAmount elapsed, no longer buffering jump.");
        yield return null;
    }

    void RigidbodyDrag(float x)
    {
        rb.drag = x;
    }

    public void SlowMovement(float percentage, float duration)
    {
        if (!isSlowed)
        {
            Debug.Log("[ShadeController]: Slowing movement by " + percentage + "% for " + duration + "s");
            StartCoroutine(SlowMovementRoutine(percentage, duration));
        }
        isSlowed = true;
    }

    IEnumerator SlowMovementRoutine(float percentage, float duration)
    {
        isSlowed = true;
        percentage *= 2f;
        if (percentage > 90f) percentage = 90f;
        slowPercent = (100f - percentage) * 0.01f;
        float tempgrav = gravityStrength;
        gravityStrength *= slowPercent;
        yield return new WaitForSeconds(duration);
        rb.gravityScale = tempgrav;
        isSlowed = false;
        slowPercent = 0f;
    }

    //**************************************
    //*         IPlayerController          *
    //**************************************

    public void OnHitEnemy(float stunLength)
    {
        Debug.Log("[BatController]: How did a bat hit an enemy?");
    }

    public void OnTakeDamage(Vector2 damageOrigin)
    {
        // Rip bat :(
    }

    public void OnTakeDamage()
    {
        // Rip bat :(
    }

    public void OnWebEnter(float percentage)
    {
        // Flat 95% slow for bats in webs
        isSlowed = true;
        slowPercent = (100f - 95f) * 0.01f;
    }

    public void OnWebExit()
    {
        isSlowed = false;
        slowPercent = 0f;
    }

    public void OnHitSpikes(Vector2 launchTarget, float launchStrength)
    {
        // Instantly die
    }

    public void DisableMovement()
    {
        canMove = false;
    }

    public void EnableMovement()
    {
        canMove = true;
    }

    //********************************
    //*         IEffectable          *
    //********************************

    public void ApplyEffect(EffectSO effectSO)
    {
        if (effectSO.Damage > 0)
            playerFormController.DamagePlayer();
        if (effectSO.StunData.Enabled)
            playerFormController.DisableMovement(this, effectSO.StunData.Duration);
        if (effectSO.SlowData.Enabled)
            SlowMovement(effectSO.SlowData.Percent, effectSO.SlowData.Duration);
    }

    //*****************************
    //*         IMovable          *
    //*****************************

    public void ApplyForce(ForceSO forceSO)
    {
        Debug.Log("[ShadeController]: ApplyForce called on Player from " + forceSO.name);
        playerFormController.DisableMovement(this, 0.5f);
        float new_x = side * forceSO.Direction.x;
        Vector2 velocity = new Vector2(new_x, forceSO.Direction.y).normalized * forceSO.Speed;
        rb.AddForce(velocity, forceSO.ForceMode);
    }

    public void ApplyRelativeForce(float forward, ForceSO forceSO)
    {
        Debug.Log("[ShadeController]: ApplyRelativeForce called on Player from " + forceSO.name);
        playerFormController.DisableMovement(this, 0.5f);
        if(forward == 0)
        {
            forward = 1;
        }
        float new_x = forward * forceSO.Direction.x;
        Vector2 velocity = new Vector2(new_x, forceSO.Direction.y).normalized * forceSO.Speed;
        rb.AddForce(velocity, forceSO.ForceMode);
    }

    //void WallParticle(float vertical)
    //{
    //    var main = slideParticle.main;
    //
    //    if (wallSlide || (wallGrab && vertical < 0))
    //    {
    //        slideParticle.transform.parent.localScale = new Vector3(ParticleSide(), 1, 1);
    //        main.startColor = Color.white;
    //    }
    //    else
    //    {
    //        main.startColor = Color.clear;
    //    }
    //}
    //
    //int ParticleSide()
    //{
    //    int particleSide = coll.onRightWall ? 1 : -1;
    //    return particleSide;
    //}
}