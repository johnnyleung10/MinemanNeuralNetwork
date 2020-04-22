using System.Collections.Generic;
using UnityEngine;

using MathNet.Numerics.LinearAlgebra;
using TMPro.EditorUtilities;
using UnityEditor;

public class GeneticManager : MonoBehaviour
{
    [Header("References")]
    public GameObject map;
    public GameObject background;
    public GameObject AIPlayer;
    public GameObject border;

    public AIController[] controllers;

    [Header("Controls")]
    public int initialPopulation = 4;
    [Range(0.0f, 1.0f)]
    public float mutationRate = 0.055f;

    [Header("Crossover Controls")]
    public int bestAgentSelection = 8;
    public int worstAgentSelection = 3;
    public int numberToCrossover;

    private List<int> genePool = new List<int>();

    private int naturallySelected;

    [HideInInspector]
    public NNet[] population;

    [Header("Public View")]
    public int currentGeneration;
    public int currentFinished = 0;

    private void Start()
    {
        MapGen();
        controllers = GameObject.FindObjectsOfType<AIController>();
        //CreatePopulation();
    }

    private void MapGen()
    {
        for (int i = 0; i < initialPopulation; i++)
        {
            NewMap(125f * (i + 1));
        }
    }
    private void NewMap(float factor)
    {
        Vector3 mapTranslate = new Vector3(-16.59f + factor, 40.1f, -2.082031f);
        map.transform.position = mapTranslate;
        Vector3 backgroundTranslate = new Vector3(11.4f + factor, 41.9f, 10);
        background.transform.position = backgroundTranslate;
        Vector3 AIPlayerTranslate = new Vector3(0f + factor, 32f, 0);
        AIPlayer.transform.position = AIPlayerTranslate;
        Vector3 borderTranslate = new Vector3(8.955578f + factor, 36.93333f, -2.082031f);
        border.transform.position = borderTranslate;

        GameObject newMap = Instantiate(map);
        Instantiate(background);
        GameObject newAIPlayer = Instantiate(AIPlayer);
        Instantiate(border);

        newMap.transform.parent = newAIPlayer.transform;
    }
    private void CreatePopulation()
    {
        population = new NNet[initialPopulation];
        FillPopulationWithRandomValues(population, 0);
        //ResetToCurrentGenome();
        SetGenomes();
    }

    private void SetGenomes()
    {
        for (int i = 0; i < initialPopulation; i++)
        {
            controllers[i].ResetWithNetwork(population[i]);
            controllers[i].genomeNum = i;
        }
    }

    private void ResetToCurrentGenome()
    {
        //controller.ResetWithNetwork(population[currentGenome]);
    }

    private void FillPopulationWithRandomValues(NNet[] newPopulation, int startingIndex)
    {
        while(startingIndex < initialPopulation)
        {
            newPopulation[startingIndex] = new NNet();
            newPopulation[startingIndex].Initialize(controllers[0].LAYERS, controllers[0].NEURONS, controllers[0].NUMINPUT);
            startingIndex++;
        }
    }

    public void Death(float fitness, int genomeNum) // <------- WHEN CALLED
    {
        /*
        if (currentGenome < population.Length - 1)
        {
            Debug.Log(fitness);
            population[genomeNum].fitness = fitness;
            currentGenome++;
            ResetToCurrentGenome();
        }
        else
        {
            RePopulate();
        }
        */
        Debug.Log("Genome " +genomeNum + " Fitness: " +System.Math.Round(fitness, 3));
        population[genomeNum].fitness = fitness;

        if (currentFinished == initialPopulation - 1)
        {
            RePopulate();
        }
    }

    private void RePopulate()
    {
        genePool.Clear();
        currentGeneration++;
        naturallySelected = 0;
        SortPopulation();

        NNet[] newPopulation = PickBestPopulation();
        Crossover(newPopulation);
        Mutate(newPopulation);

        FillPopulationWithRandomValues(newPopulation, naturallySelected);

        population = newPopulation;

        SetGenomes();
        //ResetToCurrentGenome();
    }

