using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Effect;
using Unity.VisualScripting;
using UnityEngine;

public class RatController : MonoBehaviour, IPlayerController, IEffectable, IMovable
{
    private Collision coll;
    [HideInInspector]
    public Rigidbody2D rb;

    [Space]
    [Header("Movement")]
    public float walkSpeed = 10;
    public float movementSnappiness = 8.5f;
    public float jumpForce = 11;
    public float maxFallSpeed = 10f;
    public float jumpBufferFrames = 4;
    public float coyoteFrames = 3;
    public float wallSlideSpeed = 1;
    public float ledgeHopStrength = 1f;
    public float wallJumpControl = 3.75f;
    //public float dashSpeed = 50;

    [Space]
    [Header("Combat")]
    public Collider2D attackHitbox;
    public bool mouseAiming;
    public float attackCooldown = 1f;
    public float attackRange = 1f;
    public float floorHeight = -0.55f;
    public float feetHeight = -1.4f;
    public float headHeight = 1.2f;
    public float hitBoxSize = 0.8f;
    public float knockbackForce = 12f;

    [Space]
    [Header("Booleans")]
    public bool canMove = true;
    public bool canAttack = true;
    public bool wallJumped;
    public bool wallSlide;
    public bool doubleJumped;
    public bool isSlowed;

    [Space]
    [Header("Polish")]
    public Animator animator;
    public ParticleSystem jumpParticle;
    public ParticleSystem attackParticle;
    public ParticleSystem hurtParticle;
    //public ParticleSystem wallJumpParticle;
    //public ParticleSystem slideParticle;
    public GameObject ratModel;
    public float rotateTime = 0.08f;
    public int side = 1;

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
    public float wallJumpAmount;
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

        if (coll.onGround) //&& !isDashing)
        {
            wallJumped = false;
            doubleJumped = false;
            GetComponent<GravityController>().enabled = true;
        }

        if(coll.onWall && !coll.onGround)
        {
            if (xRaw != 0)
            {
                wallSlide = true;
                WallSlide();
            }
            doubleJumped = false;
        }

        if (!coll.onWall || coll.onGround)
            wallSlide = false;

        if (Input.GetButtonDown("Jump"))
        {
            //anim.SetTrigger("jump");

            if (coll.onGround || coyoteEnabled)
                Jump(Vector2.up, false);
            else if (coll.onWall && !coll.onGround && !isSlowed)
                WallJump();
            else if (!coll.onGround && !doubleJumped)
            {
                DoubleJump(Vector2.up * 1.3f, false);
            }
            else
                StartCoroutine(JumpBuffer(jumpBufferFrames));
        }

        if (Input.GetButtonDown("Fire1"))
        {
            //anim.SetTrigger("attack");

            if (canAttack)
                Attack();
        }

        if (Input.GetButtonDown("Fire3") && !hasKilledSelf)
        {
            hasKilledSelf = true;
            playerFormController.ReturnToShade();
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
            animator.SetBool("isRunning", false);
            animator.SetBool("isGrounded", false);
        }

        if (coll.onGround && !animator.GetBool("isGrounded"))
        {
            animator.SetBool("isGrounded", true);
        }

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

        // Rotate and flip the model depending on which direction the player is moving
        if (xRaw == 0 || coll.onWall)
        {
            if(!DOTween.IsTweening(ratModel.transform))
                ratModel.transform.DOLocalRotate(Vector3.zero, rotateTime).SetEase(Ease.OutExpo);
            animator.SetBool("isRunning", false);
        }
        else if(xRaw > 0)
        {
            side = 1;
            ratModel.transform.localScale = new Vector3(0.9f, 1.05f, 1);
            if (!DOTween.IsTweening(ratModel.transform))
                ratModel.transform.DOLocalRotate(new Vector3(0, 0, -3), rotateTime);
            if (coll.onGround) animator.SetBool("isRunning", true);
        }
        else if (xRaw < 0)
        {
            side = -1;
            ratModel.transform.localScale = new Vector3(-0.9f, 1.05f, 1);
            if (!DOTween.IsTweening(ratModel.transform))
                ratModel.transform.DOLocalRotate(new Vector3(0, 0, 3), rotateTime);
            if (coll.onGround) animator.SetBool("isRunning", true);
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
        hasKilledSelf = false;
        wallJumped = false;
        doubleJumped = false;
        wallSlide = false;
        wallJumpAmount = 0f;
        isSlowed = false;
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
        }
        else // Keyboard 8-directional aiming
        {
            if (!coll.onGround || coll.onWall)
            {
                if (yRaw < 0) // No diagonal down in air
                    aimDir = new Vector2(0, yRaw * attackRange);
                else if (xRaw != 0)
                    aimDir = new Vector2(xRaw * attackRange, yRaw * attackRange);
                else // Default to whichever side you're facing
                    aimDir = new Vector2(side * attackRange, yRaw * attackRange);
            }
            else
            {
                if (xRaw == 0 && yRaw == 0) // Default to whichever side you're facing
                    aimDir = new Vector2(side * attackRange, yRaw * attackRange);
                else
                    aimDir = new Vector2(xRaw * attackRange, yRaw * attackRange);
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

        if (Random.value < 0.5f)
            animator.SetTrigger("attack");
        else
            animator.SetTrigger("clawAttack");
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
            rb.velocity = new Vector2(rb.velocity.x * slowPercent, rb.velocity.y * (slowPercent / 2f));
        }
    }

