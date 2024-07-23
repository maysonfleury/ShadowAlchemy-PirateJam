using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Effect;
using UnityEngine;

public class ShadeController : MonoBehaviour, IEffectable, IMovable
{
    private Collision coll;
    [HideInInspector]
    public Rigidbody2D rb;

    [Space]
    [Header("Stats")]
    public float walkSpeed = 10;
    public float jumpForce = 11;
    public float jumpBufferFrames = 50;
    public float coyoteFrames = 50;
    public float wallSlideSpeed = 1;
    public float wallJumpControl = 3.75f;
    public float dashSpeed = 50;

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

    [Space]
    [Header("Booleans")]
    public bool canMove = true;
    public bool canAttack = true;
    public bool wallJumped;
    public bool wallSlide;
    public bool isDashing;
    public int side = 1;

    [Space]
    [Header("Polish")]
    public ParticleSystem dashParticle;
    public ParticleSystem jumpParticle;
    public ParticleSystem attackParticle;
    public GameObject shadeModel;
    public float rotateTime = 10f;
    //public ParticleSystem wallJumpParticle;
    //public ParticleSystem slideParticle;

    // Private values
    private float xRaw;
    private float yRaw;
    private bool groundTouch;
    private bool hasDashed;
    private bool coyoteEnabled;

