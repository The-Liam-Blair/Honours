using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using Random = UnityEngine.Random;

public class VoronoiBiomes : MonoBehaviour
{
    [SerializeField] public Vector2 Size;

    [SerializeField] public int Regions;

    [SerializeField] public GameObject tile;

    public Color[] RegionColours;

    private GameObject[] BiomeMap;

    public Vector2[] centrePoints;

    private float timer = 0f;

    private GameObject _parent;
    
    public void Start()
    {
        _parent = GameObject.Find("Output");

        foreach (Transform child in _parent.transform)
        {
            Destroy(child.gameObject);
        }

        RegionColours = new Color[Regions];

        for (int i = 0; i < RegionColours.Length; i++)
        {
            int col = Random.Range(0, 4);

            switch (col)
            {
                case 0:
                    RegionColours[i] = Color.white;
                    break;

                case 1:
                    RegionColours[i] = Color.green;
                    break;

                case 2:
                    RegionColours[i] = Color.yellow;
                    break;

                case 3:
                    RegionColours[i] = Color.blue;
                    break;
            }
        }

        BiomeMap = new GameObject[(int) (Size.x * Size.y)];
        centrePoints = new Vector2[Regions];

        for (int i = 0; i < Regions; i++)
        {
            centrePoints[i] = new Vector2(Random.Range(0, Size.x), Random.Range(0, Size.y));
            Debug.DrawRay(centrePoints[i], Vector3.back * 5f, Color.red, 5f);
        }

        GenerateVoronoiBiomeMap(false);
        
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && timer <= 0f)
        {
            Start();
            timer = 0.5f;
        }

        timer -= Time.deltaTime;
    }

    void GenerateVoronoiBiomeMap(bool regen = false)
    {
        int x = 0;
        int y = 0;

        for (int i = 0; i < Size.x * Size.y; i++)
        {
            if (regen) { BiomeMap[i].transform.position = new Vector3(x, y); }
            else
            {
                BiomeMap[i] = Instantiate(tile, new Vector3(x, y, 0), Quaternion.identity);
                BiomeMap[i].transform.parent = _parent.transform;
            }
            
            BiomeMap[i].GetComponent<Renderer>().material.color = RegionColours[FindClosestCentroid(centrePoints, new Vector2(x, y))];


            var col = BiomeMap[i].GetComponent<Renderer>().material.color;

            if (col == Color.blue) { BiomeMap[i].tag = "blue"; }
            else if (col == Color.green) { BiomeMap[i].tag = "green"; }
            else if (col == Color.yellow) { BiomeMap[i].tag = "yellow"; }
            else if (col == Color.white) { BiomeMap[i].tag = "white"; }


            // Increment x and increment y every time the width extreme has been reached.
            x++;
            if (x % Size.x == 0)
            {
                y++;
                x = 0;
            }
        }
    }

    
    // Slightly incorrect method of finding the closest centroid, but it produces an interesting, unstructured effect.
    int FindClosestCentroid(Vector2[] centroids, Vector2 point)
    {
        float minDistance = float.MaxValue;
        int closestCentroid = 0;

        for (int i = 0; i < centroids.Length; i++)
        {
            float distance = Vector2.Distance(centroids[i], point);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestCentroid = i;
            }
        }
        return closestCentroid;
    }

    void SetBiomeColourAdjacency()
    {
        // For each tile in the biome set, check if the tile's have their pre-adjacency rules broken or not in this generation.
        // If unbroken, move to next tile.
        // If broken, get the centroid controlling the other tile's colour -> set it to a (weighted) colour that passes adjacency rule.

        // Continue looping through the biome (incase fixing one adjacency broke another) until a loop is passed without any modification to the biome.
    }
}