using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Effect;
using UnityEngine;

public class SpiderController : MonoBehaviour, IPlayerController, IEffectable, IMovable
{
    private Collision coll;
    [HideInInspector]
    public Rigidbody2D rb;

    [Space]
    [Header("Stats")]
    public float walkSpeed = 10;
    public float movementSnappiness = 8.5f;
    public float jumpForce = 14;
    public float maxFallSpeed = 15f;
    public float jumpBufferFrames = 4;
    public float coyoteFrames = 3;
    public float wallJumpControl = 5f;
    public float ledgeHopStrength = 8.5f;

    [Space]
    [Header("Combat")]
    public Collider2D attackHitbox;
    public Transform rangedAttackOrigin;
    public GameObject webAttackPrefab;
    public bool mouseAiming;
    public float knockbackForce = 25f;
    public float attackCooldown = 0.55f;
    public float rangedAttackCooldown = 3f;
    public float rangedAttackForce = 3f;
    public float attackRange = 1f;
    public float floorHeight = -0.55f;
    public float feetHeight = -1.4f;
    public float headHeight = 1.2f;
    public float hitBoxSize = 0.8f;

    [Space]
    [Header("Booleans")]
    public bool canMove = true;
    public bool canAttack = true;
    public bool canWebAttack = true;
    public bool wallGrab;
    public bool wallJumped;
    public bool isSlowed;
    public bool isInWeb;

    [Space]
    [Header("Polish")]
    public Animator animator;
    public ParticleSystem dashParticle;
    public ParticleSystem jumpParticle;
    public ParticleSystem attackParticle;
    public ParticleSystem hurtParticle;
    public GameObject spiderModel;
    public int side = 1;
    //public ParticleSystem wallJumpParticle;
    //public ParticleSystem slideParticle;

    // Private values
    private float xRaw, yRaw;
    private Vector2 aimDir;
    public float slowPercent;
    private bool groundTouch;
    private bool coyoteEnabled;
    private RippleEffect camRipple;
    private SFXManager sfxManager;
    private PlayerFormController playerFormController;
    private float xAxis;
    private float fallSpeedYDampingChangeThreshold;
    private float wallJumpXDampingChangeThreshold;
    private float wallJumpAmount;
    private bool hasKilledSelf;

