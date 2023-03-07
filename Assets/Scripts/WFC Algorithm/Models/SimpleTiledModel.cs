/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;
using System.Xml;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Simple Tiled Model implementation used in the Wave Function Collapse algorithm.
/// </summary>
public class SimpleTiledModel : Model
{
    /// <summary>
	/// All tiles
	/// </summary>
    public List<string> tiles;

	public SimpleTiledModel(string name, string subsetName, int width, int height, bool periodic)
        :base(width,height)
    {
		
		
		this.periodic = periodic;

        // Loads an XML file, which stores:
        // - Tiles to be used in this current model,
		// - Rules per tile, such as neighboring tiles.
        var xdoc = new XmlDocument();
		xdoc.LoadXml(name);
		XmlNode xnode = xdoc.FirstChild;

        // Unique designates a tile which does not have any rotations.
        bool unique = xnode.Get("unique", false);
		
		xnode = xnode.FirstChild;

		// Subset indicates that this tile ruleset is borrowing a pre-defined tile subset, which are reused to generate different models that utilise similar tiles and tile rules.
        List<string> subset = null;

		// If a subset has been defined, then find the subset(s) utilized.
        if (subsetName != "")
		{
			subset = new List<string>();
            foreach (XmlNode xsubset in xnode.NextSibling.NextSibling.ChildNodes) 
				if (xsubset.NodeType != XmlNodeType.Comment && xsubset.Get<string>("name") == subsetName)
					foreach (XmlNode stile in xsubset.ChildNodes) subset.Add(stile.Get<string>("name"));
		}

        // Increment rotate, where each increment is a rotation of 90 degrees clockwise.
        Func<string, string> rotate = (n) =>{
			int rot = int.Parse(n.Substring(0,1))+1;
			return ""+rot+n.Substring(1);
		};

		tiles = new List<string>();
		var tempStationary = new List<double>();

		List<int[]> action = new List<int[]>();
		Dictionary<string, int> firstOccurrence = new Dictionary<string, int>();


		// For each tile that exists in the tile ruleset, add to the tile list.
        foreach (XmlNode xtile in xnode.ChildNodes)
		{
			// Get tile name.
			string tilename = xtile.Get<string>("name");

            // If tile is not in the subset, then skip.
            if (subset != null && !subset.Contains(tilename)) continue;

			Func<int, int> a, b;
			int cardinality;

			// Retrieve tile symmetry, which is how the tile can be rotated/flipped.
			char sym = xtile.Get("symmetry", 'X');

            // Switch through pre-set symmetry methods:

            // L symmetry, where the tile can be rotated 90 degrees clockwise, and flipped horizontally.
            if (sym == 'L')
			{
				cardinality = 4;
				a = i => (i + 1) % 4;
				b = i => i % 2 == 0 ? i + 1 : i - 1;
			}

            // T symmetry, where the tile can be rotated 90 degrees clockwise, and flipped vertically.
            else if (sym == 'T')
			{
				cardinality = 4;
				a = i => (i + 1) % 4;
				b = i => i % 2 == 0 ? i : 4 - i;
			}

			// I Symmetry, which represents a central tile with 2 tiles underneath it.
			else if (sym == 'I')
			{
				cardinality = 2;
				a = i => 1 - i;
				b = i => i;
			}

			// D symmetry- Unknown.
			else if (sym == 'D')
			{
				cardinality = 2;
				a = i => 1 - i;
				b = i => 1 - i;
			}

			// X symmetry- None defined.
			else
			{
				cardinality = 1;
				a = i => i;
				b = i => i;
			}

			T = action.Count;
			
			firstOccurrence.Add(tilename, T);
			
			int[][] map = new int[cardinality][];

            // For this tile's unique cardinality (rotations), add to the action list.
            for (int t = 0; t < cardinality; t++)
			{
				map[t] = new int[8];

				map[t][0] = t;
				map[t][1] = a(t);
				map[t][2] = a(a(t));
				map[t][3] = a(a(a(t)));
				map[t][4] = b(t);
				map[t][5] = b(a(t));
				map[t][6] = b(a(a(t)));
				map[t][7] = b(a(a(a(t))));

				for (int s = 0; s < 8; s++) map[t][s] += T;

				action.Add(map[t]);
			}

            // If designated as unique, do not record additional tiles (Which represent tiles rotated). Otherwise, record rotations as unique tiles.
            if (unique)
			{
				for (int t = 0; t < cardinality; t++)
				{
					tiles.Add(""+"0"+tilename);
				}
			}
			else
			{
				tiles.Add("0"+tilename);
				for (int t = 1; t < cardinality; t++) tiles.Add(rotate(tiles[T + t - 1]));
			}

            // Retrieve tile's weight.
            for (int t = 0; t < cardinality; t++) tempStationary.Add(xtile.Get("weight", 1.0f));
		}

		T = action.Count;
		weights = tempStationary.ToArray();

        propagator = new int[4][][];
        var tempPropagator = new bool[4][][];
		for (int d = 0; d < 4; d++)
		{
            tempPropagator[d] = new bool[T][];
            propagator[d] = new int[T][];
            for (int t = 0; t < T; t++) tempPropagator[d][t] = new bool[T];
        }

		// Inspect neighbors
        foreach (XmlNode xneighbor in xnode.NextSibling.ChildNodes)
		{
			// Retrieve neighbors
			string[] left = xneighbor.Get<string>("left").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			string[] right = xneighbor.Get<string>("right").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			// Ensure that tile has 2 neighbors on opposite sides.
			if (subset != null && (!subset.Contains(left[0]) || !subset.Contains(right[0]))) continue;

            int L = action[firstOccurrence[string.Join(" ", left.Take(left.Length - 1).ToArray())]][left.Length == 1 ? 0 : int.Parse(left.Last())], D = action[L][1];
			int R = action[firstOccurrence[string.Join(" ", right.Take(right.Length - 1).ToArray())]][right.Length == 1 ? 0 : int.Parse(right.Last())], U = action[R][1];

            tempPropagator[0][R][L] = true;
            tempPropagator[0][action[R][6]][action[L][6]] = true;
            tempPropagator[0][action[L][4]][action[R][4]] = true;
            tempPropagator[0][action[L][2]][action[R][2]] = true;

            tempPropagator[1][U][D] = true;
            tempPropagator[1][action[D][6]][action[U][6]] = true;
            tempPropagator[1][action[U][4]][action[D][4]] = true;
            tempPropagator[1][action[D][2]][action[U][2]] = true;
		}

		for (int t2 = 0; t2 < T; t2++) for (int t1 = 0; t1 < T; t1++)
			{
                tempPropagator[2][t2][t1] = tempPropagator[0][t1][t2];
                tempPropagator[3][t2][t1] = tempPropagator[1][t1][t2];
			}

        List<int>[][] sparsePropagator = new List<int>[4][];
        for (int d = 0; d < 4; d++)
        {
            sparsePropagator[d] = new List<int>[T];
            for (int t = 0; t < T; t++) sparsePropagator[d][t] = new List<int>();
        }

        for (int d = 0; d < 4; d++) for (int t1 = 0; t1 < T; t1++)
            {
                List<int> sp = sparsePropagator[d][t1];
                bool[] tp = tempPropagator[d][t1];

                for (int t2 = 0; t2 < T; t2++) if (tp[t2]) sp.Add(t2);

                int ST = sp.Count;
                propagator[d][t1] = new int[ST];
                for (int st = 0; st < ST; st++) propagator[d][t1][st] = sp[st];
            }
        }



	/// <summary>
	/// Retrieve a given tile's name at a set coordinate.
	/// </summary>
	/// <param name="x">x pos</param>
	/// <param name="y">y pos</param>
	/// <returns>Tile name or "?" if the tile could not be found at the given coordinate position.</returns>
	public string Sample(int x, int y){
		bool found = false;
		string res = "?"; // Init return value to the error value, to be replaced with the correct tile name if it is found.
		
		// Loops through all the potential tiles this given tile could be.
		// If the tile is found, the return value is set to that tile's name, and found is set to true. If this same tile at this position
		// is found to also be another tile, it means it has not been collapsed fully yet (> 1 possibilities) and so the return type is set to "?" as the tile is not determined yet.
		for (int t = 0; t < T; t++) if (wave[x + y * FMX][t])
        {
			if (found) {return "?";}
			found = true;
			res = tiles[t];
		}
		return res;
	}

	protected override bool OnBoundary(int x, int y){
		return !periodic && (x < 0 || y < 0 || x >= FMX || y >= FMY);
	}

}