using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// Handles all user inputs and UI interaction.
/// </summary>
public class UIManager : MonoBehaviour
{
    private VoronoiBiomes voronoiGenerator;
    private SimpleTiledWFC WFCGenerator;

    private GameObject AlgorithmCanvas;
    private GameObject ModifyVariableCanvas;

    // INPUT - User can enter values inside this component.
    // VALUE - The value that the user has entered is shown back to the user, stores entered variables.
    
    // Probably an extremely inefficient solution, though its required due to the huge variety of variables.

    private Text VALUE_LloydRelaxation;
    private Slider INPUT_LloydRelaxation;

    private Text VALUE_CollapsedNeighbour;
    private Slider INPUT_CollapsedNeighbour;

    private Text VALUE_OutputTitle;
    private InputField INPUT_OutputX;
    private InputField INPUT_OutputY;

    private Text VALUE_Seed;
    private Text VALUE_BiomeSeed;
    private InputField INPUT_Seed;
    private InputField INPUT_BiomeSeed;

    private Text VALUE_Regions;
    private InputField INPUT_Regions;

    private Slider INPUT_Iterations;
    private Text VALUE_Iterations;

    private Slider INPUT_SandDistri;
    private Slider INPUT_GrassDistri;
    private Slider INPUT_SnowDistri;
    private Slider INPUT_WaterDistri;
    private Slider INPUT_ForestDistri;
    private Slider INPUT_ShallowWaterDistri;

    private Text VALUE_SandDistri;
    private Text VALUE_GrassDistri;
    private Text VALUE_SnowDistri;
    private Text VALUE_WaterDistri;
    private Text VALUE_ForestDistri;
    private Text VALUE_ShallowWaterDistri;

    private Slider INPUT_SandBSpread;
    private Slider INPUT_GrassBSpread;
    private Slider INPUT_SnowBSpread;
    private Slider INPUT_WaterBSpread;
    private Slider INPUT_ForestBSpread;
    private Slider INPUT_ShallowWaterBSpread;

    private Text VALUE_SandBSpread;
    private Text VALUE_GrassBSpread;
    private Text VALUE_SnowBSpread;
    private Text VALUE_WaterBSpread;
    private Text VALUE_ForestBSpread;
    private Text VALUE_ShallowWaterBSpread;

    private Text CurrentSeed;
    private Text CurrentBiomeSeed;