    void GroundTouch()
    {
        //side = anim.sr.flipX ? -1 : 1;

        jumpParticle.Play();
    }

    private void WallJump()
    {
        //if ((side == 1 && coll.onRightWall) || side == -1 && !coll.onRightWall)
        //{
        //    side *= -1;
        //    //anim.Flip(side);
        //}

        playerFormController.DisableMovement(this, 0.1f);

        Vector2 wallDir = coll.onRightWall ? Vector2.left : Vector2.right;

        Jump((Vector2.up * 1.2f) + (wallDir / 1.5f), true);

        wallJumped = true;
        wallJumpAmount++;
    }

    private void WallSlide()
    {
        if(coll.wallSide != side)
         //anim.Flip(side * -1);

        if (!canMove)
            return;

        // Only activate wallslide if the player is moving downwards.
        // Prevents walls from stopping upwards momentum (and adds ledge hopping for free)
        if (rb.velocity.y > 0)
            return;

        bool pushingWall = false;
        if((rb.velocity.x > 0 && coll.onRightWall) || (rb.velocity.x < 0 && coll.onLeftWall))
        {
            pushingWall = true;
        }
        float push = pushingWall ? 0 : rb.velocity.x;

        rb.velocity = new Vector2(push, -wallSlideSpeed);
    }

    private void Jump(Vector2 dir, bool wall)
    {
        //slideParticle.transform.parent.localScale = new Vector3(ParticleSide(), 1, 1);
        //ParticleSystem particle = wall ? wallJumpParticle : jumpParticle;
        animator.SetTrigger("jump");
        sfxManager.Play("jump");

        coyoteEnabled = false;

        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.velocity += dir * jumpForce;

        //particle.Play();
        jumpParticle.Play(); // remove when adding wallJumpParticle
    }

    private void DoubleJump(Vector2 dir, bool wall)
    {
        //slideParticle.transform.parent.localScale = new Vector3(ParticleSide(), 1, 1);
        //ParticleSystem particle = wall ? wallJumpParticle : jumpParticle;
        animator.SetTrigger("doubleJump");
        sfxManager.Play("jump");

        doubleJumped = true;

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

        //hasDashed = false;

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

        //dashParticle.Play();
        rb.gravityScale = 0;
        GetComponent<GravityController>().enabled = false;
        wallJumped = true;
        //isDashing = true;
        
        //shadeModel.GetComponent<Renderer>().material.DOFade()
        yield return new WaitForSeconds(.3f);

        //dashParticle.Stop();
        rb.gravityScale = 3;
        GetComponent<GravityController>().enabled = true;
        wallJumped = false;
        //isDashing = false;
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
        slowPercent = (100f - percentage) * 0.01f;
        yield return new WaitForSeconds(duration);
        isSlowed = false;
        slowPercent = 0f;
    }

    void RigidbodyDrag(float x)
    {
        if (rb) rb.drag = x;
    }

    IEnumerator DoubleHit(float stunLength)
    {
        Knockback(-aimDir.x, -aimDir.y);
        TimeManager.Instance.HitStopFrames(stunLength);
        sfxManager.Play("hit");

        for (int i = 0; i < stunLength; i++)
        {
           yield return new WaitForEndOfFrame(); 
        }

        Knockback(-aimDir.x, -aimDir.y);
        TimeManager.Instance.HitStopFrames(stunLength);
        sfxManager.Play("hit");
    }


    //**************************************
    //*         IPlayerController          *
    //**************************************

    public void OnHitEnemy(float stunLength)
    {
        Debug.Log("[RatController]: Enemy was hit!");
        StartCoroutine(DoubleHit(stunLength));
    }

    public void OnTakeDamage(Vector2 damageOrigin)
    {
        Debug.Log("[RatController]: Rat Form took damage!");
        animator.SetTrigger("hit");
        hurtParticle.Play();
        Vector2 knockbackDir = new Vector2(transform.position.x, transform.position.y) - damageOrigin;
        Knockback(knockbackDir.x * knockbackForce, knockbackDir.y * knockbackForce);
    }

    public void OnTakeDamage()
    {
        Debug.Log("[RatController]: Rat Form took damage!");
        animator.SetTrigger("hit");
        hurtParticle.Play();
        Knockback(0, 1 * knockbackForce);
    }

    public void OnWebEnter(float percentage)
    {
        percentage *= 0.75f;    // Web slows Rats 25% more than Shade
        isSlowed = true;
        slowPercent = (100f - percentage) * 0.01f;
    }

    public void OnWebExit()
    {
        isSlowed = false;
        slowPercent = 0f;
    }

    public void OnHitSpikes(Vector2 launchTarget, float launchStrength)
    {
        playerFormController.DisableMovement(this, 0.2f);
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
        Debug.Log("[ShadeController]: ApplyForce called on Player from " + forceSO.name);
        playerFormController.DisableMovement(this, 0.2f);
        float new_x = side * forceSO.Direction.x;
        Vector2 velocity = new Vector2(new_x, forceSO.Direction.y).normalized * forceSO.Speed;
        rb.AddForce(velocity, forceSO.ForceMode);
    }

    public void ApplyRelativeForce(float forward, ForceSO forceSO)
    {
        Debug.Log("[ShadeController]: ApplyRelativeForce called on Player from " + forceSO.name);
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