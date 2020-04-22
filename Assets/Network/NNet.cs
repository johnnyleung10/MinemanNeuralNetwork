using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MathNet.Numerics.LinearAlgebra;
using System;

using Random = UnityEngine.Random;

public class NNet : MonoBehaviour
{
    public Matrix<float> inputLayer;
    public List<Matrix<float>> hiddenLayers = new List<Matrix<float>>();
    public Matrix<float> outputLayer = Matrix<float>.Build.Dense(1, 5);

    public List<Matrix<float>> weights = new List<Matrix<float>>();
    public List<float> biases = new List<float>();
    public float fitness;

    public bool floatToBool(float f)
    {
        //Debug.Log("Attack float " + f);
        if (f > 0.5)
        {
            return true;
        } 
        return false;
    }

    public Vector3 floatToVector(float f)
    {
        //Debug.Log("Direction float " + f);
        Vector3 returnVector = new Vector3(0, 0, 0);
        f = f * 2 * Mathf.PI;


        returnVector.x = Mathf.Cos(f);
        returnVector.y = Mathf.Sin(f);

        return returnVector;
    }

    public int getInputs(List<ArrayList> inputSensors, int SENSORSIZE = 11)
    {
        int input = 0;
        for (int i = 0; i < SENSORSIZE; i++)
        {
            input += inputSensors[i].Count;
        }
        return input;
    }

    public List<string> sensorSortedList(List<ArrayList> inputSensors, int SENSORSIZE = 11)
    {
        List<string> returnArr = new List<string>();
        for (int i = 0; i < SENSORSIZE; i++)
        {
            foreach (string tag in inputSensors[i])
            {
                returnArr.Add(tag);
            }
        }
        return returnArr;
    }

    public void Initialize(int hiddenLayerCount, int hiddenNeuronCount, int inputNum)
    {
        hiddenLayers.Clear();
        outputLayer.Clear();
        weights.Clear();
        biases.Clear();

        inputLayer = Matrix<float>.Build.Dense(1, inputNum);
        for (int i = 0; i < hiddenLayerCount + 1; i++)
        {
            Matrix<float> f = Matrix<float>.Build.Dense(1, hiddenNeuronCount);
            hiddenLayers.Add(f);

            biases.Add(Random.Range(-1f, 1f));

            // WEIGHTS
            if (i == 0)
            {
                Matrix<float> inputToH1 = Matrix<float>.Build.Dense(inputNum, hiddenNeuronCount);
                weights.Add(inputToH1);
            }

            Matrix<float> HiddenToHidden = Matrix<float>.Build.Dense(hiddenNeuronCount, hiddenNeuronCount);
            weights.Add(HiddenToHidden);
        }

        Matrix<float> OutputWeight = Matrix<float>.Build.Dense(hiddenNeuronCount, 5); // <--------------- OUTPUT COUNT = 5
        weights.Add(OutputWeight);
        biases.Add(Random.Range(-1f, 1f));

        RandomizeWeights();
    }

    public NNet InitializeCopy(int hiddenLayerCount, int hiddenNeuronCount)
    {
        NNet n = new NNet();
        List<Matrix<float>> newWeights = new List<Matrix<float>>();

        for (int i = 0; i < this.weights.Count; i++)
        {
            Matrix<float> currentWeight = Matrix<float>.Build.Dense(weights[i].RowCount, weights[i].ColumnCount);
            
            for (int x = 0; x < currentWeight.RowCount; x++)
            {
                for (int y = 0; y < currentWeight.ColumnCount; y++)
                {
                    currentWeight[x, y] = weights[i][x, y];
                }
            }
            newWeights.Add(currentWeight);
        }

        List<float> newBiases = new List<float>();
        newBiases.AddRange(biases);

        n.weights = newWeights;
        n.biases = newBiases;

        n.InitializeHidden(hiddenLayerCount, hiddenNeuronCount);

        return n;
    }

    public void InitializeHidden(int hiddenLayerCount, int hiddenNeuronCount)
    {

        //inputLayer.Clear();
        hiddenLayers.Clear();
        outputLayer.Clear();

        for (int i = 0; i < hiddenLayerCount + 1; i++)
        {
            Matrix<float> newHiddenLayer = Matrix<float>.Build.Dense(1, hiddenNeuronCount);
            hiddenLayers.Add(newHiddenLayer);
        }

    }

    public void RandomizeWeights()
    {
        for (int i = 0; i < weights.Count; i++)
        {
            for (int x = 0; x < weights[i].RowCount; x++)
            {
                for (int y = 0; y < weights[i].ColumnCount; y++)
                {
                    weights[i][x, y] = Random.Range(-1f, 1f);
                }
            }
        }
    }

    public (float, float, bool, bool, Vector3) RunNetwork(List<string> sensorList, int numInputs) // <======== Num of inputs and num of return outputs!!!!!!!!!!!!!!
    {
        for (int i = 0; i < numInputs; i++)
        {
            inputLayer[0, i] = sensorList[i].GetHashCode();
        }

        inputLayer = inputLayer.PointwiseTanh();

        hiddenLayers[0] = ((inputLayer * weights[0] + biases[0]).PointwiseTanh());

        for (int i = 1; i < hiddenLayers.Count; i++)
        {
            hiddenLayers[i] = ((hiddenLayers[i - 1] * weights[i]) + biases[i]).PointwiseTanh();
        }

        outputLayer = ((hiddenLayers[hiddenLayers.Count - 1] * weights[weights.Count - 1]) + biases[biases.Count - 1]).PointwiseTanh();

        // Move, Sensitivity of Movement, Jump?, Mine?, Direction
        return (outputLayer[0, 0], Mathf.Abs(outputLayer[0, 1]), floatToBool(Mathf.Abs(outputLayer[0, 2])), floatToBool(Mathf.Abs(outputLayer[0, 3])), floatToVector(Mathf.Abs(outputLayer[0, 4])));
    }

}