    void Start()
    {
        // References to sub-systems.
        voronoiGenerator = GameObject.Find("Voronoi").GetComponent<VoronoiBiomes>();
        WFCGenerator = GameObject.Find("Output").GetComponent<SimpleTiledWFC>();

        // References to UI systems.
        AlgorithmCanvas = GameObject.Find("Algorithm Interaction Canvas");
        ModifyVariableCanvas = GameObject.Find("Variable Modification Canvas");

        // UI Variable references: Input field (to get value) and title (to show value to the user):

        // Lloyd Relaxation Iterations
        INPUT_LloydRelaxation = GameObject.Find("LloydRelaxationInput").GetComponent<Slider>();
        VALUE_LloydRelaxation = GameObject.Find("LloydTitle").GetComponent<Text>();

        // Collapsed Neighbour Threshold
        INPUT_CollapsedNeighbour = GameObject.Find("CollapsedNeighbourInput").GetComponent<Slider>();
        VALUE_CollapsedNeighbour = GameObject.Find("CollapsedNeighbourTitle").GetComponent<Text>();

        // Output size
        INPUT_OutputX = GameObject.Find("OutputXInput").GetComponent<InputField>();
        INPUT_OutputY = GameObject.Find("OutputYInput").GetComponent<InputField>();
        VALUE_OutputTitle = GameObject.Find("OutputSizeTitle").GetComponent<Text>();

        // Seed and Biome spread seed
        INPUT_Seed = GameObject.Find("SeedInput").GetComponent<InputField>();
        INPUT_BiomeSeed = GameObject.Find("BiomeSeedInput").GetComponent<InputField>();
        VALUE_Seed = GameObject.Find("SeedTitle").GetComponent<Text>();
        VALUE_BiomeSeed = GameObject.Find("BiomeSeedTitle").GetComponent<Text>();

        // Voronoi Region Count
        INPUT_Regions = GameObject.Find("RegionCountInput").GetComponent<InputField>();
        VALUE_Regions = GameObject.Find("RegionCountTitle").GetComponent<Text>();

        // Number of iterations the WFC algorithm will run for per update->draw cycle (More = faster generation!)
        INPUT_Iterations = GameObject.Find("IterationsInput").GetComponent<Slider>();
        VALUE_Iterations = GameObject.Find("IterationsTitle").GetComponent<Text>();

        // Distribution of biomes, utilised by the Voronoi Diagram algorithm.
        INPUT_SandDistri = GameObject.Find("DistriSand").GetComponent<Slider>();
        INPUT_GrassDistri = GameObject.Find("DistriGrass").GetComponent<Slider>();
        INPUT_SnowDistri = GameObject.Find("DistriSnow").GetComponent<Slider>();
        INPUT_WaterDistri = GameObject.Find("DistriOcean").GetComponent<Slider>();
        INPUT_ForestDistri = GameObject.Find("DistriForest").GetComponent<Slider>();
        INPUT_ShallowWaterDistri = GameObject.Find("DistriShallowWater").GetComponent<Slider>();
        VALUE_SandDistri = GameObject.Find("OutputSandDistri").GetComponent<Text>();
        VALUE_GrassDistri = GameObject.Find("OutputGrassDistri").GetComponent<Text>();
        VALUE_SnowDistri = GameObject.Find("OutputSnowDistri").GetComponent<Text>();
        VALUE_WaterDistri = GameObject.Find("OutputOceanDistri").GetComponent<Text>();
        VALUE_ForestDistri = GameObject.Find("OutputForestDistri").GetComponent<Text>();
        VALUE_ShallowWaterDistri = GameObject.Find("OutputShallowWaterDistri").GetComponent<Text>();

        // Biome spread coefficients (How likely each tile in x biome is capable of being converted), utilized by the WFC algorithm.
        INPUT_SandBSpread = GameObject.Find("BSpreadSand").GetComponent<Slider>();
        INPUT_GrassBSpread = GameObject.Find("BSpreadGrass").GetComponent<Slider>();
        INPUT_SnowBSpread = GameObject.Find("BSpreadSnow").GetComponent<Slider>();
        INPUT_WaterBSpread = GameObject.Find("BSpreadOcean").GetComponent<Slider>();
        INPUT_ForestBSpread = GameObject.Find("BSpreadForest").GetComponent<Slider>();
        INPUT_ShallowWaterBSpread = GameObject.Find("BSpreadShallowWater").GetComponent<Slider>();
        VALUE_SandBSpread = GameObject.Find("OutputSandBSpread").GetComponent<Text>();
        VALUE_GrassBSpread = GameObject.Find("OutputGrassBSpread").GetComponent<Text>();
        VALUE_SnowBSpread = GameObject.Find("OutputSnowBSpread").GetComponent<Text>();
        VALUE_WaterBSpread = GameObject.Find("OutputOceanBSpread").GetComponent<Text>();
        VALUE_ForestBSpread = GameObject.Find("OutputForestBSpread").GetComponent<Text>();
        VALUE_ShallowWaterBSpread = GameObject.Find("OutputShallowWaterBSpread").GetComponent<Text>();

        // Current seed and biome seed values for this generation, to be used to record and replicate good generations.
        CurrentBiomeSeed = GameObject.Find("CurrentBiomeSeed").GetComponent<Text>();
        CurrentSeed = GameObject.Find("CurrentSeed").GetComponent<Text>();

        // Saves some starter variables so no invalid/null values on application start.
        SaveVariables();

        // Set inactive once all the references are collected, otherwise null ref error.
        ModifyVariableCanvas.SetActive(false);
    }

    void Update()
    {
        // If modify variables UI window is open, then continuously update the variable values to the user.
        if (ModifyVariableCanvas.activeSelf)
        {
            VALUE_LloydRelaxation.text = "Lloyd Relaxation Iterations: " + INPUT_LloydRelaxation.value;
            
            VALUE_CollapsedNeighbour.text = "Collapsed Neighbour Threshold: >= " + INPUT_CollapsedNeighbour.value;
            
            VALUE_OutputTitle.text = "Output Dimensions: " + INPUT_OutputX.text + " x " + INPUT_OutputY.text;
            
            VALUE_Seed.text = "Seed (0 = Random): " + INPUT_Seed.text;
            VALUE_BiomeSeed.text = "Biome Spread Seed (0 = Random): " + INPUT_BiomeSeed.text;
            
            VALUE_Regions.text = "Voronoi Regions: " + INPUT_Regions.text;

            VALUE_Iterations.text = "WFC Generation Iterations: " + INPUT_Iterations.value;

            VALUE_SandDistri.text = INPUT_SandDistri.value.ToString();
            VALUE_GrassDistri.text = INPUT_GrassDistri.value.ToString();
            VALUE_SnowDistri.text = INPUT_SnowDistri.value.ToString();
            VALUE_WaterDistri.text = INPUT_WaterDistri.value.ToString();
            VALUE_ForestDistri.text = INPUT_ForestDistri.value.ToString();
            VALUE_ShallowWaterDistri.text = INPUT_ShallowWaterDistri.value.ToString();

            VALUE_SandBSpread.text = INPUT_SandBSpread.value.ToString();
            VALUE_GrassBSpread.text = INPUT_GrassBSpread.value.ToString();
            VALUE_SnowBSpread.text = INPUT_SnowBSpread.value.ToString();
            VALUE_WaterBSpread.text = INPUT_WaterBSpread.value.ToString();
            VALUE_ForestBSpread.text = INPUT_ForestBSpread.value.ToString();
            VALUE_ShallowWaterBSpread.text = INPUT_ShallowWaterBSpread.value.ToString();
        }
    }


