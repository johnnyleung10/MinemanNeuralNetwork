using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.U2D.TriangleNet;

[RequireComponent(typeof(NNet))]
public class AIController : Swordman
{
    public GameObject map;

    public int genomeNum;

    private Vector3 startPosition;
    private NNet network;

    [Header("AI Moves")]
    [Range(-1f, 1f)]
    public float MoveDirection;
    [Range(0, 1f)]
    public float MoveSensitivity;
    public bool Jump;
    public bool Attack;
    public Vector3 dir = new Vector3(0, 0, 0);

    public bool Alive;

    public float timeSinceStart = 0f;

    [Header("Fitness")]
    public float overallFitness;
    public float dirtMultiplier = 0.2f;
    public float stoneMultiplier = 1.0f;
    public float diamondMultiplier = 20.0f;

    private Vector3 lastPosition;
    public float totalDiamondsMined;
    public float totalStoneMined;
    public float totalDirtMined;
    public int jumpCount;

    [Header("Network Options")]
    public int LAYERS = 1;
    public int NEURONS = 10;
    public int NUMINPUT = 2;


    private List<ArrayList> sensors = new List<ArrayList>();
    private const int SENSORSIZE = 11;
    private int sensorIndex = 0;

    private float SwingCoolDown = 0.5f;
    private float SwingCoolDownTimer = 0f;


    private void Awake()
    {
        startPosition = transform.position;
        Alive = true;
        network = this.GetComponent<NNet>();
    }

    public void ResetWithNetwork(NNet net)
    {
        Alive = true;
        network = net;
        Reset();
    }

    public void Reset()
    {
        timeSinceStart = 0f;
        totalDiamondsMined = 0f;
        totalStoneMined = 0f;
        totalDirtMined = 0f;
        lastPosition = startPosition;
        overallFitness = 0f;
        transform.position = startPosition;

        // Reset Moves
        dir = new Vector3(0, 0, 0);
        MoveDirection = 0;
        MoveSensitivity = 0;
        Jump = false;
        Attack = false;

        jumpCount = 0;

        ResetMap();
    }

    public void ResetMap()
    {
        Transform t = this.transform;
        GameObject currMap;
        Vector3 temp = new Vector3(0, 0, 0);
        foreach(Transform tr in t)
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
        newMap.transform.parent = transform;
    }

    private void FixedUpdate()
    {
        if (Alive)
        {
            SensorInit();
            InputSensors();
            lastPosition = transform.position;

            timeSinceStart += Time.deltaTime;

            NUMINPUT = network.getInputs(sensors, SENSORSIZE);

            if (NUMINPUT <= 1)
            {
                Death();
            }
            network.Initialize(LAYERS, NEURONS, NUMINPUT);
            (MoveDirection, MoveSensitivity, Jump, Attack, dir) = network.RunNetwork(network.sensorSortedList(sensors), NUMINPUT);

            Debug.DrawLine(transform.position, transform.position + dir, Color.cyan, 2);
            SwingCoolDownTimer = checkCoolDown(SwingCoolDownTimer);
            // Debug.Log(SwingCoolDownTimer);
            if (SwingCoolDownTimer == 0)
            {
                MineMiner(Attack, dir);
                SwingCoolDownTimer = SwingCoolDown;
            }


            JumpMiner(Jump);
            MoveMiner(MoveDirection, MoveSensitivity);


            Jump = false;
            Attack = false;


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

    private void Death()
    {
        GameObject.FindObjectOfType<GeneticManager>().Death(overallFitness, genomeNum);
        PauseMiner();
    }

    private void CalculateFitness()
    {
        overallFitness = (totalDirtMined * dirtMultiplier) + (totalDiamondsMined * diamondMultiplier) + (totalStoneMined * stoneMultiplier);

        if (timeSinceStart > 20 && overallFitness < 100)
        {
            Death();
        }
        if (jumpCount > 40)
        {
            Death();
        }
        if (overallFitness >= 400)
        {
            // Save Model?
            Death();
        }
        if (timeSinceStart > 120)
        {
            Death();
        }
    }

    private void SensorInit()
    {
        sensors.Clear();
        sensorIndex = 0;

        for (int i = 0; i < SENSORSIZE; i++)
        {
            ArrayList sensorNum = new ArrayList();
            sensors.Add(sensorNum);
        }
    }

    private void InputSensors()
    {
        // Right sensors
        float distance = 5.0f;
        Vector3 startRight = transform.position + transform.right;
        int countRight = 0;

        while (countRight <= 5)
        {
            float hyp = Mathf.Sqrt(Mathf.Pow(distance, 2) + Mathf.Pow(-1 + (startRight - transform.position).y, 2));
            RaycastHit2D[] hit2d = Physics2D.RaycastAll(transform.position, startRight - transform.position, hyp);
            foreach (RaycastHit2D hit in hit2d)
            {
                if (hit.collider != null)
                {
                    sensors[sensorIndex].Add(hit.collider.gameObject.tag);
                    Debug.DrawLine(transform.position, hit.point, Color.red);
                }
            }

            sensorIndex++;
            startRight -= new Vector3(0.20f, 0.20f, 0);
            countRight++;
        }

        // Left sensors
        Vector3 startLeft = transform.position - transform.right;
        int countLeft = 0;
        while (countLeft <= 4)
        {
            float hyp = Mathf.Sqrt(Mathf.Pow(distance, 2) + Mathf.Pow(-1 + (startLeft - transform.position).y, 2));
            RaycastHit2D[] hit2d = Physics2D.RaycastAll(transform.position, startLeft - transform.position, hyp);
            foreach (RaycastHit2D hit in hit2d)
            {
                if (hit.collider != null)
                {
                    sensors[sensorIndex].Add(hit.collider.gameObject.tag);
                    Debug.DrawLine(transform.position, hit.point, Color.red);
                }
            }

            sensorIndex++;
            startLeft -= new Vector3(-0.20f, 0.20f, 0);
            countLeft++;
        }
    }

    private void MoveMiner(float moveDirection, float sensitivity)
    {
        GroundCheckUpdate();
        if (moveDirection > 0)
            Move_Right(sensitivity);
        else if (moveDirection < 0)
            Move_Left(-sensitivity);
    }

    private void MineMiner(bool mine, Vector3 directionSwing)
    {
        if (mine && directionSwing == new Vector3(0, 0, 0))
            return;
        else if (mine)
        {
            StartCoroutine(Move_Attack(transform.position + directionSwing, (returnValue) =>
            {
                if (returnValue.Equals("TileDirt"))
                {
                    totalDirtMined++;
                }
                if (returnValue.Equals("TileStone"))
                {
                    totalStoneMined++;
                }
                else if (returnValue.Equals("TileDiamond"))
                {
                    totalDiamondsMined++;
                }
                }));
        }
    }

    private void JumpMiner(bool jump)
    {
        // Debug.Log(jump);
        if (jump)
        {
            Move_Jump();
            //jumpCount++;
        }

    }

    private void PauseMiner()
    {
        Move_Death();
        Alive = false;
    }
}
