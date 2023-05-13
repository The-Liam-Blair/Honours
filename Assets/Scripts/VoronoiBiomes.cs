using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class VoronoiBiomes : MonoBehaviour
{
    // NOTE
    // If colours are moved around in the available colours list or otherwise, the exact values for Color.yellow are (1f, 0.9215686f, 0.01568628f, 1f).
    // Using the values suggested by Unity (1f, 0.92f, 0.016f, 1f) will create a big enough gap between the two colour values that the Math.Approximately function will fail
    //  (In the UpdateRegionColour() function) resulting in the tags of formerly yellow tiles to remain yellow and will not change, and WILL cause the WFC algorithm to fail!
    

    // Dimensions of the region map as x and y size values.
    [SerializeField] public Vector2 Size;

    // Number of Vornoi regions.
    [SerializeField] public int Regions;

    // Reference to the tile prefab.
    [SerializeField] public GameObject tile;

    // Current number of Lloyd's Relaxation iterations to be applied to the algorithm.
    [SerializeField] public int LloydIterations;

    // Colours that each tile per region.
    public Color[] RegionColours;

    // The available colours that each region can be set to.
    public Color[] availableColours;

    // The array of tiles that form the Voronoi region output.
    public GameObject[] BiomeMap;

    // The centroid each tile belongs to, aligned with BiomeMap.
    public int[] BiomeMapRegions;

    // Positions of centroids.
    public Vector2[] centrePoints;

    // Parent object reference for cleaning up the Unity editor object hierarchy.
    private GameObject _parent;

    // List of adjacency rules, mimicking WFC's rules, to be used to solve the adjacency problem.
    List<Tuple<Color,Color>> AdjacencyRules;

    // The desired distribution of each colour.
    public double[] RegionWeight;

    // The current actual distribution of each colour.
    public double[] CurrentRegionWeight;

    private byte iter = 0;

    private System.Random random;

    private void Start()
    {
        _parent = GameObject.Find("Voronoi");
    }
    
    public void GenerateNewVoronoiDiagram()
    {
        var time = Time.realtimeSinceStartup;

        // Same as WFC: Copy the seed from it if it exists, or (if seed = 0) generate a new one.
        var seed = GameObject.Find("Output").GetComponent<SimpleTiledWFC>().seed;

        if (seed == 0)
        {
            var seedValue = (int)DateTime.Now.Ticks;
            random = new System.Random(seedValue);
        }
        else
        {
            random = new System.Random(seed);
        }

        GameObject.Find("CurrentSeed").GetComponent<Text>().text = "Current Seed: " + seed;


        iter = 0;


        AdjacencyRules = new List<Tuple<Color, Color>>();

        // Reset current map, if it exists.
        foreach (Transform child in _parent.transform)
        {
            Destroy(child.gameObject);
        }

        RegionColours = new Color[Regions];

        // Generate a random colour for each region.
        for (int i = 0; i < RegionColours.Length; i++)
        {
            int colour = RegionWeight.Random(random.NextDouble());
            RegionColours[i] = availableColours[colour];
        }

        BiomeMap = new GameObject[(int) (Size.x * Size.y)];
        BiomeMapRegions = new int[BiomeMap.Length];
        centrePoints = new Vector2[Regions];

        // Generate random points for each centroid.
        for (int i = 0; i < Regions; i++)
        {
            centrePoints[i] = new Vector2(random.Next(0, (int) Size.x), random.Next(0, (int) Size.y));
            Debug.DrawRay(centrePoints[i], Vector3.back * 5f, Color.red, 5f);
        }

        // Draw the Voronoi diagram.
        GenerateVoronoiBiomeMap(false, time);
    }

    private void GenerateVoronoiBiomeMap(bool regen = false, float time = 0f)
    {
        int x = 0;
        int y = 0;

        for (int i = 0; i < Size.x * Size.y; i++)
        {
            // If regen is true, only update positions of existing tiles. (For Lloyd relaxation)
            // Otherwise, instantiate new tiles.
            if (regen) { BiomeMap[i].transform.position = new Vector3(x, y, 5); }
            else
            {
                BiomeMap[i] = Instantiate(tile, new Vector3(x, y, 5), Quaternion.identity);
                BiomeMap[i].name = i.ToString();
                BiomeMap[i].transform.parent = _parent.transform;
            }

            // Find the closest centroid to the current tile, and set its colour to the colour of that centroid.
            BiomeMap[i].GetComponent<Renderer>().material.color = RegionColours[FindClosestCentroid(centrePoints, BiomeMap[i].transform.position)];

            var col = BiomeMap[i].GetComponent<Renderer>().material.color;


            // Set the tag of the tile based on its colour, and initialise the dynamic biome weights array.
            if (col == availableColours[0])
            {
                BiomeMap[i].tag = "snow";
            }
            else if (col == availableColours[1])
            {
                BiomeMap[i].tag = "sand";
            }
            else if (col == availableColours[2])
            {
                BiomeMap[i].tag = "grass";
            }
            else if (col == availableColours[3])
            {
                BiomeMap[i].tag = "water";
            }
            else if (col == availableColours[4])
            {
                BiomeMap[i].tag = "forest";
            }
            else if (col == availableColours[5])
            {
                BiomeMap[i].tag = "shallowWater";
            }

            // Increment x and increment y every time the width extreme has been reached.
            x++;
            if (x % Size.x == 0)
            {
                y++;
                x = 0;
            }
        }

        for (int i = 0; i < BiomeMap.Length; i++)
        {
            BiomeMapRegions[i] = FindClosestCentroid(centrePoints, BiomeMap[i].transform.position);
        }

        if (time != 0)
        {
            Debug.Log("TIME (ms) OF BIOME CREATION" + (Time.realtimeSinceStartup - time) * 1000f);
        }

        // Initialize the biome distribution weights to prepare for biome correction.
        CalculateBiomeDistribution();
    }

    
    // Returns the closest centroid.
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

    public void CheckandCorrectBiomeColourAdjacency()
    {
        bool notModified = true;
        iter = 0;

        var time = Time.realtimeSinceStartup;

        AdjacencyRules.Clear();

        // Duplicate rules are used here to simplify the rule check sub-algorithm later on (Set item1 to the colour in consideration and
        // check all rules where item1 == colour being checked).

        AdjacencyRules.Add(new Tuple<Color, Color>(Color.green, Color.yellow)); // Grass can be next to sand...
        AdjacencyRules.Add(new Tuple<Color, Color>(Color.yellow, Color.green));

        AdjacencyRules.Add(new Tuple<Color, Color>(Color.yellow, Color.cyan)); // Sand can be next to the shallow waters...
        AdjacencyRules.Add(new Tuple<Color, Color>(Color.cyan, Color.yellow));

        AdjacencyRules.Add(new Tuple<Color, Color>(Color.cyan, Color.blue)); // Shallow waters can be next to the ocean...
        AdjacencyRules.Add(new Tuple<Color, Color>(Color.blue, Color.cyan));

        AdjacencyRules.Add(new Tuple<Color, Color>(Color.green, new Color(0, 0.5f, 0, 1))); // Grass can be next to forests...
        AdjacencyRules.Add(new Tuple<Color, Color>(new Color(0, 0.5f, 0, 1), Color.green));

        AdjacencyRules.Add(new Tuple<Color, Color>(Color.green, Color.white)); // Grass can be next to snow...
        AdjacencyRules.Add(new Tuple<Color, Color>(Color.white, Color.green));

        AdjacencyRules.Add(new Tuple<Color, Color>(Color.yellow, Color.yellow)); // Same tiles can be next to each other...
        AdjacencyRules.Add(new Tuple<Color, Color>(Color.green, Color.green));
        AdjacencyRules.Add(new Tuple<Color, Color>(Color.blue, Color.blue));
        AdjacencyRules.Add(new Tuple<Color, Color>(Color.white, Color.white));
        AdjacencyRules.Add(new Tuple<Color, Color>(Color.cyan, Color.cyan));
        AdjacencyRules.Add(new Tuple<Color, Color>(new Color(0, 0.5f, 0, 1), new Color(0, 0.5f, 0, 1)));

        // Loop until  max iteration count is reached (fail state) or no tiles have been modified in a single iteration (success state).
        while (notModified && iter < 100)
        {
            //Debug.Log("iter:" + iter);
            notModified = false;
            iter++;

            // Loop through enitre tile set.
            for (int x = 0; x < Size.x; x++)
            {
                for (int y = 0; y < Size.y; y++)
                {
                    GameObject tile = BiomeMap[x + y * (int) Size.x];

                    GameObject[] adjTiles = new GameObject[4]; // Adjacent tiles (left, right, up, down).

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

                    // Loop through this tile's neighbours.
                    for (int i = 0; i < 4; i++)
                    {
                        // Null tile = beyond edge of map, so no neighbour tile exists.
                        if (adjTiles[i] != null)
                        {

                            // Scan the rules list and see if a rule exists where the current tile (item 1) can be next to the adjacent tile (item 2).
                            bool hasNotBrokenRule = AdjacencyRules.Contains(new Tuple<Color, Color>(
                                tile.GetComponent<Renderer>().material.color,
                                adjTiles[i].GetComponent<Renderer>().material.color));

                            // If the rule is broken, then correct the other tile's region.
                            if (!hasNotBrokenRule)
                            { 
                                notModified = true;

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

        Debug.Log("TIME (ms) OF BIOME CORRECTION: " + (Time.realtimeSinceStartup - time) * 1000f);
        if (iter == 100)
            Debug.Log("BIOME CORRECTION FAILED TO CONVERGE");
    }

    private void UpdateRegionColour(int centroid, Color colour)
    {
        Color newCol = Color.black;

        List<Tuple<Color,Color>> validRules = new List<Tuple<Color, Color>>();

        // Find all the rules which are valid for this centroid's colour.
        // Approximation is needed as colour is stored as floats, so a delta value is needed to account for rounding errors.
        for (int i = 0; i < AdjacencyRules.Count; i++)
        {
            if (Mathf.Approximately(AdjacencyRules[i].Item1.r, colour.r) &&
                Mathf.Approximately(AdjacencyRules[i].Item1.g, colour.g) &&
                Mathf.Approximately(AdjacencyRules[i].Item1.b, colour.b))
            {
                validRules.Add(AdjacencyRules[i]);
            }
        }

        // Error catch in case no valid rule are found. If triggered, the next iteration of checking for correct adjacency will catch the error.
        if (validRules.Count == 0)
        {
            Debug.Log("Error: No valid rules found for colour: " + colour);
            return;
        }

        double[] weights = new double[validRules.Count];


        
        // Get the colour of each valid rule and assign a weight to it.
        for (int i = 0; i < validRules.Count; i++)
        {
            if (validRules[i].Item2 == Color.white) // Snow
            {
                weights[i] = CurrentRegionWeight[0];
            }
            else if (validRules[i].Item2 == Color.yellow) // Desert
            {
                weights[i] = CurrentRegionWeight[1];
            }
            else if (validRules[i].Item2 == Color.green) // Grass
            {
                weights[i] = CurrentRegionWeight[2];
            }
            else if (validRules[i].Item2 == Color.blue) // Ocean
            {
                weights[i] = CurrentRegionWeight[3];
            }
            else if (validRules[i].Item2 == new Color(0, 0.5f, 0, 1)) // Dark green - Forests
            {
                weights[i] = CurrentRegionWeight[4];
            }
            else if (validRules[i].Item2 == Color.cyan) // Shallow Waters
            {
                weights[i] = CurrentRegionWeight[5];
            }
            
        }
        

        // Pick a weight by random.
        int weight = weights.Random(random.NextDouble());
        newCol = validRules[weight].Item2;

        // Update all tiles associated with the centroid to the new colour.
        for(int i = 0; i < BiomeMap.Length; i++)
        {
            if (BiomeMapRegions[i] == centroid)
            {
                BiomeMap[i].GetComponent<Renderer>().material.color = newCol;

                // Update tag of the tile as well, as that is read by the WFC algorithm.

                if (newCol == availableColours[0]) 
                {
                    BiomeMap[i].tag = "snow";
                }
                else if (newCol == availableColours[1])
                {
                    BiomeMap[i].tag = "sand";
                }
                else if (newCol == availableColours[2])
                {
                    BiomeMap[i].tag = "grass";
                }
                else if (newCol == availableColours[3])
                {
                    BiomeMap[i].tag = "water";
                }
                else if (newCol == availableColours[4])
                {
                    BiomeMap[i].tag = "forest";
                }
                else if (newCol == availableColours[5])
                {
                    BiomeMap[i].tag = "shallowWater";
                }
            }
        } 
        // Update the centroid's assigned colour as well.
        RegionColours[centroid] = newCol;

        // Once a colour has been assigned, recalculate the weights for the biomes to attempt to skew the weights to match the user's desired distributions.
        CalculateBiomeDistribution();
    }

    public void LloydRelaxation()
    {
        iter = 0;

        var time = Time.realtimeSinceStartup;
        
        while (iter < LloydIterations)
        {
            iter++;
            for (int i = 0; i < centrePoints.Count(); i++)
            {
                Vector2 averagePos = Vector2.zero;
                int numTiles = 0;

                for (int j = 0; j < BiomeMap.Length; j++)
                {
                    if (BiomeMapRegions[j] == i)
                    {
                        averagePos += new Vector2(BiomeMap[j].transform.position.x, BiomeMap[j].transform.position.y);
                        numTiles++;
                    }
                }

                averagePos /= numTiles;

                centrePoints[i] = averagePos;
            }
        }
        GenerateVoronoiBiomeMap(true);

        Debug.Log("TIME (ms) OF " + LloydIterations + " LLOYD RELAXATIONS ITERATIONS: " + (Time.realtimeSinceStartup - time) * 1000f);

    }

    public void CalculateBiomeDistribution()
    {
        // Stores the true distribution of biomes in the map in a given moment.
        CurrentRegionWeight = new double[availableColours.Count()];

        // Finds the current colours of the regions and stores the count in the region weights array.
        for (int i = 0; i < CurrentRegionWeight.Length; i++)
        {
            CurrentRegionWeight[i] = RegionColours.Count(x => x == availableColours[i]);
        }

        // Divide by total region count to normalize the results, such that the weights when added equal 1.
        // Then apply a scaling formula to the weights against the desired user input weight to skew the weights towards the generation the user wants.
        for (int i = 0; i < CurrentRegionWeight.Length; i++)
        {
            CurrentRegionWeight[i] /= Regions;

            // Add an epsilon value to avoid division by zero, in case a given biome type does not exist yet in the current generation.
            CurrentRegionWeight[i] = (RegionWeight[i] / (CurrentRegionWeight[i] + 0.001));
        }

    }

    private void Clear()
    {
        foreach (var t in BiomeMap)
        {
            Destroy(t);
        }
    }

    #if UNITY_EDITOR
    [CustomEditor(typeof(VoronoiBiomes))]
    public class VoronoiBiomeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            VoronoiBiomes _THIS = (VoronoiBiomes)target;
            if (GUILayout.Button("Generate Voronoi"))
            {
                Debug.Log("---------------------------");
                var time = Time.realtimeSinceStartup;
                _THIS.GenerateNewVoronoiDiagram();
                //_THIS.LloydRelaxation();
                //_THIS.CheckandCorrectBiomeColourAdjacency();
                Debug.Log("TOTAL GENERATION TIME (ms) OF ITERATION: " + (Time.realtimeSinceStartup - time) * 1000f);

            }

            if (GUILayout.Button("Correct biome colour adjacency 25 iterations"))
            {
                _THIS.CheckandCorrectBiomeColourAdjacency();
            }

            if (GUILayout.Button("Perform Lloyd Relaxation variable iterations"))
            {
                _THIS.LloydRelaxation();
            }

            if (GUILayout.Button("Clear Voronoi Regions"))
            {
                _THIS.Clear();
            }
            DrawDefaultInspector();
        }
    }
    #endif
}