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
    public bool mouseAiming;
    public float knockbackForce = 25f;
    public float attackCooldown = 0.55f;
    public float attackRange = 1f;
    public float floorHeight = -0.55f;
    public float feetHeight = -1.4f;
    public float headHeight = 1.2f;
    public float hitBoxSize = 0.8f;

    [Space]
    [Header("Booleans")]
    public bool canMove = true;
    public bool canAttack = true;
    public bool wallGrab;
    public bool wallJumped;
    public bool isSlowed;

    [Space]
    [Header("Polish")]
    public ParticleSystem dashParticle;
    public ParticleSystem jumpParticle;
    public ParticleSystem attackParticle;
    public GameObject spiderModel;
    public int side = 1;
    //public ParticleSystem wallJumpParticle;
    //public ParticleSystem slideParticle;

    // Private values
    private float xRaw, yRaw;
    private Vector3 aimDir;
    public float slowPercent;
    private bool groundTouch;
    private bool coyoteEnabled;
    private RippleEffect camRipple;
    private SFXManager sfxManager;
    private float xAxis;
    private float fallSpeedYDampingChangeThreshold;
    private float wallJumpXDampingChangeThreshold;
    public float wallJumpAmount;

    // Start is called before the first frame update
    void Start()
    {
        coll = GetComponent<Collision>();
        rb = GetComponent<Rigidbody2D>();
        camRipple = FindObjectOfType<RippleEffect>();
        sfxManager = FindObjectOfType<SFXManager>();
        fallSpeedYDampingChangeThreshold = CameraManager.instance.fallSpeedYDampingThreshold;
        wallJumpXDampingChangeThreshold = CameraManager.instance.wallJumpXDampingThreshold;
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
        
        if (wallGrab)
        {
            rb.gravityScale = 0;
            if(Math.Abs(xRaw) > 0.2f) rb.velocity = new Vector2(rb.velocity.x, 0);

            float speedModifier = y > 0 ? .5f : 1;

            rb.velocity = new Vector2(rb.velocity.x, y * (walkSpeed * speedModifier));
        }
        else
        {
            rb.gravityScale = 3;
        }

        if (Input.GetButtonDown("Jump"))
        {
            //anim.SetTrigger("jump");

            if (coll.onGround || coyoteEnabled)
                Jump(Vector2.up, false);
            else if (coll.onWall && !coll.onGround)
                WallJump();
            else
                StartCoroutine(JumpBuffer(jumpBufferFrames));
        }

        if (Input.GetButtonDown("Fire1"))
        {
            //anim.SetTrigger("attack");

            if (canAttack)
                Attack();
        }

        //if (Input.GetButtonDown("Fire3") && !hasDashed)
        //{
        //    if(xRaw != 0 || yRaw != 0)
        //        Dash(xRaw, yRaw);
        //}

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

        if(xRaw > 0)
        {
            side = 1;
            //anim.Flip(side);
        }
        else if (xRaw < 0)
        {
            side = -1;
            //anim.Flip(side);
        }
    }

    void FixedUpdate()
    {
        if (!coll.onGround)
            rb.velocity = new Vector3(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -maxFallSpeed, maxFallSpeed * 5f));
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

            // Don't hit the floor beneath you
            if (coll.onGround)
                aimDir.y = Mathf.Clamp(aimDir.y, floorHeight, attackRange + (attackRange * 0.4f));
            else
                aimDir.y = Mathf.Clamp(aimDir.y, feetHeight, attackRange + (attackRange * 0.4f));

            // Don't hit yourself
            if (Mathf.Abs(aimDir.y) < headHeight)
                aimDir.x = Mathf.Clamp(Mathf.Abs(aimDir.x), hitBoxSize, attackRange) * Mathf.Sign(aimDir.x);
            else
                aimDir.x = Mathf.Clamp(Mathf.Abs(aimDir.x), 0f, attackRange) * Mathf.Sign(aimDir.x);
            aimDir.z = 0;
        }
        else // Keyboard 4-directional aiming
        {
            // Priotize up-down attacks in the air and on wall
            if (!coll.onGround || coll.onWall)
            {
                if (yRaw != 0)
                {
                    if (yRaw > 0)
                        aimDir = new Vector3(0, yRaw * attackRange, 0);
                    else if (yRaw < 0)  // More forgiving hitbox for below player
                        aimDir = new Vector3(0, yRaw * attackRange - 0.2f, 0);
                }
                else if (xRaw != 0)
                    aimDir = new Vector3(xRaw * attackRange, 0, 0);
                else // Default to last left-right direction input
                    aimDir = new Vector3(side * attackRange, 0, 0);
            }
            else // Prioritize right-left attacks on the ground
            {
                if (xRaw != 0)
                    aimDir = new Vector3(xRaw * attackRange, 0, 0);
                else if (yRaw > 0)
                    aimDir = new Vector3(0, yRaw * attackRange, 0);
                else // Default to last left-right direction input
                    aimDir = new Vector3(side * attackRange, 0, 0);
            }
        }

        // Okay now aim
        attackHitbox.transform.localPosition = aimDir;
        attackHitbox.GetComponent<PlayerAttack>().forward = Mathf.Sign(aimDir.x);
    }

    private void Attack()
    {
        Aim();
        attackHitbox.enabled = true;
        canAttack = false;
        attackParticle.Play();
        Invoke(nameof(ResetAttack), attackCooldown);
        Invoke(nameof(DisableHitbox), 0.05f);
    }

    private void ResetAttack()
    {
        canAttack = true;
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
            rb.velocity = new Vector2(rb.velocity.x * slowPercent, rb.velocity.y * slowPercent);
        }
    }

    void GroundTouch()
    {
        //side = anim.sr.flipX ? -1 : 1;

        jumpParticle.Play();
    }

    private void WallJump()
    {
        StopCoroutine(DisableMovement(0));
        StartCoroutine(DisableMovement(10f));

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

    public void DisableMovementForFrames(float frames)
    {
        Debug.Log("[SpiderController]: Disabling movement for " + frames + " frames");
        StopCoroutine(DisableMovement(0f));
        StartCoroutine(DisableMovement(frames));
    }

    IEnumerator DisableMovement(float frames)
    {
        canMove = false;
        for (int i = 0; i < frames; i++)
            yield return new WaitForEndOfFrame();
        canMove = true;
    }

    public void DisableMovementForSeconds(float time)
    {
        Debug.Log("[SpiderController]: Disabling movement for " + time + "s");
        StopCoroutine(DisableMovementTime(0f));
        StartCoroutine(DisableMovementTime(time));
    }

    IEnumerator DisableMovementTime(float time)
    {
        canMove = false;
        yield return new WaitForSeconds(time);
        canMove = true;
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
        Vector2 knockbackDir = new Vector2(transform.position.x, transform.position.y) - damageOrigin;
        Knockback(knockbackDir.x * knockbackForce, knockbackDir.y * knockbackForce);
    }

    public void OnWebEnter(float percentage)
    {
        // Flat 5% slow for spiders in webs
        isSlowed = true;
        slowPercent = (100f - 5) * 0.01f;
        GetComponent<GravityController>().enabled = false;
    }

    public void OnWebExit()
    {
        isSlowed = false;
        slowPercent = 0f;
        GetComponent<GravityController>().enabled = true;
    }

    //********************************
    //*         IEffectable          *
    //********************************

    public void ApplyEffect(EffectSO effectSO)
    {
        if (effectSO.Damage > 0)
            gameObject.transform.parent.GetComponent<PlayerFormController>().DamagePlayer();
        if (effectSO.StunData.Enabled)
            DisableMovementForSeconds(effectSO.StunData.Duration);
        if (effectSO.SlowData.Enabled)
            SlowMovement(effectSO.SlowData.Percent, effectSO.SlowData.Duration);
    }

    //*****************************
    //*         IMovable          *
    //*****************************

    public void ApplyForce(ForceSO forceSO)
    {
        Debug.Log("[SpiderController]: ApplyForce called on Player from " + forceSO.name);
        DisableMovementForSeconds(0.5f);
        float new_x = side * forceSO.Direction.x;
        Vector2 velocity = new Vector2(new_x, forceSO.Direction.y).normalized * forceSO.Speed;
        rb.AddForce(velocity, forceSO.ForceMode);
    }

    public void ApplyRelativeForce(float forward, ForceSO forceSO)
    {
        Debug.Log("[SpiderController]: ApplyRelativeForce called on Player from " + forceSO.name);
        DisableMovementForSeconds(0.5f);
        if(forward == 0)
        {
            forward = 1;
        }
        float new_x = forward * forceSO.Direction.x;
        Vector2 velocity = new Vector2(new_x, forceSO.Direction.y).normalized * forceSO.Speed;
        rb.AddForce(velocity, forceSO.ForceMode);
    }
}