using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class BotInfo : MonoBehaviour
{
    public TMP_Text geneticInfoText;
    public TMP_Text genomeInfoText;

    private NNManager manager;
    private int currGen;
    Dictionary<int, float> populationFitness = new Dictionary<int, float>();

    private void Start()
    {
        manager = GameObject.FindObjectOfType<NNManager>();
    }


    private void Update()
    {
        currGen = manager.GenerationNum;
        genomeInfoText.text = "";
        geneticInfoText.text = "Current Generation: " + currGen;

        for (int i = 0; i < manager.populationSize; i++)
        {
            populationFitness.Add(i, manager.bots[i].overallFitness);
        }

        foreach (KeyValuePair<int, float> cntrl in populationFitness.OrderByDescending(key => key.Value))
        {
            if (genomeInfoText.text.Equals(""))
            {
                genomeInfoText.text = genomeInfoText.text + "ID " + cntrl.Key + ": " + System.Math.Round(cntrl.Value, 2) +" D: " +manager.bots[cntrl.Key].totalDiamondsMined;
            }
            else
            {
                genomeInfoText.text = genomeInfoText.text + "\nID " + cntrl.Key + ": " + System.Math.Round(cntrl.Value, 2) + " D: " + manager.bots[cntrl.Key].totalDiamondsMined;
            }
        }

        populationFitness.Clear();
    }
}
