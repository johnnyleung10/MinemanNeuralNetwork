using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NNManager : MonoBehaviour
{
    [Header("References")]
    public GameObject map;
    public GameObject background;
    public GameObject AIPlayer;
    public GameObject border;

    public int GenerationNum = 0;
    public float timeSinceStart = 0;
    public int populationSize; //creates population size
    public int Deaths = 0;

    public int[] layers = new int[3] { 3, 4, 3 }; //initializing network to the right size
    private bool first;

    [Range(0.0001f, 1f)] public float MutationChance = 0.01f;

    [Range(0f, 1f)] public float MutationStrength = 0.5f;

    [Range(0.1f, 10f)] public float Gamespeed = 1f;

    public NeuralNetwork[] networks;
    public List<BotController> bots = new List<BotController>();

    void Start() // Start is called before the first frame update
    {
        if (populationSize % 2 != 0)
            populationSize = 50; //if population size is not even, sets it to fifty

        InitNetworks();
        MapGen();

        foreach (BotController bot in GameObject.FindObjectsOfType<BotController>()) { bots.Add(bot); }
        first = true;
        CreateBots();
    }

    private void Update()
    {
        timeSinceStart += Time.deltaTime;
        if (Deaths == populationSize)
        {
            CreateBots();
        }
    }

    public void InitNetworks()
    {
        networks = new NeuralNetwork[populationSize];
        for (int i = 0; i < populationSize; i++)
        {
            NeuralNetwork net = new NeuralNetwork(layers);
            net.Load("Assets/Save.txt"); //on start load the network save
            networks[i] = net;
        }
    }

    private void MapGen()
    {
        for (int i = 0; i < populationSize; i++)
        {
            NewMap(125f * i, i);
        }
    }

    private void NewMap(float factor, int envNum)
    {
        GameObject enviorment = new GameObject("Enviorment " + envNum);

        Vector3 mapTranslate = new Vector3(-16.59f + factor, 40.1f, -2.082031f);
        map.transform.position = mapTranslate;
        Vector3 backgroundTranslate = new Vector3(11.4f + factor, 41.9f, 10);
        background.transform.position = backgroundTranslate;
        Vector3 AIPlayerTranslate = new Vector3(-0.5f + factor, 30.5f, 0);
        AIPlayer.transform.position = AIPlayerTranslate;
        Vector3 borderTranslate = new Vector3(8.955578f + factor, 36.93333f, -2.082031f);
        border.transform.position = borderTranslate;

        GameObject newMap = Instantiate(map);
        GameObject newBackground = Instantiate(background);
        GameObject newAIPlayer = Instantiate(AIPlayer);
        GameObject newBorder = Instantiate(border);

        newMap.transform.parent = enviorment.transform;
        newBackground.transform.parent = enviorment.transform;
        newAIPlayer.transform.parent = enviorment.transform;
        newBorder.transform.parent = enviorment.transform;
    }

    public void CreateBots()
    {
        Time.timeScale = Gamespeed; //sets gamespeed, which will increase to speed up training
        if (!first)
        {
            SortNetworks(); //this sorts networks and mutates them
        }

        for (int i = 0; i < populationSize; i++)
        {
            bots[i].ResetWithNetwork();
            bots[i].network = networks[i];
            bots[i].genomeNum = i;
        }
        first = false;
        GenerationNum++;
        Deaths = 0;
        timeSinceStart = 0;
    }

    public void SortNetworks()
    {
        for (int i = 0; i < populationSize; i++)
        {
            networks[i].fitness = bots[i].overallFitness; //gets bots to set their corrosponding networks fitness
        }
        SortPopulation();

        networks[populationSize - 1].Save("Assets/Save.txt");//saves networks weights and biases to file, to preserve network performance
        for (int i = 0; i < populationSize / 2; i++)
        {
            networks[i] = networks[i + populationSize / 2].copy(new NeuralNetwork(layers));
            networks[i].Mutate((int)(1 / MutationChance), MutationStrength);
        }
    }

    private void SortPopulation()
    {
        for (int i = 0; i < populationSize; i++)
        {
            for (int j = i; j < populationSize; j++)
            {
                if (networks[i].fitness > networks[j].fitness)
                {
                    NeuralNetwork temp = networks[i];
                    networks[i] = networks[j];
                    networks[j] = temp;
                }
            }
        }
    }
}
