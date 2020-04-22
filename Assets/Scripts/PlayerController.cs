using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerController :MonoBehaviour
{
    public bool IsSit = false;
    public int currentJumpCount = 0; 
    public bool isGrounded = false;
    public bool OnceJumpRayCheck = false;
    public bool isLeft = true;

    public bool Is_DownJump_GroundCheck = false; 
    protected float m_MoveX;
    public Rigidbody2D m_rigidbody;
    protected CapsuleCollider2D m_CapsulleCollider;
    protected Animator m_Anim;

    [Header("[Setting]")]
    public float MoveSpeed = 6;
    public int JumpCount = 1;
    public float jumpForce = 15f;

    protected void AnimUpdate()
    {
        if (!m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
        {
            if (Input.GetKey(KeyCode.Mouse0))
            {
                m_Anim.Play("Attack");
            }
            else
            {
                if (m_MoveX == 0)
                {
                    if (!OnceJumpRayCheck)
                        m_Anim.Play("Idle");
                }
                else
                {
                    m_Anim.Play("Run");
                }
            }
        }
    }

    protected int BlockDistance(float x1, float x2)
    {
        return (Mathf.Abs(Mathf.RoundToInt(x1) - Mathf.RoundToInt(x2)));
    }

    protected void Flip(bool bLeft)
    {
        if (bLeft)
        {
            isLeft = true;
        } else
        {
            isLeft = false;
        }
        transform.localScale = new Vector3(bLeft ? 1 : -1, 1, 1);
    }


    protected void performJump()
    {
        m_Anim.Play("Jump");

        m_rigidbody.velocity = new Vector2(0, 0);

        m_rigidbody.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        OnceJumpRayCheck = true;
        isGrounded = false;

        currentJumpCount++;
    }


    IEnumerator GroundCapsulleColliderTimmerFuc()
    {
        yield return new WaitForSeconds(0.3f);
        m_CapsulleCollider.enabled = true;
    }


    Vector2 RayDir = Vector2.down;

    float PretmpY;
    float GroundCheckUpdateTic = 0;
    float GroundCheckUpdateTime = 0.01f;
    protected void GroundCheckUpdate()
    {
        if (!OnceJumpRayCheck)
            return;

        GroundCheckUpdateTic += Time.deltaTime;


        if (GroundCheckUpdateTic > GroundCheckUpdateTime)
        {
            GroundCheckUpdateTic = 0;

            if (PretmpY == 0)
            {
                PretmpY = transform.position.y;
                return;
            }

            float reY = transform.position.y - PretmpY;

            if (reY <= 0)
            {
                if (isGrounded)
                {
                    LandingEvent();
                    OnceJumpRayCheck = false;
                }
            }
            PretmpY = transform.position.y;
        }
    }



    protected abstract void LandingEvent();


}
