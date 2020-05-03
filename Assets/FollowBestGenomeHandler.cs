using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class FollowBestGenomeHandler : MonoBehaviour
{
    private CinemachineVirtualCamera vcam;
    private NNManager nnManager;

    public void FollowBest2()
    {
        nnManager = GameObject.FindObjectOfType<NNManager>();
        vcam = GameObject.FindObjectOfType<CinemachineVirtualCamera>();

        int bestGenomeNum = 0;
        float bestFit = 0;
        for (int i = 0; i < nnManager.populationSize; i++)
        {
            if (nnManager.bots[i].overallFitness > bestFit)
            {
                bestFit = nnManager.bots[i].overallFitness;
                bestGenomeNum = i;
            }
        }

        BotController[] BotControllers = GameObject.FindObjectsOfType<BotController>();
        foreach (BotController bc in BotControllers)
        {
            if (bc.genomeNum == bestGenomeNum)
            {
                Debug.Log("NOW FOLLOWING " + bestGenomeNum + " BESTFIT " + bestFit);
                vcam.m_Follow = bc.transform;
                break;
            }
        }
    }
}
