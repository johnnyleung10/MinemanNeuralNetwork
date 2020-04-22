using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GeneticInfo : MonoBehaviour
{
    public TMP_Text geneticInfoText;
    public TMP_Text genomeInfoText;

    private GeneticManager manager;
    private 

    void Update()
    {
        manager = GameObject.FindObjectOfType<GeneticManager>();
        int currGen = manager.currentGeneration;
        //int currGenome = manager.currentGenome;

        geneticInfoText.text = "Current Generation: " + (currGen + 1);
        //genomeInfoText.text = "Fitness: " + System.Math.Round(manager.controller.overallFitness, 2) + "\nTotal Diamonds Mined: " +manager.controller.totalDiamondsMined;
    }
}
