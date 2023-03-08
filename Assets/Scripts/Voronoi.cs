using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Voronoi : MonoBehaviour
{
    public GameObject[] voronoiBiomes; // 1D list of biome tiles.

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

        var tiledWFC = GameObject.Find("Output").GetComponent<SimpleTiledWFC>();

        var width = tiledWFC.width;
        var height = tiledWFC.depth; // why is it named depth

        var tileSize = tiledWFC.gridsize;

        if (voronoiBiomes == null)
        {
            _isGenerated = false;
        }
        else
        {
            _isGenerated = voronoiBiomes.Length > 0 && voronoiBiomes[0] != null;
        }

        // Instantiate the array if it did not exist already.
        if (!_isGenerated)
        {
            voronoiBiomes = new GameObject[width * height];
        }
        else
        {
            return;
        }

        int x = 0;
        int y = 0;

        for (var i = 0; i < width * height; i++)
        {
            if ((x >= 4 && x <= 8) && (y >= 1 && y <= 4) ||
                (x >= 1 && x <= 5) && (y >= 6 && y <= 9))
            {
                voronoiBiomes[i] = Instantiate(biomedTile,
                    new Vector3((tileSize * x), tileSize * y, 5),
                    Quaternion.identity);
                voronoiBiomes[i].transform.SetParent(_PARENT);
            }
            else
            {

                // Create a new biome tile at this position (Scaled so that it fits the output boundaries).
                voronoiBiomes[i] = Instantiate(blankTile,
                    new Vector3((tileSize * x), tileSize * y, 5),
                    Quaternion.identity);
                voronoiBiomes[i].transform.SetParent(_PARENT);
            }

            voronoiBiomes[i].SetActive(true);

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
        // While loop used as it may not destroy all children in one clear iteration.
        while (_PARENT.childCount > 0)
        {
            foreach (Transform child in _PARENT)
            {
                DestroyImmediate(child.gameObject);
            }
        }
        // De-reference the array.
        voronoiBiomes = null;
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