    /// <summary>
    /// Teleports WFC outupt away to show the Voronoi Regions behind it. Clicking again teleports the output back.
    /// </summary>
    public void toggleWFC()
    {
        if (WFCGenerator.gameObject.transform.position == Vector3.zero)
        {
            WFCGenerator.gameObject.transform.position = new Vector3(1000, 1000, 0);
        }
        else
        {
            WFCGenerator.gameObject.transform.position = Vector3.zero;
        }
    }

    /// <summary>
    /// Spawn a new Voronoi diagram set.
    /// </summary>
    public void VoronoiGenerate()
    {
        voronoiGenerator.GenerateNewVoronoiDiagram();
    }

    /// <summary>
    /// Perform Lloyd Relaxation on the current Voronoi diagram.
    /// </summary>
    public void VoronoiLloydRelaxation()
    {
        voronoiGenerator.LloydRelaxation();
    }

    /// <summary>
    /// Perform biome correction across a Voronoi diagram.
    /// </summary>
    public void VoronoiBiomeCorrect()
    {
        voronoiGenerator.CheckandCorrectBiomeColourAdjacency();
    }

    /// <summary>
    /// Generate a WFC output.
    /// </summary>
    public void WFCGenerate()
    {
        if (WFCGenerator.gameObject.transform.position != Vector3.zero)
        {
            WFCGenerator.gameObject.transform.position = Vector3.zero;
        }

        // Changes to how the WFC algorithm runs in the update loop means that 0 iterations (instantly generating then drawing w/o animation) leads to crashes.
        if (WFCGenerator.iterations > 0)
        {
            WFCGenerator.contradictionCounter = 0;
            WFCGenerator.generate = true;
            WFCGenerator.Generate();
        }
    }

    /// <summary>
    /// OPens the variable modification window while closing the window that handles the algorithm.
    /// </summary>
    public void ModifyVariables()
    {
        AlgorithmCanvas.SetActive(false);
        ModifyVariableCanvas.SetActive(true);
    }

    /// <summary>
    /// Closes the variable modification window while opening the window that handles the algorithm.
    /// </summary>
    public void EndModifyVariables()
    {
        SaveVariables();

        AlgorithmCanvas.SetActive(true);
        ModifyVariableCanvas.SetActive(false);
    }

    /// <summary>
    /// All variables are updated as per user input. Called once the user leaves the variable modification window.
    /// </summary>
    private void SaveVariables()
    {
        voronoiGenerator.LloydIterations = (int) INPUT_LloydRelaxation.value;

        voronoiGenerator.Regions = Int32.Parse(INPUT_Regions.text);

        double[] regionDist = new double[6];
        regionDist[0] = INPUT_SnowDistri.value;
        regionDist[1] = INPUT_SandDistri.value;
        regionDist[2] = INPUT_GrassDistri.value;
        regionDist[3] = INPUT_WaterDistri.value;
        regionDist[4] = INPUT_ForestDistri.value;
        regionDist[5] = INPUT_ShallowWaterDistri.value;
        voronoiGenerator.RegionWeight = regionDist;

        Vector2 outputSize = new Vector2(Int32.Parse(INPUT_OutputX.text), Int32.Parse(INPUT_OutputY.text));
        voronoiGenerator.Size = outputSize;
        WFCGenerator.width = (int) outputSize.x;
        WFCGenerator.depth = (int) outputSize.y;

        WFCGenerator.seed = Int32.Parse(INPUT_Seed.text);
        WFCGenerator.BiomeSpreadSeed = Int32.Parse(INPUT_BiomeSeed.text);

        WFCGenerator.iterations = (int) INPUT_Iterations.value;

        WFCGenerator.collapsedNeighbourThreshold = (int) INPUT_CollapsedNeighbour.value;

        float[] biomeSpreadValues = new float[6];
        biomeSpreadValues[0] = INPUT_SnowBSpread.value;
        biomeSpreadValues[1] = INPUT_WaterBSpread.value;
        biomeSpreadValues[2] = INPUT_ForestBSpread.value;
        biomeSpreadValues[3] = INPUT_ShallowWaterBSpread.value;
        biomeSpreadValues[4] = INPUT_GrassBSpread.value;
        biomeSpreadValues[5] = INPUT_SandBSpread.value;
        WFCGenerator.BiomeSpreadValues = biomeSpreadValues;
    }
}