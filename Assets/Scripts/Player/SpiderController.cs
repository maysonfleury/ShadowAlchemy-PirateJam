using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SpiderController : MonoBehaviour
{
    private Collision coll;
    [HideInInspector]
    public Rigidbody2D rb;

    [Space]
    [Header("Stats")]
    public float walkSpeed = 10;
    public float jumpForce = 50;
    public float jumpBufferFrames = 50;
    public float coyoteFrames = 50;
    public float wallJumpControl = 3.75f;
    public float jumpAttackSpeed = 20;

    [Space]
    [Header("Booleans")]
    public bool canMove;
    public bool wallGrab;
    public bool wallJumped;
    public bool isDashing;

    [Space]

    private bool groundTouch;
    private bool hasDashed;

    public int side = 1;

    //[Space]
    //[Header("Polish")]
    public ParticleSystem dashParticle;
    public ParticleSystem jumpParticle;
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

        Walk(dir);

        if (coll.onWall && canMove)
        {
            if(side != coll.wallSide)
                //anim.Flip(side*-1);
            wallGrab = true;
        }

        if (!coll.onWall || !canMove)
        {
            wallGrab = false;
        }

        if (coll.onGround && !isDashing)
        {
            wallJumped = false;
            GetComponent<GravityController>().enabled = true;
        }

        if (coll.onGround && coll.onWall)
        {
            if (y > 0)
                wallGrab = true;
            else
                wallGrab = false;
        }
        
        if (wallGrab && !isDashing)
        {
            rb.gravityScale = 0;
            if(x > .2f || x < -.2f)
            rb.velocity = new Vector2(rb.velocity.x, 0);

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

            if (coll.onGround)
                Jump(Vector2.up, false);
            if (coll.onWall && !coll.onGround)
                WallJump();
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
        }

        if(!coll.onGround && groundTouch)
        {
            groundTouch = false;
        }

        //WallParticle(y);

        if (wallGrab || !canMove)
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

    private void Dash(float x, float y)
    {
        //Camera.main.transform.DOComplete();
        //Camera.main.transform.DOShakePosition(.2f, .5f, 14, 90, false, true);
        //FindObjectOfType<RippleEffect>().Emit(Camera.main.WorldToViewportPoint(transform.position));

        hasDashed = true;

        //anim.SetTrigger("dash");

        rb.velocity = Vector2.zero;
        Vector2 dir = new Vector2(x, y);

        rb.velocity += dir.normalized * jumpAttackSpeed;
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

        dashParticle.Stop();
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

    private void Walk(Vector2 dir)
    {
        if (!canMove)
            return;

        if (wallGrab)
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

        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.velocity += dir * jumpForce;

        jumpParticle.Play(); // remove when adding wallJumpParticle
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