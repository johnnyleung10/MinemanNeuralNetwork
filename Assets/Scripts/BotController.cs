using System;
using System.Collections.Generic;
using UnityEngine;

public class BotController : PlayerController
{
    public GameObject map;

    [HideInInspector]
    private string[] TileTags = { "TileStone", "TileGrass", "TileDirt", "TileDiamond" };

    public int genomeNum;

    private Vector3 startPosition;
    private float[] input = new float[3];
    public NeuralNetwork network;

    public bool Alive;

    public float timeSinceStart = 0f;
    public float timeSinceLastDiamond = 0f;

    [Header("AI Moves")]
    private float MoveTime;
    private float MoveDirection;
    private int MineDirection;

    [Header("Fitness")]
    public float overallFitness;
    public float dirtMultiplier = 0.2f;
    public float stoneMultiplier = 1.0f;
    public float diamondMultiplier = 20.0f;

    public float totalDiamondsMined;
    public float totalStoneMined;
    public float totalDirtMined;

    private float SwingCoolDown = 0.5f;
    private float SwingCoolDownTimer = 0f;

    private Dictionary<int, Transform> diamondsMap = new Dictionary<int, Transform>();
    private float closestDiamond = 20f;
    private Vector3 closestDiamondPos;
    private float HORIZONTAL_MULTIPLIER = 0.75f;
    private float VERTICAL_MULTIPLiER = 1.0f;

    private float defaultGravity;

    // Initialize
    private void Awake()
    {
        m_CapsulleCollider = this.transform.GetComponent<CapsuleCollider2D>();
        m_Anim = this.transform.Find("model").GetComponent<Animator>();
        m_rigidbody = this.transform.GetComponent<Rigidbody2D>();

        startPosition = transform.position;
        Alive = false;
        defaultGravity = this.GetComponent<Rigidbody2D>().gravityScale;
        closestDiamondPos = new Vector3(0, 0, 0);
    }

    // List all diamonds on current map
    private void ListDiamonds()
    {
        closestDiamondPos = new Vector3(0, 0, 0);
        diamondsMap.Clear();
        foreach (Transform tr in this.transform.parent)
        {
            if (tr.tag == "Map")
            {
                foreach (Transform tile in tr)
                {
                    if (tile.tag == "TileDiamond")
                    {
                        diamondsMap.Add(tile.GetInstanceID(), tile);
                    }
                }
                break;
            }
        }
    }

    // Find the closest diamond to the AI
    private void NearestDiamond()
    {
        foreach (KeyValuePair<int, Transform> diamondEntry in diamondsMap)
        {
            if (diamondEntry.Value == null)
            {
                continue;
            }
            if (DistanceScore(diamondEntry.Value.position, this.transform.position) < DistanceScore(closestDiamondPos, this.transform.position))
            {
                closestDiamond = Vector3.Distance(this.transform.position, diamondEntry.Value.position);
                closestDiamondPos = diamondEntry.Value.position;
            }
        }
    }

    // Computes a distance score to a point, vertical movement is weighted more heavily than horizontal movement
    private float DistanceScore(Vector3 target, Vector3 orig)
    {
        float horizontalScore = HORIZONTAL_MULTIPLIER * Math.Abs(target.x - orig.x);
        float verticalScore = VERTICAL_MULTIPLiER * Math.Abs(target.y - orig.y);
        float totalScore = horizontalScore + verticalScore;
        return totalScore;
    }

    // Reset
    public void Reset()
    {
        timeSinceStart = 0f;
        totalDiamondsMined = 0f;
        totalStoneMined = 0f;
        totalDirtMined = 0f;
        overallFitness = 0f;
        transform.position = startPosition;

        timeSinceLastDiamond = 0f;
        overallFitness = 0f;

        closestDiamond = 100f;
        closestDiamondPos = new Vector3(0, 0, 0);

        ResetMap();
        this.GetComponent<Rigidbody2D>().gravityScale = defaultGravity;
    }

    // Called from NNManager
    public void ResetWithNetwork()
    {
        Reset();
        Alive = true;
    }

    // Respawn a new map
    public void ResetMap()
    {
        Transform t = this.transform.parent;
        GameObject currMap;
        Vector3 temp = new Vector3(0, 0, 0);
        foreach (Transform tr in t)
        {
            if (tr.tag.Equals("Map"))
            {
                currMap = tr.gameObject;
                temp = currMap.transform.position;
                Destroy(currMap);
            }
        }

        map.transform.position = temp;
        GameObject newMap = Instantiate(map);
        newMap.transform.parent = transform.parent;
        ListDiamonds();
    }