    // Start is called before the first frame update
    void Start()
    {
        coll = GetComponent<Collision>();
        rb = GetComponent<Rigidbody2D>();
        camRipple = FindObjectOfType<RippleEffect>();
        sfxManager = FindObjectOfType<SFXManager>();
        playerFormController = GetComponentInParent<PlayerFormController>();
        fallSpeedYDampingChangeThreshold = CameraManager.instance.fallSpeedYDampingThreshold;
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
        Aim();

        if (coll.onGround)
        {
            wallJumped = false;
            wallJumpAmount = 0;
            GetComponent<GravityController>().enabled = true;
        }

        if (coll.onWall && canMove)
        {
            //if(side != coll.wallSide)
                //anim.Flip(side*-1);
            wallGrab = true;
        }

        if (!coll.onWall) wallGrab = false;

        if (!coll.onGround && !coll.onWall && coll.onLedge && wallJumped)
        {
            Debug.Log("Ledge Hop!");
            Vector2 dirr = new Vector2(side, 1);
            rb.velocity = dirr * ledgeHopStrength;
        }

        if (coll.onGround && coll.onWall)
        {
            if (y > 0)
                wallGrab = true;
            else
                wallGrab = false;
        }
        
        // Spider on the wall or in its web
        if (wallGrab || isInWeb)
        {
            rb.gravityScale = 0;
            if(Math.Abs(xRaw) > 0.2f) rb.velocity = new Vector2(rb.velocity.x, 0);

            rb.velocity = new Vector2(rb.velocity.x, y * (walkSpeed / 1.5f));
        }
        else
        {
            rb.gravityScale = 3;
        }

        if (Input.GetButtonDown("Jump"))
        {
            //anim.SetTrigger("jump");

            if (coll.onGround || coyoteEnabled || isInWeb)
                Jump(Vector2.up, false);
            else if (coll.onWall && !coll.onGround)
                WallJump();
            else
                StartCoroutine(JumpBuffer(jumpBufferFrames));
        }

        if (Input.GetButtonDown("Fire1"))
        {
            if (canAttack)
                JumpAttack();
        }

        if (Input.GetButtonDown("Fire2"))
        {
            if (canWebAttack)
                WebAttack();
        }

        if (Input.GetButtonDown("Fire3") && !hasKilledSelf)
        {
            hasKilledSelf = true;
            playerFormController.ReturnToShade();
        }

        if (coll.onGround && !groundTouch)
        {
            GroundTouch();
            groundTouch = true;
            coyoteEnabled = false;
        }

        if(!coll.onGround && groundTouch)
        {
            groundTouch = false;
            StartCoroutine(CoyoteTime(coyoteFrames));
            animator.SetBool("isRunning", false);
            animator.SetBool("isGrounded", false);
        }

        //WallParticle(y);

        // Dampen the camera's YDamping depending on fall velocity/time
        if (rb.velocity.y < fallSpeedYDampingChangeThreshold && !CameraManager.instance.IsLerpingYDamping && !CameraManager.instance.LerpedFromPlayerFalling)
            CameraManager.instance.LerpYDamping(true);
        if (rb.velocity.y >= 0f && !CameraManager.instance.IsLerpingYDamping && CameraManager.instance.LerpedFromPlayerFalling)
        {
            CameraManager.instance.LerpedFromPlayerFalling = false;
            CameraManager.instance.LerpYDamping(false);
        }

        // Dampen the camera's XDamping if the player is wallJumping
        if (wallJumpAmount >= wallJumpXDampingChangeThreshold && !CameraManager.instance.IsLerpingXDamping && !CameraManager.instance.LerpedFromPlayerWallJumping)
            CameraManager.instance.LerpXDamping(true);
        if (wallJumpAmount < wallJumpXDampingChangeThreshold && !CameraManager.instance.IsLerpingXDamping && CameraManager.instance.LerpedFromPlayerWallJumping)
        {
            CameraManager.instance.LerpedFromPlayerWallJumping = false;
            CameraManager.instance.LerpXDamping(false);
        }

        if (xRaw == 0 || coll.onWall)
        {
            animator.SetBool("isRunning", false);
        }
        else if(xRaw > 0)
        {
            side = 1;
            spiderModel.transform.localScale = new Vector3(2, 2, 1);
            animator.SetBool("isRunning", true);
        }
        else if (xRaw < 0)
        {
            side = -1;
            spiderModel.transform.localScale = new Vector3(-2, 2, 1);
            animator.SetBool("isRunning", true);
        }

        if (coll.onWall && !coll.onGround)
        {
            if (yRaw != 0)
            {
                animator.SetBool("isCrawling", true);
                animator.SetBool("isCrawlingIdle", false);
            }
            else
                animator.SetBool("isCrawlingIdle", true);

            if (coll.onLeftWall)
            {
                side = 1;
                spiderModel.transform.localScale = new Vector3(2, 2, 1);
            }
            else if (coll.onRightWall)
            {
                side = -1;
                spiderModel.transform.localScale = new Vector3(-2, 2, 1);
            }
        }
        else if (coll.onGround)
        {
            animator.SetBool("isCrawling", false);
            animator.SetBool("isCrawlingIdle", false);
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
        canAttack = true;
        canWebAttack = true;
        hasKilledSelf = false;
        wallJumped = false;
        wallGrab = false;
        wallJumpAmount = 0f;
        isSlowed = false;
        isInWeb = false;
        slowPercent = 0f;
    }

    //******************************
    //*         Attacking          *
    //******************************

    private void Aim()
    {
        if (mouseAiming) // Mouse 360-degree aiming
        {
            // Get direction of cursor in relation to character model
            Vector3 cursorPosCam = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 15f));
            Vector3 cursorPos = cursorPosCam + (Camera.main.transform.forward * 15.0f);
            aimDir = (cursorPos - gameObject.transform.position).normalized * 10f;

            aimDir.y = Mathf.Clamp(aimDir.y, feetHeight, attackRange);
            if (Mathf.Abs(aimDir.y) < headHeight)
                aimDir.x = Mathf.Clamp(Mathf.Abs(aimDir.x), hitBoxSize, attackRange) * Mathf.Sign(aimDir.x);
            else
                aimDir.x = Mathf.Clamp(Mathf.Abs(aimDir.x), 0f, attackRange) * Mathf.Sign(aimDir.x);
        }
        else // Keyboard 8-directional aiming
        {
            if (xRaw == 0 && yRaw == 0) // Default to whichever side you're facing
                aimDir = new Vector3(side * attackRange, yRaw * attackRange, 0);
            else
                aimDir = new Vector3(xRaw * attackRange, yRaw * attackRange, 0);
        }

