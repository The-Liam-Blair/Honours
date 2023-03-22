using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Windows;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector = System.Numerics.Vector;

public class VoronoiBiomes : MonoBehaviour
{
    [SerializeField] public Vector2 Size;

    [SerializeField] public int Regions;

    [SerializeField] public GameObject tile;

    public Color[] RegionColours;

    public Color[] availableColours;
    public double[] RegionWeight;

    private GameObject[] BiomeMap;

    public Vector2[] centrePoints;

    private float timer = 0f;

    private GameObject _parent;

    List<Tuple<Color,Color>> AdjacencyRules;

    private int iter = 0;

    private System.Random random;


    public void Start()
    {
        random = new System.Random();
        
        iter = 0;
        
        AdjacencyRules = new List<Tuple<Color, Color>>();
        _parent = GameObject.Find("Output");

        foreach (Transform child in _parent.transform)
        {
            Destroy(child.gameObject);
        }

        RegionColours = new Color[Regions];

        for (int i = 0; i < RegionColours.Length; i++)
        {
            int colour = RegionWeight.Random(random.NextDouble());
            RegionColours[i] = availableColours[colour];
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
        if (Input.GetKeyDown(KeyCode.Z) && timer <= 0f)
        {
            Start();
            timer = 0.5f;
        }

        if (Input.GetKeyDown(KeyCode.X) && timer <= 0f)
        {
            CheckandCorrectBiomeColourAdjacency();
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
                BiomeMap[i].name = i.ToString();
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

    void CheckandCorrectBiomeColourAdjacency()
    {
        bool modified = true;
        // For each tile in the biome set, check if the tile's have their pre-adjacency rules broken or not in this generation.
        // If unbroken, move to next tile.
        // If broken, get the centroid controlling the other tile's colour -> set it to a (weighted) colour that passes adjacency rule.

        // Continue looping through the biome (incase fixing one adjacency broke another) until a loop is passed without any modification to the biome.

        Debug.Log("iter:" + iter);
        modified = false;
        iter++;
        
        AdjacencyRules.Clear();

        AdjacencyRules.Add(new Tuple<Color, Color>(Color.green, Color.yellow)); // Grass can be next to sand...
        AdjacencyRules.Add(new Tuple<Color, Color>(Color.yellow, Color.green));

        AdjacencyRules.Add(new Tuple<Color, Color>(Color.yellow, Color.blue)); // Sand can be next to the ocean...
        AdjacencyRules.Add(new Tuple<Color, Color>(Color.blue, Color.yellow));

        AdjacencyRules.Add(new Tuple<Color, Color>(Color.green, Color.white)); // Grass can be next to snow...
        AdjacencyRules.Add(new Tuple<Color, Color>(Color.white, Color.green));

        AdjacencyRules.Add(new Tuple<Color, Color>(Color.yellow, Color.yellow)); // Same tiles can be next to each other...
        AdjacencyRules.Add(new Tuple<Color, Color>(Color.green, Color.green));
        AdjacencyRules.Add(new Tuple<Color, Color>(Color.blue, Color.blue));
        AdjacencyRules.Add(new Tuple<Color, Color>(Color.white, Color.white));



        for (int x = 0; x < Size.x; x++)
        {
            for (int y = 0; y < Size.y; y++)
            {
                GameObject tile = BiomeMap[x + y * (int) Size.x];

                // get adjacent tiles.
                GameObject[] adjTiles = new GameObject[4];

                // Left
                if (x - 1 >= 0)
                {
                    adjTiles[0] = BiomeMap[x - 1 + y * (int) Size.x];
                }

                // Right
                if (x + 1 < Size.x)
                {
                    adjTiles[1] = BiomeMap[x + 1 + y * (int) Size.x];
                }

                // Up
                if (y + 1 < Size.y)
                {
                    adjTiles[2] = BiomeMap[x + (y + 1) * (int) Size.x];
                }

                // Down
                if (y - 1 >= 0)
                {
                    adjTiles[3] = BiomeMap[x + (y - 1) * (int) Size.x];
                }

                for (int i = 0; i < 4; i++)
                {
                    if (adjTiles[i] != null)
                    {
                        bool hasNotBrokenRule = AdjacencyRules.Contains(new Tuple<Color, Color>(
                            tile.GetComponent<Renderer>().material.color,
                            adjTiles[i].GetComponent<Renderer>().material.color));


                        if (!hasNotBrokenRule)
                        {
                            modified = true;

                            // Find the centroid controlling the other tile's colour.
                            int centroid = FindClosestCentroid(centrePoints,
                                new Vector2(adjTiles[i].transform.position.x, adjTiles[i].transform.position.y));

                            // Get all tiles associated with that centroid and change all those tile's colours to a legal colour.
                            UpdateRegionColour(centroid, tile.GetComponent<Renderer>().material.color);

                            Debug.DrawLine(tile.transform.position + Vector3.back,
                                adjTiles[i].transform.position + Vector3.back, Color.black, 5f);
                            
                            break;
                        }

                    }
                }
            }
        }
    }

    void UpdateRegionColour(int centroid, Color colour)
    {
        // get a random colour from the adjacency rules list where colour 1 is the colour passed in and colour 2 is any matching colour.
        // set the colour of all tiles associated with the centroid to the new colour.

        Color newCol = Color.black;

        List<Tuple<Color,Color>> validRules = new List<Tuple<Color, Color>>();

        for (int i = 0; i < AdjacencyRules.Count; i++)
        {
            if (Mathf.Approximately(AdjacencyRules[i].Item1.r, colour.r) &&
                Mathf.Approximately(AdjacencyRules[i].Item1.g, colour.g) &&
                Mathf.Approximately(AdjacencyRules[i].Item1.b, colour.b))
            {
                validRules.Add(AdjacencyRules[i]);
            }
        }

        if (validRules.Count == 0)
        {
            Debug.Log("Error: No valid rules found for colour: " + colour);
            return;
        }

        newCol = validRules[Random.Range(0, validRules.Count - 1)].Item2;

        foreach (var t in BiomeMap) 
        {
            if (FindClosestCentroid(centrePoints, new Vector2(t.transform.position.x, t.transform.position.y)) == centroid)
            {
                t.GetComponent<Renderer>().material.color = newCol;
            }
        } 
        RegionColours[centroid] = newCol;
    }

    // Handles the generation of UI buttons in the Unity Editor and the functions each button calls.
#if UNITY_EDITOR
    [CustomEditor(typeof(VoronoiBiomes))]
    public class VoronoiBiomeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            VoronoiBiomes _THIS = (VoronoiBiomes)target;
            if (GUILayout.Button("Spawn Voronoi"))
            {
                _THIS.Start();
            }

            if (GUILayout.Button("Iterate Voronoi Biome Correction"))
            {
                _THIS.CheckandCorrectBiomeColourAdjacency();
            }
            DrawDefaultInspector();
        }
    }
#endif
}