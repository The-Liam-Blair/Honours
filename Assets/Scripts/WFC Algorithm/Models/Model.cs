/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/


/*
 *<set>
  <tiles>
        <tile name="FinalTiles/ShallowWater" symmetry="X" weight="1.0"/>
        <tile name="FinalTiles/Water" symmetry="X" weight="1.0"/>
        <tile name="FinalTiles/ShallowWaterLTile" symmetry="L" weight="1.0"/>
        <tile name="FinalTiles/WaterLTile" symmetry="L" weight="0.01"/>
        <tile name="FinalTiles/GrassLTile" symmetry="L" weight="1.0"/>
        <tile name="FinalTiles/SandLTile" symmetry="L" weight="0.01"/>
        <tile name="FinalTiles/Sand" symmetry="X" weight="1.0"/>
        <tile name="FinalTiles/Grass" symmetry="X" weight="1.0"/>
        <tile name="FinalTiles/Snow" symmetry="X" weight="1.0"/>
        <tile name="FinalTiles/SnowLTile" symmetry="L" weight="0.01"/>
        <tile name="FinalTiles/Forest" symmetry="X" weight="1.0"/>
        <tile name="FinalTiles/ForestLTile" symmetry="L" weight="1.0"/>
	</tiles>
 */

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public abstract class Model
{
    protected bool[][] wave;

    protected int[][][] propagator;
    int[][][] compatible;
    protected int[] observed;

    protected bool init = false;

    Tuple<int, int>[] stack;
    int stacksize;

    protected System.Random random;
    protected System.Random biomeSpreadRandom;
    protected int FMX, FMY, T;
    protected bool periodic;
    
    protected double[] weights;
    double[] weightLogWeights;

    int[] sumsOfOnes;
    double sumOfWeights, sumOfWeightLogWeights, startingEntropy;
    double[] sumsOfWeights, sumsOfWeightLogWeights, entropies;

    private double[] biomeWeights;

    public int collapsedNeighbourThreshold = 1;

    public float[] BiomeSpreadValues = new float[6];




    public bool completed;
    public bool failed;

    protected Model(int width, int height)
    {
        FMX = width;
        FMY = height;
    }

    void Init()
    {
        completed = false;
        wave = new bool[FMX * FMY][];
        compatible = new int[wave.Length][][];
        for (int i = 0; i < wave.Length; i++)
        {
            wave[i] = new bool[T];
            compatible[i] = new int[T][];
            for (int t = 0; t < T; t++) compatible[i][t] = new int[4];
        }

        weightLogWeights = new double[T];
        sumOfWeights = 0;
        sumOfWeightLogWeights = 0;

        for (int t = 0; t < T; t++)
        {
            weightLogWeights[t] = weights[t] * Math.Log(weights[t]);
            sumOfWeights += weights[t];
            sumOfWeightLogWeights += weightLogWeights[t];
        }

        startingEntropy = Math.Log(sumOfWeights) - sumOfWeightLogWeights / sumOfWeights;

        sumsOfOnes = new int[FMX * FMY];
        sumsOfWeights = new double[FMX * FMY];
        sumsOfWeightLogWeights = new double[FMX * FMY];
        entropies = new double[FMX * FMY];

        stack = new Tuple<int, int>[wave.Length * T];
        stacksize = 0;

        biomeWeights = GameObject.Find("Voronoi").GetComponent<VoronoiBiomes>().RegionWeight;

    }



    bool? Observe()
    {
        double min = 1E+3;
        int argmin = -1;

        // For each un-collapsed tile...
        for (int i = 0; i < wave.Length; i++)
        {
            if (OnBoundary(i % FMX, i / FMX)) continue;

            int amount = sumsOfOnes[i]; // Get the number of superpositions for this current tile, the number of states it still could be.
            if (amount == 0)
            {
                return false; // 0 potential states means that a contradiction has been encountered, and the algorithm has failed; stop execution.
            }

            double entropy = entropies[i]; // Get the entropy for this tile.
            if (amount > 1 && entropy <= min) // If this tile still un-collapsed (>1 remaining states) and it's entropy is greater than any other recorded tile's entropy...
            {
                double noise = 1E-6 * random.NextDouble(); // Add an extremely small number to the entropy for randomness.

                // If the modified entropy is less than the current minimum entropy, update the minimum entropy and the tile with the minimum entropy.
                if (entropy + noise < min)
                {
                    min = entropy + noise;
                    argmin = i;
                }
            }
        }

        // If argMin was not modified, it means that it did not find any tiles that had to be collapsed, so the algorithm has succeeded.
        if (argmin == -1)
        {
            observed = new int[FMX * FMY];
            completed = true;
            for (int i = 0; i < wave.Length; i++) for (int t = 0; t < T; t++) if (wave[i][t]) { observed[i] = t; break; } // Observed records the tile set with collapsed states only.
            return true;																								  // But is however unused...
        }

        double[] distribution = new double[T];
        for (int t = 0; t < T; t++)
        {
            distribution[t] = wave[argmin][t] ? weights[t] : 0; // Set the weights of the distribution according to the tile weights, if they are still valid potential states.

        }

        RaycastHit hit;
        Physics.Raycast(new Vector3(argmin % FMX, argmin / FMX), Vector3.forward, out hit, 8f);


        // For unrotated tiles: Use the "AdjacencyATileset.xml" and use of Voronoi Regions to properly use this algorithm.
        
        // For rotated tiles: Use the "RotatedTileset.xml" WITHOUT Voronoi Regions to properly use this algorithm.
        // If using rotated tileset, all the ray cast sections can be commented out for efficiency but leaving it uncommented will not impact the output quality.

        // Neighbours of the current tile, in the order: above, below, left, right.
        double[][] neighbours = new double[4][];
        for (int i = 0; i < 4; i++) neighbours[i] = new double[T];

        // 1D positions of each neighbour.
        int above = argmin + FMX;
        int below = argmin - FMX;
        int left = argmin - 1;
        int right = argmin + 1;

        // Check whether the neighbouring tiles are not out of bounds in the 2d grid, so edge tiles will only have 3 neighbours and corner tiles will have 2.
        if (above >= wave.Length)  { above = -1; }
        if (below < 0)             { below = -1; }
        if (left % FMX == FMX - 1) { left = -1;  }
        if (right % FMX == 0)      { right = -1; }


        // Loop through all the distributions for all neighbours and get their current weights. Skip tiles that are out of bounds.
        for (int t = 0; t < T; t++)
        {
            if (above != -1) { neighbours[0][t] = wave[above][t] ? weights[t] : 0; }
            if (below != -1) { neighbours[1][t] = wave[below][t] ? weights[t] : 0; }
            if (left != -1)  { neighbours[2][t] = wave[left][t] ? weights[t] : 0;  }
            if (right != -1) { neighbours[3][t] = wave[right][t] ? weights[t] : 0; }
        }

        // Find number of neighbours whose states are known already.
        int collapsedNeighbours = 0;
        int[] collapsedNeighbourBiomes = new int[4];

        for (int i = 0; i < 4; i++)
        {
            if (IsCollapsed(neighbours[i]))
            {
                collapsedNeighbours++;
                collapsedNeighbourBiomes[i] = Array.IndexOf(neighbours[i], 1.0f);
            }
            else
            {
                collapsedNeighbourBiomes[i] = -1;
            }
        }

        // BIOME DICTIONARY:
        // 0 - Snow
        // 1 - Water
        // 2 - Forest
        // 3 - Shallow Water
        // 4 - Grass
        // 5 - Sand

        if (hit.transform != null)
        {
            switch (hit.transform.gameObject.tag)
            {
                // If the Voronoi tile at this position was grass...
                case "grass":
                    // Roll a random number between 0 and 1 and compare to this biome's biome spread value. If the roll is less than the spread (fail)...
                    if (biomeSpreadRandom.NextDouble() <= 1 - BiomeSpreadValues[4])
                    {
                            // This tile is set to grass.
                            for (int i = 0; i < distribution.Length; i++)
                            {
                                if (i == 4)
                                {
                                    distribution[i] = 1;
                                }
                                else
                                {
                                    distribution[i] = 0;
                                }
                            }
                    }
                    // Otherwise (Roll succeeds)...
                    else
                    {
                        // Check whether this tile qualifies for biome spread via checking the collapsed neighbour threshold.
                        // If check succeeds, modify this tile through biome spread.
                        if (collapsedNeighbours >= collapsedNeighbourThreshold)
                        {
                            PerformBiomeSpread(argmin, collapsedNeighbourBiomes, distribution);
                        }
                        
                        // Else (if check fails), Go back to original and set tile to grass.
                        else
                        {
                            for (int i = 0; i < distribution.Length; i++)
                            {
                                if (i == 4)
                                {
                                    distribution[i] = 1;
                                }
                                else
                                {
                                    distribution[i] = 0;
                                }
                            }
                        }
                    }
                    break;

                case "sand":
                    if (biomeSpreadRandom.NextDouble() <= 1 - BiomeSpreadValues[5])
                    {
                            for (int i = 0; i < distribution.Length; i++)
                            {
                                if (i == 5)
                                {
                                    distribution[i] = 1;
                                }
                                else
                                {
                                    distribution[i] = 0;
                                }
                            }
                    }
                    else
                    {
                        if (collapsedNeighbours >= collapsedNeighbourThreshold)
                        {
                            PerformBiomeSpread(argmin, collapsedNeighbourBiomes, distribution);
                        }
                        else
                        {
                            for (int i = 0; i < distribution.Length; i++)
                            {
                                if (i == 5)
                                {
                                    distribution[i] = 1;
                                }
                                else
                                {
                                    distribution[i] = 0;
                                }
                            }
                        }
                    }
                    break;

                case "water":
                    if (biomeSpreadRandom.NextDouble() <= 1 - BiomeSpreadValues[1])
                    {
                        for (int i = 0; i < distribution.Length; i++)
                        {
                            if (i == 1)
                            {
                                distribution[i] = 1;
                            }
                            else
                            {
                                distribution[i] = 0;
                            }
                        }
                    }
                    else
                    {
                        if (collapsedNeighbours >= collapsedNeighbourThreshold)
                        {
                            PerformBiomeSpread(argmin, collapsedNeighbourBiomes, distribution);
                        }
                        else
                        {
                            for (int i = 0; i < distribution.Length; i++)
                            {
                                if (i == 1)
                                {
                                    distribution[i] = 1;
                                }
                                else
                                {
                                    distribution[i] = 0;
                                }
                            }
                        }
                    }
                    break;

                case "snow":
                    if (biomeSpreadRandom.NextDouble() <= 1 - BiomeSpreadValues[0])
                    {
                        for (int i = 0; i < distribution.Length; i++)
                        {
                            if (i == 0)
                            {
                                distribution[i] = 1;
                            }
                            else
                            {
                                distribution[i] = 0;
                            }
                        }
                    }
                    else
                    {
                        if (collapsedNeighbours >= collapsedNeighbourThreshold)
                        {
                            PerformBiomeSpread(argmin, collapsedNeighbourBiomes, distribution);
                        }
                        else
                        {
                            for (int i = 0; i < distribution.Length; i++)
                            {
                                if (i == 0)
                                {
                                    distribution[i] = 1;
                                }
                                else
                                {
                                    distribution[i] = 0;
                                }
                            }
                        }
                    }
                    break;

                case "forest":
                    if (biomeSpreadRandom.NextDouble() <= 1 - BiomeSpreadValues[2])
                    {
                        for (int i = 0; i < distribution.Length; i++)
                        {
                            if (i == 2)
                            {
                                distribution[i] = 1;
                            }
                            else
                            {
                                distribution[i] = 0;
                            }
                        }
                    }
                    else
                    {
                        if (collapsedNeighbours >= collapsedNeighbourThreshold)
                        {
                            PerformBiomeSpread(argmin, collapsedNeighbourBiomes, distribution);
                        }
                        else
                        {
                            for (int i = 0; i < distribution.Length; i++)
                            {
                                if (i == 2)
                                {
                                    distribution[i] = 1;
                                }
                                else
                                {
                                    distribution[i] = 0;
                                }
                            }
                        }
                    }
                    break;

                case "shallowWater":
                    if (biomeSpreadRandom.NextDouble() <= 1 - BiomeSpreadValues[3])
                    {
                        for (int i = 0; i < distribution.Length; i++)
                        {
                            if (i == 3)
                            {
                                distribution[i] = 1;
                            }
                            else
                            {
                                distribution[i] = 0;
                            }
                        }
                    }
                    else
                    {
                        if (collapsedNeighbours >= collapsedNeighbourThreshold)
                        {
                            PerformBiomeSpread(argmin, collapsedNeighbourBiomes, distribution);
                        }
                        else
                        {
                            for (int i = 0; i < distribution.Length; i++)
                            {
                                if (i == 3)
                                {
                                    distribution[i] = 1;
                                }
                                else
                                {
                                    distribution[i] = 0;
                                }
                            }
                        }
                    }

                    break;
            }
        }

        switch (collapsedNeighbours)
        {
            case 0:
                Debug.DrawRay(new Vector3(argmin % FMX, argmin / FMX, -1), Vector3.back * 3, Color.black, 3);
                break;

            case 1:
                Debug.DrawRay(new Vector3(argmin % FMX, argmin / FMX, -1), Vector3.back * 3, Color.red, 3);
                break;

            case 2:
                Debug.DrawRay(new Vector3(argmin % FMX, argmin / FMX, -1), Vector3.back * 3, Color.yellow, 3);
                break;

            case 3:
                Debug.DrawRay(new Vector3(argmin % FMX, argmin / FMX, -1), Vector3.back * 3, Color.green, 3);
                break;


            case 4:
                Debug.DrawRay(new Vector3(argmin % FMX, argmin / FMX, -1), Vector3.back * 3, Color.magenta, 3);
                break;

        }

                    

        int r = distribution.Random(random.NextDouble()); // Select a random state from the distribution.

        bool[] w = wave[argmin];

        // For each state for this tile...
        for (int t = 0; t < T; t++)
        {
            // If this specific tile's state does not match the randomly selected state, remove it as a potential state.
            if (w[t] != (t == r))
            {
                Ban(argmin, t);
            }
        }
        
        return null; // Null return indicates that the algorithm is still running and has not succeeded or failed yet.
    }

    protected void Propagate()
    {
        // For each tile that may have been affected by the current wave collapse...
        while (stacksize > 0)
        {
            var e1 = stack[stacksize - 1];
            stacksize--; // Decrement stack.

            int i1 = e1.Item1; // Get the original tile's position in the 1D wave array.
            int x1 = i1 % FMX, y1 = i1 / FMX; // Get the original tile's x and y coordinates inside the output.
            bool[] w1 = wave[i1];  // Get the state(s) of the original tile.

            // For each neighboring tile from the original tile...
            for (int d = 0; d < 4; d++)
            {
                int dx = DX[d], dy = DY[d]; // Get the next neighboring tile (DX and DY are pre-determined values that indicate a direction when used together,
                                            // described in the order (Left, Up, Right Down).

                int x2 = x1 + dx, y2 = y1 + dy; // Get the coordinates of this neighboring tile.
                if (OnBoundary(x2, y2)) continue; // If out of bounds, then the tile doesn't exist and move on.

                // Check if the coordinate values of the neighboring tile are within the tile map boundaries, and set it within the boundaries as appropiate.
                if (x2 < 0) x2 += FMX;
                else if (x2 >= FMX) x2 -= FMX;
                if (y2 < 0) y2 += FMY;
                else if (y2 >= FMY) y2 -= FMY;

                int i2 = x2 + y2 * FMX; // Get the neighboring tile's position in the 1D wave array.
                int[] p = propagator[d][e1.Item2]; // Item 2: state, d: Neighbor being judged. Propagator: Set of states per neighbor.
                int[][] compat = compatible[i2];

                for (int l = 0; l < p.Length; l++)
                {
                    int t2 = p[l];
                    int[] comp = compat[t2];

                    comp[d]--;
                    if (comp[d] == 0)
                    {
                        Ban(i2, t2);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Run the WFC algorithm, called by the user.
    /// </summary>
    /// <param name="seed">Specified seed value to duplicate a certain output result.</param>
    /// <param name="limit">Number of iterations per update cycle as defined by the user.</param>
    /// <returns></returns>
    public bool Run(int seed, int BiomeSpreadSeed, int limit)
    {
        if (wave == null || failed)
        {
            failed = false;
            Init(); // Initialise the wave and needed variables if they are not initialized already.
        }
        
        if (!this.init)
        {
            this.init = true;
            failed = false;
            this.Clear(); // Reset any existing variables if they exist.
        }

        // If seed value is set to 0, use a randomized seed when preparing the randomization function. Otherwise, use the specified seed.
        if (seed == 0 && random == null)
        {
            var seedValue = (int)DateTime.Now.Ticks;
            random = new System.Random(seedValue);

            GameObject.Find("CurrentSeed").GetComponent<Text>().text = "Current Seed: " + seedValue;
        }
        else
        {
            random = new System.Random(seed);
        }

        // Seperate seed for determining biome spread to prevent cases where user forgets to change seed, and for general quality of life.
        // Most of the time will be set to random 0 unless the user is trying to replicate a specific output.
        if (BiomeSpreadSeed == 0 && biomeSpreadRandom == null)
        {
            var seedValue = (int)DateTime.Now.Ticks;
            biomeSpreadRandom = new System.Random(seedValue);
            GameObject.Find("CurrentBiomeSeed").GetComponent<Text>().text = "Current Biome Spread Seed: " + seedValue;
        }
        else
        {
            biomeSpreadRandom = new System.Random(BiomeSpreadSeed);
        }

        // For each iteration cycle length as defined by the user...
        for (int l = 0; l < limit || limit == 0; l++)
        {
            // Perform the observation stage; taking a single, least-entropy tile and collapsing it.
            bool? result = Observe();

            // If result returned true (Algorithm succeeded) or false (Algorithm failed), end execution.
            // Once execution has ended, the draw stage will be called to instantiate the tiles in the Unity scene.
            if (result != null) return (bool)result;

            // If algorithm has not finished yet, then perform the propagation stage; collapse neighboring tiles (to the tile collapsed in the observation stage) and handle
            // the chain reaction.
            Propagate();
        }

        return true;
    }

    /// <summary>
    /// Eliminate a tile's state as it is now invalid.
    /// </summary>
    /// <param name="i">Tile being considered (1D array iterator)</param>
    /// <param name="t">Single state of the tile.</param>
    protected void Ban(int i, int t)
    {
        wave[i][t] = false; // Remove the current tile's state from the list of potential states.

        int[] comp = compatible[i][t];
        for (int d = 0; d < 4; d++) comp[d] = 0;
        stack[stacksize] = new Tuple<int, int>(i, t); // Instantiate the stack with the tile and its state, to be used during propagation.
        stacksize++;

        double sum = sumsOfWeights[i]; // Get the combined weights of all valid states for this tile.
        entropies[i] += sumsOfWeightLogWeights[i] / sum - Math.Log(sum); // Set entropy of this tile to 0 temporarily (When sum of log weights = 0).

        sumsOfOnes[i] -= 1; // Remove a single state, decrementing the number of valid states for this tile.
        sumsOfWeights[i] -= weights[t]; // Remove that state's weight from the weight sum.
        sumsOfWeightLogWeights[i] -= weightLogWeights[t]; // Remove that state's natural log of its weight from the sum of natural logs of weights.

        sum = sumsOfWeights[i];
        entropies[i] -= sumsOfWeightLogWeights[i] / sum - Math.Log(sum); // Re-calculate the entropy value again based on the updated weight sum and log weight sum
    }                                                                    // (It will be decreased due to less potential states the tile be).

    protected virtual void Clear()
    {
        for (int i = 0; i < wave.Length; i++)
        {
            for (int t = 0; t < T; t++)
            {
                wave[i][t] = true;
                for (int d = 0; d < 4; d++) compatible[i][t][d] = propagator[opposite[d]][t].Length;
            }

            sumsOfOnes[i] = weights.Length;
            sumsOfWeights[i] = sumOfWeights;
            sumsOfWeightLogWeights[i] = sumOfWeightLogWeights;
            entropies[i] = startingEntropy;
        }
    }

    protected abstract bool OnBoundary(int x, int y);

    protected static int[] DX = { -1, 0, 1, 0 };
    protected static int[] DY = { 0, 1, 0, -1 };
    static int[] opposite = { 2, 3, 0, 1 };

    
    /// <summary>
    /// Check whether a given tile has collapsed by checking that it has one state (with value 1), with all other potential states being set to 0.
    /// </summary>
    /// <param name="tile">Tile being checked for it's state status.</param>
    /// <returns>True if collapsed otherwise false.</returns>
    public bool IsCollapsed(double[] tile)
    {
        bool OneValue = tile.Sum().Equals(1); // True = cumulative sum of values = 1, False = cumulative sum of values < 1.
        for (int i = 0; i < tile.Length; i++)
        {
            // Check each state: If there exists a state where it's value is between 0 and 1, then the tile has > 1 states remaining, ergo not collapsed yet.
            if (tile[i] > 0 && tile[i] < 1)
            {
                OneValue = false;
                break;
            }
        }

        return OneValue;
    }


    /// <summary>
    /// From the random check failing, the tile is no longer collapsed by it's biome but by it's neighbours. This enables a degree of biome spread,
    /// Where, if the tile is adjacent to other collapsed tiles of a different type, that tile can be collapsed to that type instead of it's biome.
    /// </summary>
    /// <param name="argmin">Tile's position on the 1D output array.</param>
    /// <param name="collapsedNeighbourBiomes"> The final states of all collapsed neighbours so far (-1 = uncollapsed, see biome dictionary for reading the values).</param>
    /// <param name="distribution"> Distribution of this tile. (So far).</param>
    public void PerformBiomeSpread(int argmin, int[] collapsedNeighbourBiomes, double[] distribution)
    {
        // count each biome in the neighbours
        int[] biomeCount = new int[6];
        for (int i = 0; i < 4; i++)
        {
            // Record the tile type of each collapsed neighbour (See biome dictionary for reading the values)
            if (collapsedNeighbourBiomes[i] != -1)
            {
                biomeCount[collapsedNeighbourBiomes[i]]++;
            }
        }
        
        Debug.DrawRay(new Vector3(argmin % FMX, argmin / FMX), Vector3.back * 10, Color.green, 8); // If running in editor: points to tiles being modified.
        
        int max = 0;
        int maxIndex = 0;

        // Loop used in case an invalid tile is returned (A tile which is likely to return a contradiction)
        while (true)
        {
            // Perform a find-max operation to find the most common biome in the neighbours.
            for (int i = 0; i < biomeCount.Length; i++)
            {
                if (biomeCount[i] > max)
                {
                    max = biomeCount[i];
                    maxIndex = i;
                }
            }

            // If the most common biome is compatible with this tile (It will not immediately result in a contradiction), then break out of the loop and continue.
            if (distribution[maxIndex].Equals(1))
            {
                break;
            }
            
            // Else remove that tile type from consideration and run through the loop again.
            else
            {
                biomeCount[maxIndex] = 0;
                max = 0;
                maxIndex = 0;
            }
        }

        // The index of the correct tile type to collapse to is now known, so collapse the tile to that type.
        for (int i = 0; i < distribution.Length; i++)
        {
            if (i == maxIndex)
            {
                distribution[i] = 1;
            }
            else
            {
                distribution[i] = 0;
            }
        }
    }
}