    private void FixedUpdate()
    {
        if (Alive)
        {
            // Check if fallen off map
            if (transform.position.y < 0.0f)
            {
                this.GetComponent<Rigidbody2D>().gravityScale = 0f;
                transform.position = startPosition;
                Death();
            }

            NearestDiamond();

            timeSinceStart += Time.deltaTime;
            timeSinceLastDiamond += Time.deltaTime;

            // Feed Input
            closestDiamond = Vector3.Distance(this.transform.position, closestDiamondPos);
            Vector2 direction = closestDiamondPos - this.transform.position;
            float closestDiamondX = Mathf.Abs(this.transform.position.x - closestDiamondPos.x);
            float closestDiamondY = Mathf.Abs(this.transform.position.y - closestDiamondPos.y);
            // Nearest Diamond Distance
            input[0] = closestDiamondX; 
            input[1] = closestDiamondY;
            input[2] = Vector2.Angle(Vector2.right, direction);

            // Network Output
            float[] output = network.FeedForward(input);

            // Cooldown timer
            SwingCoolDownTimer = checkCoolDown(SwingCoolDownTimer);
            if (SwingCoolDownTimer == 0)
            {
                MoveTime = 0.2f;

                float maxVal = Mathf.Max(output[0], output[1], output[2]);
                if (maxVal == output[0])
                {
                    MineDirection = -1;
                }
                else if (maxVal == output[1])
                {
                    MineDirection = 0;
                }
                else if (maxVal == output[2])
                {
                    MineDirection = 1;
                }

                MoveDirection = MineMiner(MineDirection);
                SwingCoolDownTimer = SwingCoolDown;
            }
            MoveMiner(MoveDirection, 1.0f, MoveTime);
            MoveTime -= Time.deltaTime;

            CalculateFitness();
        }
    }

    private float checkCoolDown(float coolDownTimer)
    {
        if (coolDownTimer > 0)
        {
            coolDownTimer -= Time.deltaTime;
        }
        if (coolDownTimer < 0)
        {
            coolDownTimer = 0;
        }
        return coolDownTimer;
    }

    public void Death()
    {
        Alive = false;
        m_Anim.Play("Die");
        GameObject.FindObjectOfType<NNManager>().Deaths += 1;
    }

    private void CalculateFitness()
    {    
        if (timeSinceStart > 20 && overallFitness < 10)
        {
            Death();
        }
        if (timeSinceLastDiamond > 60)
        {
            Death();
        }
    }

    private void MoveMiner(float moveDirection, float sensitivity, float time)
    {
        if (time > 0)
        {
            GroundCheckUpdate();
            if (moveDirection > 0)
            {
                Move_Right(sensitivity);
            }
            else if (moveDirection < 0)
            {
                Move_Left(-sensitivity);
            }
        }
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

    private int MineMiner(int directionSwing)
    {
        int retVal = 0;
        Vector3 changeDir;
        if (directionSwing == -1)
        {
            changeDir = new Vector3(-1, 0, 0);
            retVal = -1;
        }

        else if (directionSwing == 0)
        {
            changeDir = new Vector3(0, -1, 0);
        }
        else if (directionSwing == 1)
        {
            changeDir = new Vector3(1, 0, 0);
            retVal = 1;
        }
        else
        {
            changeDir = new Vector3(0, 1, 0);
        }

        string returnVal = Mine(changeDir + this.transform.position);

        if (returnVal.Equals("TileDirt"))
        {
            totalDirtMined++;
            overallFitness += dirtMultiplier;
        }
        if (returnVal.Equals("TileStone"))
        {
            totalStoneMined++;
            overallFitness += stoneMultiplier;
        }
        else if (returnVal.Equals("TileDiamond"))
        {
            totalDiamondsMined++;
            timeSinceLastDiamond = 0;
            closestDiamond = 100f;
            overallFitness += diamondMultiplier;
        }

        return retVal;
    }

    private string Mine(Vector3 directionSwing)
    {
        string itemTag;
        if (m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
            return "";

        RaycastHit2D hit2d = Physics2D.Raycast(transform.position, directionSwing - transform.position);
        if (hit2d.collider != null)
        {

            if (hit2d.collider.gameObject != null && (BlockDistance(hit2d.collider.gameObject.transform.position.x, m_rigidbody.transform.position.x) <= 2))
            {
                m_Anim.Play("Attack");
                Debug.DrawLine(transform.position, hit2d.point, Color.green);
                if (Mathf.FloorToInt(hit2d.collider.gameObject.transform.position.x) < Mathf.FloorToInt(m_rigidbody.transform.position.x))
                {
                    Flip(true);
                }
                else if (Mathf.FloorToInt(hit2d.collider.gameObject.transform.position.x) > Mathf.FloorToInt(m_rigidbody.transform.position.x))
                {
                    Flip(false);
                }

                itemTag = hit2d.collider.gameObject.tag;

                foreach (string tag in TileTags)
                {
                    if (tag == itemTag)
                    {
                        if (tag == "TileDiamond")
                        {
                            itemTag = tag;
                            diamondsMap.Remove(hit2d.collider.gameObject.transform.GetInstanceID());
                        }

                        Destroy(hit2d.collider.gameObject, 0.5f);
                        return itemTag;
                    }
                }
            }
        }
        return "";
    }

    protected override void LandingEvent()
    {
        if (!m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Run") && !m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
            m_Anim.Play("Idle");
    }
}
