using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ShadeController : MonoBehaviour
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
    public float attackCooldown = 1f;

    [Space]
    [Header("Booleans")]
    public bool canMove;
    public bool canAttack;
    public bool wallJumped;
    public bool wallSlide;
    public bool isDashing;

    [Space]

    private bool groundTouch;
    private bool hasDashed;
    private bool coyoteEnabled;

    public int side = 1;

    [Space]
    [Header("Polish")]
    public ParticleSystem dashParticle;
    public ParticleSystem jumpParticle;
    public ParticleSystem attackParticle;
    //public ParticleSystem wallJumpParticle;
    //public ParticleSystem slideParticle;

    // Start is called before the first frame update
    void Start()
    {
        coll = GetComponent<Collision>();
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");
        float xRaw = Input.GetAxisRaw("Horizontal");
        float yRaw = Input.GetAxisRaw("Vertical");
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
            if (x != 0)
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

        if (wallSlide || !canMove)
            return;

        if(x > 0)
        {
            side = 1;
            //anim.Flip(side);
        }
        if (x < 0)
        {
            side = -1;
            //anim.Flip(side);
        }
    }

    void GroundTouch()
    {
        hasDashed = false;
        isDashing = false;

        //side = anim.sr.flipX ? -1 : 1;

        jumpParticle.Play();
    }

    private void Aim()
    {
        Vector3 cursorPosCam = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 15f));
        Vector3 cursorPos = cursorPosCam + (Camera.main.transform.forward * 15.0f);
        Vector3 aimDir = (cursorPos - gameObject.transform.position).normalized * 10f;
        if (coll.onGround)
            aimDir.y = Mathf.Clamp(aimDir.y, -0.53f, 1.4f);
        else
            aimDir.y = Mathf.Clamp(aimDir.y, -1.4f, 1.4f);
        if (Mathf.Abs(aimDir.y) < 1.2f)
            aimDir.x = Mathf.Clamp(Math.Abs(aimDir.x), 0.8f, 1f) * Mathf.Sign(aimDir.x);
        else
            aimDir.x = Mathf.Clamp(Math.Abs(aimDir.x), 0f, 1f) * Mathf.Sign(aimDir.x);
        aimDir.z = 0;
        Transform attackVisual = attackParticle.transform.parent.transform;
        attackVisual.localPosition = aimDir;
    }

    private void Attack()
    {
        canAttack = false;
        attackParticle.Play();
        Invoke("ResetAttack", attackCooldown);
    }

    private void ResetAttack()
    {
        canAttack = true;
    }

    private void Dash(float x, float y)
    {
        Camera.main.transform.DOComplete();
        Camera.main.transform.DOShakePosition(.2f, .5f, 14, 90, false, true);
        FindObjectOfType<RippleEffect>().Emit(Camera.main.WorldToViewportPoint(transform.position));

        hasDashed = true;

        //anim.SetTrigger("dash");

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
        if ((side == 1 && coll.onRightWall) || side == -1 && !coll.onRightWall)
        {
            side *= -1;
            //anim.Flip(side);
        }

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
            rb.velocity = Vector2.Lerp(rb.velocity, (new Vector2(dir.x * walkSpeed, rb.velocity.y)), wallJumpControl * Time.deltaTime);
        }
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