using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Voronoi : MonoBehaviour
{
    protected GameObject[] voronoiBiomes; // 1D list of biome tiles.

    [SerializeField] private GameObject blankTile; // White tile representing unassigned biome.

    [SerializeField] private GameObject biomedTile;

    private bool _isGenerated; // Is the tile set generated already?

    public Transform _PARENT; // Object parent these biome tiles will be set as the child of.

    // screen fitting values: Height: 90   Width: 160

    /// <summary>
    /// Instantiate or set an existing voronoi tileset active.
    /// </summary>
    public void SpawnVoronoi()
    {
        _PARENT = GameObject.Find("VORONOI").transform;

        var width = GameObject.Find("Output").GetComponent<SimpleTiledWFC>().width;
        var height = GameObject.Find("Output").GetComponent<SimpleTiledWFC>().depth; // why is it named depth

        _isGenerated = voronoiBiomes.Length > 0 && voronoiBiomes[0] != null; // Determine if the biome tiles currently exist or not by checking if the array is initialised
                                                                             // then checking if the 1st tile is instantiated as a benchmark.

        // Instantiate the array if it did not exist already.
        if (!_isGenerated)
        {
            voronoiBiomes = new GameObject[width * height];
        }

        int x = 0;
        int y = 0;

        for (var i = 0; i < width * height; i++)
        {
            if (_isGenerated) // If tiles are already generated, then just reactivate.
            {
                voronoiBiomes[i].SetActive(true);
            }
            else // Otherwise, instantiate each tile at positions w.r.t the tile output but behind it.
            {
                voronoiBiomes[i] = Instantiate(blankTile,
                    new Vector3((2 * x), 2 * y, 5),
                    Quaternion.identity);
                voronoiBiomes[i].transform.SetParent(_PARENT);

                voronoiBiomes[i].SetActive(true);
            }

            // Increment x and increment y every time the width extreme has been reached.
            x++;
            if (x % width == 0)
            {
                y++;
                x = 0;
            }
        }
    }

    /// <summary>
    /// Destroy the biome tiles. Useful for resetting it to re-initiate to adjust the map bounds.
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < voronoiBiomes.Length; i++)
        {
            DestroyImmediate(voronoiBiomes[i]);
        }
    }



    // Handles the generation of UI buttons in the Unity Editor and the functions each button calls.
#if UNITY_EDITOR
    [CustomEditor(typeof(Voronoi))]
    public class VoronoiEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            Voronoi _THIS = (Voronoi)target;
            if (GUILayout.Button("Spawn Voronoi"))
            {
                _THIS.SpawnVoronoi();
            }

            if (GUILayout.Button("Clear Voronoi"))
            {
                _THIS.Clear();
            }
            DrawDefaultInspector();
        }
    }
#endif
}