    // Start is called before the first frame update
    void Start()
    {
        coll = GetComponent<Collision>();
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        //if (TimeManager.Instance.IsGamePaused)
        //    return;
        if (!canMove)
            return;

        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");
        xRaw = Input.GetAxisRaw("Horizontal");
        yRaw = Input.GetAxisRaw("Vertical");
        Vector2 dir = new Vector2(x, y);

        Movement(dir);
        Aim();

        if (coll.onGround && !isDashing)
        {
            wallJumped = false;
            GetComponent<GravityController>().enabled = true;
        }

        if(coll.onWall && !coll.onGround)
        {
            if (xRaw != 0)
            {
                wallSlide = true;
                WallSlide();
            }
        }

        if (!coll.onWall || coll.onGround)
            wallSlide = false;

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

        if (Input.GetButtonDown("Fire3") && !hasDashed)
        {
            if(xRaw != 0 || yRaw != 0)
                Dash(xRaw, yRaw);
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
        }

        //WallParticle(y);

        if (xRaw == 0 || coll.onWall)
        {
            shadeModel.transform.DOLocalRotate(Vector3.zero, Time.deltaTime * rotateTime);
        }
        else if(xRaw > 0)
        {
            side = 1;
            //anim.Flip(side);
            if (!coll.onWall)
                shadeModel.transform.DOLocalRotate(new Vector3(0, 0, -10), Time.deltaTime * rotateTime);
        }
        else if (xRaw < 0)
        {
            side = -1;
            //anim.Flip(side);
            if (!coll.onWall)
                shadeModel.transform.DOLocalRotate(new Vector3(0, 0, 10), Time.deltaTime * rotateTime);
        }
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
            Vector3 aimDir = (cursorPos - gameObject.transform.position).normalized * 10f;

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

            // Okay now aim
            attackHitbox.transform.localPosition = aimDir;
        }
        else // Keyboard 4-directional aiming
        {
            Vector3 aimDir;

            // Priotize up-down attacks in the air and on wall
            if (!coll.onGround || coll.onWall)
            {
                if (yRaw != 0)
                    aimDir = new Vector3(0, yRaw * attackRange, 0);
                else if (xRaw != 0)
                    aimDir = new Vector3(xRaw * attackRange, 0, 0);
                else // Default to last left-right direction input
                    aimDir = new Vector3(side * attackRange, 0, 0);
            }
            else // Prioritize right-left attacks on the ground
            {
                if (xRaw != 0)
                    aimDir = new Vector3(xRaw * attackRange, 0, 0);
                else if (yRaw != 0)
                    aimDir = new Vector3(0, yRaw * attackRange, 0);
                else // Default to last left-right direction input
                    aimDir = new Vector3(side * attackRange, 0, 0);
            }
            attackHitbox.transform.localPosition = aimDir;
        }
    }

    private void Attack()
    {
        Aim();
        attackHitbox.enabled = true;
        canAttack = false;
        attackParticle.Play();
        Invoke(nameof(ResetAttack), attackCooldown);
        Invoke(nameof(DisableHitbox), 0.2f);
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
    }

    void GroundTouch()
    {
        hasDashed = false;
        isDashing = false;

        //side = anim.sr.flipX ? -1 : 1;

        jumpParticle.Play();
    }

    private void Dash(float x, float y)
    {
        //anim.SetTrigger("dash");

        Camera.main.transform.DOComplete();
        Camera.main.transform.DOShakePosition(.2f, .5f, 14, 90, false, true);
        FindObjectOfType<RippleEffect>().Emit(Camera.main.WorldToViewportPoint(transform.position));

        hasDashed = true;

        rb.velocity = Vector2.zero;
        Vector2 dir = new Vector2(x, y);

        rb.velocity += dir.normalized * dashSpeed;
        StartCoroutine(DashWait());
    }

    IEnumerator DashWait()
    {
        //FindObjectOfType<GhostTrail>().ShowGhost();
        StartCoroutine(GroundDash());
        DOVirtual.Float(14, 0, .8f, RigidbodyDrag);

        dashParticle.Play();
        rb.gravityScale = 0;
        GetComponent<GravityController>().enabled = false;
        wallJumped = true;
        isDashing = true;
        
        //shadeModel.GetComponent<Renderer>().material.DOFade()
        yield return new WaitForSeconds(.3f);

        //dashParticle.Stop();
        rb.gravityScale = 3;
        GetComponent<GravityController>().enabled = true;
        wallJumped = false;
        isDashing = false;
    }

    IEnumerator GroundDash()
    {
        yield return new WaitForSeconds(.15f);
        if (coll.onGround)
            hasDashed = false;
    }

    private void WallJump()
    {
        //if ((side == 1 && coll.onRightWall) || side == -1 && !coll.onRightWall)
        //{
        //    side *= -1;
        //    //anim.Flip(side);
        //}

        StopCoroutine(DisableMovement(0));
        StartCoroutine(DisableMovement(.1f));

        Vector2 wallDir = coll.onRightWall ? Vector2.left : Vector2.right;

        Jump(Vector2.up + (wallDir / 1.5f), true);

        wallJumped = true;
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

    public void DisableMovementForSeconds(float seconds)
    {
        StopCoroutine(DisableMovement(0f));
        StartCoroutine(DisableMovement(seconds));
    }

    IEnumerator DisableMovement(float time)
    {
        canMove = false;
        yield return new WaitForSeconds(time);
        canMove = true;
    }

    void RigidbodyDrag(float x)
    {
        rb.drag = x;
    }


    //********************************
    //*         IEffectable          *
    //********************************

    public void ApplyEffect(EffectSO effectSO)
    {
        throw new NotImplementedException();
    }

    //*****************************
    //*         IMovable          *
    //*****************************

    public void ApplyForce(ForceSO forceSO)
    {
        Debug.Log("ApplyForce called on Player from " + forceSO.name);
        DisableMovementForSeconds(0.1f);
        float new_x = side * forceSO.Direction.x;
        Vector2 velocity = new Vector2(new_x, forceSO.Direction.y).normalized * forceSO.Speed;
        rb.AddForce(velocity, forceSO.ForceMode);
    }

    public void ApplyRelativeForce(float forward, ForceSO forceSO)
    {
        Debug.Log("ApplyRelativeForce called on Player from " + forceSO.name);
        DisableMovementForSeconds(0.1f);
        if(forward == 0)
        {
            forward = 1;
        }
        float new_x = forward * forceSO.Direction.x;
        Vector2 velocity = new Vector2(new_x, forceSO.Direction.y).normalized * forceSO.Speed;
        rb.AddForce(velocity, forceSO.ForceMode);
    }
}