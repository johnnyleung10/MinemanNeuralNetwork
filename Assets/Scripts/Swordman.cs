using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Swordman : PlayerController
{
    [HideInInspector]
    public string[] TileTags = {"TileStone", "TileGrass", "TileDirt", "TileDiamond"};

    private void Start()
    {
        m_CapsulleCollider  = this.transform.GetComponent<CapsuleCollider2D>();
        m_Anim = this.transform.Find("model").GetComponent<Animator>();
        m_rigidbody = this.transform.GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        checkInput();

        if (m_rigidbody.velocity.magnitude > 30)
        {
            m_rigidbody.velocity = new Vector2(m_rigidbody.velocity.x - 0.1f, m_rigidbody.velocity.y - 0.1f);
        }
    }

    IEnumerator BreakBlocks(Vector3 c, System.Action<string> callback)
    {
        yield return new WaitForSeconds(0.50f);

        RaycastHit2D hit2d = Physics2D.Raycast(transform.position, c - transform.position);

        if (hit2d.collider != null)
        {
            if (hit2d.collider.gameObject != null && (BlockDistance(hit2d.collider.gameObject.transform.position.x, m_rigidbody.transform.position.x) <= 2))
            {
                string itemTag = hit2d.collider.gameObject.tag;
                //Debug.Log(itemTag);
                
                foreach (string tag in TileTags)
                {
                    if (tag.Equals(itemTag))
                    {
                        Destroy(hit2d.collider.gameObject);
                        callback(itemTag);
                    }
                }               
            }
        }
    }

    // Animations
    public IEnumerator Move_Attack(Vector3 direction, System.Action<string> callback)
    {
        string itemTag = "";
        if (m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
            callback("");

        RaycastHit2D hit2d = Physics2D.Raycast(transform.position, direction - transform.position);
        if (hit2d.collider != null)
        {
            if (hit2d.collider.gameObject != null && (BlockDistance(hit2d.collider.gameObject.transform.position.x, m_rigidbody.transform.position.x) <= 2))
            {
                Debug.DrawLine(transform.position, hit2d.point, Color.green);
                if (Mathf.FloorToInt(hit2d.collider.gameObject.transform.position.x) < Mathf.FloorToInt(m_rigidbody.transform.position.x))
                {
                    Flip(true);
                }
                else if (Mathf.FloorToInt(hit2d.collider.gameObject.transform.position.x) > Mathf.FloorToInt(m_rigidbody.transform.position.x))
                {
                    Flip(false);
                }
            }
            m_Anim.Play("Attack");

            bool wait = true;
            StartCoroutine(BreakBlocks(direction, (returnValue) =>
            {
                itemTag = returnValue;
                wait = false;
            }));
            while (wait) yield return null;

            callback(itemTag);
        }
        callback("");
    }

    public void Move_Death()
    {
        m_Anim.Play("Die");
    }

    public void Move_Right(float m_MoveX)
    {
        if (isGrounded)
        {
            transform.transform.Translate(Vector2.right * m_MoveX * MoveSpeed * Time.deltaTime);
        }
        else
        {
            transform.transform.Translate(new Vector3(m_MoveX * MoveSpeed * Time.deltaTime, 0, 0));
        }

        Flip(false);
    }

    public void Move_Left(float m_MoveX)
    {
        if (isGrounded)
        {
            transform.transform.Translate(Vector2.right * m_MoveX * MoveSpeed * Time.deltaTime);
        }
        else
        {
            transform.transform.Translate(new Vector3(m_MoveX * MoveSpeed * Time.deltaTime, 0, 0));
        }

        Flip(true);
    }

    public void Move_Jump()
    {
        if (m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
            //return;

        if (currentJumpCount < JumpCount)
        {
            performJump();
        }
    }


    public void checkInput()
    { 
        if (Input.GetKeyDown(KeyCode.S))  
        {
            IsSit = true;
            m_Anim.Play("Sit");
        }
        else if (Input.GetKeyUp(KeyCode.S))
        {
            m_Anim.Play("Idle");
            IsSit = false;
        }

        m_MoveX = Input.GetAxis("Horizontal");
   
        GroundCheckUpdate(); // ADD THIS <---------------


        if (!m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
        {
            if (Input.GetKey(KeyCode.Mouse0))
            {
                Vector3 c = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                RaycastHit2D hit2d = Physics2D.Raycast(transform.position, c - transform.position);

                if (hit2d.collider != null)
                {

                    if (hit2d.collider.gameObject != null && (BlockDistance(hit2d.collider.gameObject.transform.position.x, m_rigidbody.transform.position.x) <= 2))
                    {
                        if (Mathf.FloorToInt(hit2d.collider.gameObject.transform.position.x) < Mathf.FloorToInt(m_rigidbody.transform.position.x))
                        {
                            Flip(true);
                        }
                        else if (Mathf.FloorToInt(hit2d.collider.gameObject.transform.position.x) > Mathf.FloorToInt(m_rigidbody.transform.position.x))
                        {
                            Flip(false);
                        }
                    }
                    m_Anim.Play("Attack");
                    StartCoroutine(BreakBlocks(c, (returnValue) =>
                    {}));
                }
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

        if (Input.GetKey(KeyCode.Alpha1))
        {
            m_Anim.Play("Die");
        }

        if (Input.GetKey(KeyCode.D))
        {
            if (isGrounded)  
            {
                if (m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
                {
                    return;
                }
                transform.transform.Translate(Vector2.right* m_MoveX * MoveSpeed * Time.deltaTime);
            }
            else
            {
                transform.transform.Translate(new Vector3(m_MoveX * MoveSpeed * Time.deltaTime, 0, 0));
            }

            if (m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
                return;

            if (!Input.GetKey(KeyCode.A))
                Flip(false);


        }
        else if (Input.GetKey(KeyCode.A))
        {
            if (isGrounded)
            {
                if (m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
                    return;

                transform.transform.Translate(Vector2.right * m_MoveX * MoveSpeed * Time.deltaTime);
            }
            else
            {
               transform.transform.Translate(new Vector3(m_MoveX * MoveSpeed * Time.deltaTime, 0, 0));
            }


            if (m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
                return;

            if (!Input.GetKey(KeyCode.D))
                Flip(true);

        }


        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
                return;

            if (currentJumpCount < JumpCount)
            {
                performJump();
            }
        }
    }

    protected override void LandingEvent()
    {
        if (!m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Run") && !m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
            m_Anim.Play("Idle");
    }

}
