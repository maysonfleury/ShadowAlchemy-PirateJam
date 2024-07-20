using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BatController : MonoBehaviour
{
    private Collision coll;
    [HideInInspector]
    public Rigidbody2D rb;

    [Space]
    [Header("Stats")]
    public float walkSpeed = 10;
    public float flySpeed = 10;
    public float jumpForce = 11;
    public float jumpCooldown = 0.6f;
    public float gravityStrength = 5f;

    [Space]
    [Header("Booleans")]
    public bool canMove;
    public bool canJump;

    [Space]

    public int side = 1;

    [Space]
    [Header("Polish")]
    public ParticleSystem jumpParticle;

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
                JumpBuffer(50f);
            }
        }

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

    private void Movement(Vector2 dir)
    {
        if (!canMove)
            return;

        if (coll.onGround)
            rb.velocity = new Vector2(dir.x * walkSpeed, rb.velocity.y);
        else
            rb.velocity = new Vector2(dir.x * flySpeed, rb.velocity.y);
    }

    private void Jump(Vector2 dir, bool wall)
    {
        //slideParticle.transform.parent.localScale = new Vector3(ParticleSide(), 1, 1);
        //ParticleSystem particle = wall ? wallJumpParticle : jumpParticle;

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