        // Okay now aim
        attackHitbox.transform.localPosition = aimDir;
        attackHitbox.GetComponent<PlayerAttack>().forward = Mathf.Sign(aimDir.x);
    }

    private void JumpAttack()
    {
        playerFormController.DisableMovement(this, 0.5f);

        animator.SetTrigger("jumpAttack");
        sfxManager.Play("jump");

        rb.velocity = new Vector2(0, -rb.velocity.y);
        rb.velocity += new Vector2(side, 1f) * jumpForce;

        attackHitbox.enabled = true;
        canAttack = false;

        attackParticle.Play();
        jumpParticle.Play();

        Invoke(nameof(ResetAttack), attackCooldown);
        Invoke(nameof(DisableHitbox), 0.5f);
    }

    private void ResetAttack()
    {
        canAttack = true;
    }

    private void WebAttack()
    {
        Aim();
        canWebAttack = false;
        attackParticle.Play();
        Invoke(nameof(ResetRangedAttack), attackCooldown);
        animator.SetTrigger("jumpAttack");
        GameObject web = Instantiate(webAttackPrefab, attackHitbox.transform.position, Quaternion.identity);
        web.GetComponent<Rigidbody2D>().AddForce(aimDir * rangedAttackForce, ForceMode2D.Impulse);
    }

    private void ResetRangedAttack()
    {
        canWebAttack = true;
    }

    private void DisableHitbox()
    {
        attackHitbox.enabled = false;
    }


    //*****************************
    //*         Movement          *
    //*****************************
    
    private void Movement(Vector2 dir)
    {
        if (!canMove)
            return;

        if (!wallJumped)
        {
            rb.velocity = new Vector2(dir.x * walkSpeed, rb.velocity.y);
        }
        else
        {
            rb.velocity = Vector2.Lerp(rb.velocity, new Vector2(dir.x * walkSpeed, rb.velocity.y), wallJumpControl * Time.deltaTime);
        }

        if (isSlowed)
        {
            rb.velocity = new Vector2(rb.velocity.x * slowPercent, rb.velocity.y * (slowPercent / 2f));
        }
    }

    void GroundTouch()
    {
        jumpParticle.Play();
        animator.SetBool("isGrounded", true);
    }

    private void WallJump()
    {
        playerFormController.DisableMovement(this, 0.1f);

        wallJumped = true;

        Vector2 wallDir = coll.onRightWall ? Vector2.left : Vector2.right;

        Jump(Vector2.up + (wallDir / 2f), true);
        
        wallJumpAmount++;
    }

    private void Jump(Vector2 dir, bool wall)
    {
        //slideParticle.transform.parent.localScale = new Vector3(ParticleSide(), 1, 1);
        //ParticleSystem particle = wall ? wallJumpParticle : jumpParticle;
        sfxManager.Play("jump");

        coyoteEnabled = false;

        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.velocity += dir * jumpForce;

        //particle.Play();
        jumpParticle.Play(); // remove when adding wallJumpParticle
    }

    IEnumerator JumpBuffer(float frameAmount)
    {
        for (int i = 0; i < frameAmount; i++)
        {
            if (coll.onGround)
            {
                Debug.Log("Jump was buffered for " + i + " frames.");
                Jump(Vector2.up, false);
                break;
            }
            else
                yield return new WaitForEndOfFrame();
        }
        //Debug.Log("Jump buffer frameAmount elapsed, no longer buffering jump.");
        yield return null;
    }

    IEnumerator CoyoteTime(float frameAmount)
    {
        coyoteEnabled = true;
        for (int i = 0; i < frameAmount; i++)
        {
            if (coyoteEnabled == false)
            {
                Debug.Log("Coyote Time activated after " + i + " frames.");
                break;
            }
            else
                yield return new WaitForEndOfFrame();
        }
        //Debug.Log("Coyote Time elapsed, can no longer jump.");
        coyoteEnabled = false;
        yield return null;
    }

    private void Knockback(float x, float y)
    {
        //anim.SetTrigger("dash");

        camRipple.waveSpeed = 3.75f;
        camRipple.Emit(Camera.main.WorldToViewportPoint(transform.position));

        float xVel;
        if (x == 0) xVel = rb.velocity.x;
        else if (x > 0 && rb.velocity.x > 0) xVel = rb.velocity.x;
        else if (x < 0 && rb.velocity.x < 0) xVel = rb.velocity.x;
        else xVel = x;

        rb.velocity = Vector2.zero;
        Vector2 dir = new Vector2(x, y);
        Vector2 add = new Vector2(xVel, 0);

        rb.velocity += dir.normalized * knockbackForce;
        rb.velocity += add;
        StartCoroutine(KnockbackWait());
    }

    IEnumerator KnockbackWait()
    {
        //StartCoroutine(GroundDash());
        if (rb) DOVirtual.Float(14, 0, .8f, RigidbodyDrag);

        dashParticle.Play();
        rb.gravityScale = 0;
        GetComponent<GravityController>().enabled = false;
        wallJumped = true;
        
        //shadeModel.GetComponent<Renderer>().material.DOFade()
        yield return new WaitForSeconds(.3f);

        //dashParticle.Stop();
        rb.gravityScale = 3;
        GetComponent<GravityController>().enabled = true;
        wallJumped = false;
    }

    public void SlowMovement(float percentage, float duration)
    {
        if (!isSlowed)
        {
            Debug.Log("[SpiderController]: Slowing movement by " + percentage + "% for " + duration + "s");
            StartCoroutine(SlowMovementRoutine(percentage, duration));
        }
        isSlowed = true;
    }

    IEnumerator SlowMovementRoutine(float percentage, float duration)
    {
        isSlowed = true;
        slowPercent = (100f - percentage) * 0.01f;
        rb.gravityScale *= slowPercent;
        GetComponent<GravityController>().enabled = false;
        yield return new WaitForSeconds(duration);
        rb.gravityScale = 3f;
        GetComponent<GravityController>().enabled = true;
        isSlowed = false;
        slowPercent = 0f;
    }

    void RigidbodyDrag(float x)
    {
        if (rb) rb.drag = x;
    }


    //**************************************
    //*         IPlayerController          *
    //**************************************

    public void OnHitEnemy(float stunLength)
    {
        Debug.Log("[SpiderController]: Enemy was hit!");
        Knockback(-aimDir.x, -aimDir.y);
        TimeManager.Instance.HitStopFrames(stunLength);
        sfxManager.Play("hit");
    }

    public void OnTakeDamage(Vector2 damageOrigin)
    {
        animator.SetTrigger("hit");
        hurtParticle.Play();
        Vector2 knockbackDir = new Vector2(transform.position.x, transform.position.y) - damageOrigin;
        Knockback(knockbackDir.x * knockbackForce, knockbackDir.y * knockbackForce);
    }

    public void OnWebEnter(float percentage)
    {
        // Flat 10% slow for spiders in webs
        isSlowed = true;
        slowPercent = (100f - 10) * 0.01f;
        GetComponent<GravityController>().enabled = false;
        isInWeb = true;
        animator.SetBool("isCrawling", true);
    }

    public void OnWebExit()
    {
        isSlowed = false;
        slowPercent = 0f;
        GetComponent<GravityController>().enabled = true;
        isInWeb = false;
        animator.SetBool("isCrawling", false);
    }

    public void OnHitSpikes(Vector2 launchTarget, float launchStrength)
    {
        playerFormController.DisableMovement(this, 0.5f);
        rb.velocity *= 0.2f;
        rb.velocity += launchTarget * launchStrength;
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
        Debug.Log("[SpiderController]: ApplyForce called on Player from " + forceSO.name);
        playerFormController.DisableMovement(this, 0.2f);
        float new_x = side * forceSO.Direction.x;
        Vector2 velocity = new Vector2(new_x, forceSO.Direction.y).normalized * forceSO.Speed;
        rb.AddForce(velocity, forceSO.ForceMode);
    }

    public void ApplyRelativeForce(float forward, ForceSO forceSO)
    {
        Debug.Log("[SpiderController]: ApplyRelativeForce called on Player from " + forceSO.name);
        playerFormController.DisableMovement(this, 0.2f);
        if(forward == 0)
        {
            forward = 1;
        }
        float new_x = forward * forceSO.Direction.x;
        Vector2 velocity = new Vector2(new_x, forceSO.Direction.y).normalized * forceSO.Speed;
        rb.AddForce(velocity, forceSO.ForceMode);
    }
}