    private void Crossover(NNet[] newPopulation)
    {
        for (int i = 0; i < numberToCrossover; i+=2)
        {
            int AIndex = i;
            int BIndex = i + 1;

            if (genePool.Count >= 1)
            {
                for (int j = 0; j < 100; j++)
                {
                    AIndex = genePool[Random.Range(0, genePool.Count)];
                    BIndex = genePool[Random.Range(0, genePool.Count)];

                    if (AIndex != BIndex)
                        break;
                }
            }

            NNet Child1 = new NNet();
            NNet Child2 = new NNet();

            Child1.Initialize(controllers[0].LAYERS, controllers[0].NEURONS, controllers[0].NUMINPUT);
            Child2.Initialize(controllers[0].LAYERS, controllers[0].NEURONS, controllers[0].NUMINPUT);

            Child1.fitness = 0;
            Child2.fitness = 0;

            // Crossover Weights
            for (int w = 0; w < Child1.weights.Count; w++)
            {
                if (Random.Range(0.0f, 1.0f) < 0.5f)
                {
                    Child1.weights[w] = population[AIndex].weights[w];
                    Child2.weights[w] = population[BIndex].weights[w];
                }
                else
                {
                    Child1.weights[w] = population[BIndex].weights[w];
                    Child2.weights[w] = population[AIndex].weights[w];
                }
            }

            // Crossover Biases
            for (int b = 0; b < Child1.biases.Count; b++)
            {
                if (Random.Range(0.0f, 1.0f) < 0.5f)
                {
                    Child1.biases[b] = population[AIndex].biases[b];
                    Child2.biases[b] = population[BIndex].biases[b];
                }
                else
                {
                    Child1.biases[b] = population[BIndex].biases[b];
                    Child2.biases[b] = population[AIndex].biases[b];
                }
            }

            newPopulation[naturallySelected] = Child1;
            naturallySelected++;

            newPopulation[naturallySelected] = Child2;
            naturallySelected++;
        }
    }

    private void Mutate(NNet[] newPopulation)
    {
        for (int i = 0; i < naturallySelected; i++)
        {
            for (int w = 0; w < newPopulation[i].weights.Count; w++)
            {
                if (Random.Range(0.0f, 1.0f) < mutationRate)
                {
                    newPopulation[i].weights[w] = MutateMatrix(newPopulation[i].weights[w]);
                }
            }
        }
    }

    Matrix<float> MutateMatrix(Matrix<float> A)
    {
        int randomPoints = Random.Range(1, (A.RowCount * A.ColumnCount) / 7);
        Matrix<float> c = A;

        for (int i = 0; i < randomPoints; i++)
        {
            int randomColumn = Random.Range(0, c.ColumnCount);
            int randomRow = Random.Range(0, c.RowCount);

            c[randomRow, randomColumn] = Mathf.Clamp(c[randomRow, randomColumn] + Random.Range(-1f, 1f), -1f, 1f);
        }

        return c;
    }

    private NNet[] PickBestPopulation()
    {
        NNet[] newPopulation = new NNet[initialPopulation];

        for (int i = 0; i < bestAgentSelection; i++)
        {
            newPopulation[naturallySelected] = population[i].InitializeCopy(controllers[i].LAYERS, controllers[i].NEURONS);
            Debug.Log("Best Fitness " + population[i].fitness);
            newPopulation[naturallySelected].fitness = 0;
            naturallySelected++;

            int f = Mathf.RoundToInt(population[i].fitness * 10);

            for (int c = 0; c < f; c++)
            {
                genePool.Add(i);
            }
        }

        for (int i = 0; i < worstAgentSelection; i++)
        {
            int last = population.Length - 1;
            last -= i;

            int f = Mathf.RoundToInt(population[last].fitness * 10);

            for (int c = 0; c < f; c++)
            {
                genePool.Add(last);
            }

        }

        return newPopulation;
    }

    private void SortPopulation()
    {
        Quick_Sort(0, population.Length - 1);        
    }

    private void Quick_Sort(int left, int right)
    {
        if (left < right)
        {
            int pivot = Partition(left, right);

            if (pivot > 1)
            {
                Quick_Sort(left, pivot - 1);
            }
            if (pivot + 1 < right)
            {
                Quick_Sort(pivot + 1, right);
            }
        }

    }

    private int Partition(int left, int right)
    {
        float pivot = population[left].fitness;
        while (true)
        {
            while (population[left].fitness > pivot)
            {
                left++;
            }

            while (population[right].fitness < pivot)
            {
                right--;
            }

            if (left < right)
            {
                if (population[left].fitness == population[right].fitness)
                {
                    return right;
                }

                NNet temp = population[left];
                population[left] = population[right];
                population[right] = temp;
            }
            else
            {
                return right;
            }
        }
    }

}
