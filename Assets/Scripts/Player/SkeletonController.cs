using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SkeletonController : MonoBehaviour
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

    [Space]
    [Header("Combat")]
    public Collider2D attackHitbox;
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

    [Space]

    private bool groundTouch;
    private bool coyoteEnabled;

    public int side = 1;

    [Space]
    [Header("Polish")]
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
        //Aim();

        if (coll.onGround)
        {
            GetComponent<GravityController>().enabled = true;
        }

        if (Input.GetButtonDown("Jump"))
        {
            //anim.SetTrigger("jump");

            if (coll.onGround || coyoteEnabled)
                Jump(Vector2.up, false);
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

        if (!canMove)
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

    //******************************
    //*         Attacking          *
    //******************************

    private void Aim()
    {
        // Get direction of cursor in relation to character model
        Vector3 cursorPosCam = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 15f));
        Vector3 cursorPos = cursorPosCam + (Camera.main.transform.forward * 15.0f);
        Vector3 aimDir = (cursorPos - gameObject.transform.position).normalized * 10f;

        // Don't hit the floor beneath you
        if (coll.onGround)
            aimDir.y = Mathf.Clamp(aimDir.y, floorHeight, attackRange);
        else
            aimDir.y = Mathf.Clamp(aimDir.y, feetHeight, attackRange);

        // Don't hit yourself
        if (Mathf.Abs(aimDir.y) < headHeight)
            aimDir.x = Mathf.Clamp(Mathf.Abs(aimDir.x), hitBoxSize, attackRange) * Mathf.Sign(aimDir.x);
        else
            aimDir.x = Mathf.Clamp(Mathf.Abs(aimDir.x), 0f, attackRange) * Mathf.Sign(aimDir.x);
        aimDir.z = 0;

        // Okay now aim
        attackHitbox.enabled = true;
        attackHitbox.transform.localPosition = aimDir;
    }

    private void Attack()
    {
        Aim();
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

    void GroundTouch()
    {
        //side = anim.sr.flipX ? -1 : 1;

        jumpParticle.Play();
    }

    private void Movement(Vector2 dir)
    {
        if (!canMove)
            return;

        rb.velocity = new Vector2(dir.x * walkSpeed, rb.velocity.y);
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
}