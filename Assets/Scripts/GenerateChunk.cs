using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateChunk : MonoBehaviour
{
    public int width;
    public int heightMultiplier;
    public int heightAddition;

    public GameObject DirtTile;
    public GameObject GrassTile;
    public GameObject StoneTile;

    public float smoothness;

    [HideInInspector]
    public float seed;

    public GameObject tileDiamond;

    public float chanceDiamond;

    void Start()
    {
        Generate();
    }


    public void Generate()
    {
        for (int i = 0; i < width; i++)
        {
            int height = Mathf.RoundToInt(Mathf.PerlinNoise(seed, (i + transform.position.x) / smoothness) * heightMultiplier) + heightAddition;
            GameObject selectedTile;
            for (int j = 0; j < height; j++)
            {
                if (j < height - 4)
                {
                    selectedTile = StoneTile;
                }
                else if (j < height - 1)
                {
                    selectedTile = DirtTile;
                }
                else
                {
                    selectedTile = GrassTile;
                }
                GameObject newtile = Instantiate(selectedTile, Vector3.zero, Quaternion.identity) as GameObject;
                newtile.transform.parent = this.gameObject.transform;
                newtile.transform.localPosition = new Vector3(i, j);
            }
        }
        OreGeneration();
    }

    public void OreGeneration()
    {
        foreach(GameObject t in GameObject.FindGameObjectsWithTag("TileStone"))
        {
            float random = Random.Range(0f, 100f);
            GameObject selectedTile = null;
            if (random <= chanceDiamond)
            {
                selectedTile = tileDiamond;
            }

            if (selectedTile != null)
            {
                Instantiate(selectedTile, t.transform.position, Quaternion.identity);
                Destroy(t);
            }
        }
    }
}
