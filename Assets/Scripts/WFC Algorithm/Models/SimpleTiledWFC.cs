using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class SimpleTiledWFC : MonoBehaviour{
	
	/// <summary>
	/// XML STRUCTURE:
	/// TILES: UNIQUE TILES.
	/// NEIGHBORS: ADJACENT TILES; HORIZONTALLY (WHEN TILE NAME = 0) AND VERTICALLY (WHEN TILE NAME = 1).
    /// </summary>
	public TextAsset xml = null;
	private string subset = "";

	public int gridsize = 1;
	public int width = 20;
	public int depth = 20;

	public int seed = 0;
	public bool periodic = false;
	public int iterations = 0;
	public bool incremental;

	public SimpleTiledModel model = null;
	public GameObject[,] rendering;
	public GameObject output;
	private Transform group;
	public Dictionary<string, GameObject> obmap = new Dictionary<string, GameObject>();
    private bool undrawn = true;

	public void destroyChildren (){
		foreach (Transform child in this.transform) {
     		GameObject.DestroyImmediate(child.gameObject);
 		}
 	}

 	void Start(){
		Generate();
		Run();
	}

	void Update(){
		if (incremental){
			Run();
		}
	}


	public void Run(){
		if (model == null){return;}
        if (undrawn == false) { return; }
        if (model.Run(seed, iterations)){
			Draw();
		}
	}

	public void OnDrawGizmos(){
		Gizmos.matrix = transform.localToWorldMatrix;
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireCube(new Vector3(width*gridsize/2f-gridsize*0.5f, depth*gridsize/2f-gridsize*0.5f, 0f),new Vector3(width*gridsize, depth*gridsize, gridsize));
	}

	public void Generate(){
		obmap = new  Dictionary<string, GameObject>();

		if (output == null){
			Transform ot = transform.Find("output-tiled");
			if (ot != null){output = ot.gameObject;}}
		if (output == null){
			output = new GameObject("output-tiled");
			output.transform.parent = transform;
			output.transform.position = this.gameObject.transform.position;
			output.transform.rotation = this.gameObject.transform.rotation;}

		for (int i = 0; i < output.transform.childCount; i++){
			GameObject go = output.transform.GetChild(i).gameObject;
			if (Application.isPlaying){Destroy(go);} else {DestroyImmediate(go);}
		}
		group = new GameObject(xml.name).transform;
		group.parent = output.transform;
		group.position = output.transform.position;
		group.rotation = output.transform.rotation;
        group.localScale = new Vector3(1f, 1f, 1f);
        rendering = new GameObject[width, depth];
		this.model = new SimpleTiledModel(xml.text, subset, width, depth, periodic);
        undrawn = true;
    }

	public void Draw(){
		if (output == null){return;}
		if (group == null){return;}
        undrawn = false;

		// Loop through the output in terms of it's 2D dimensions: width and height (depth).
		for (int y = 0; y < depth; y++){
			for (int x = 0; x < width; x++){ 
				// Checks if the tile has already been drawn yet before each draw iteration (Rendering is the array of drawn output tiles).
				if (rendering[x,y] == null){
					string v = model.Sample(x, y); // Retrieve the tile name which the model has calculated earlier on at the current coordinate setting.
					int rot = 0;
					GameObject fab = null;
					if (v != "?"){ // If the sample function returned "?", it means that the tile has not been collapsed yet (Should not occur as long as the model has been run previously).
						rot = int.Parse(v.Substring(0,1)); // Retrieve the first char of recorded tile name, which is it's rotation...
						v = v.Substring(1);                               // And then remove this rotational information from the tile name, leaving only it's name as inputted.

						if (!obmap.ContainsKey(v)){ // Add this tile to a list that holds all unique tiles featured in the output (Excluding rotations).
							fab = (GameObject)Resources.Load(v, typeof(GameObject));
							obmap[v] = fab;
						} else {
							fab = obmap[v];
						}
						if (fab == null){ // If the tile could not be retrieved from above, skip placing it (Error handling that should not normally trigger).
							continue;}
						Vector3 pos = new Vector3(x*gridsize, y*gridsize, 0f); // Set tile position to coordinates, scaled by the grid size attribute.
						GameObject tile = (GameObject)Instantiate(fab, new Vector3() , Quaternion.identity); // Spawn tile and set parent and scale.
						Vector3 fscale = tile.transform.localScale;
						tile.transform.parent = group;
						tile.transform.localPosition = pos;
						tile.transform.localEulerAngles = new Vector3(0, 0, 360-(rot*90)); // If the tile has a rotation, rotate it by it's rotational value.
						tile.transform.localScale = fscale;
						rendering[x,y] = tile; // Tile has been fully instantiated, so add it to the array that holds the tile outputs.
					} else
                    {
                        undrawn = true;
                    }
				}
			}
  		}	
	}
}

#if UNITY_EDITOR
[CustomEditor (typeof(SimpleTiledWFC))]
public class TileSetEditor : Editor {
	public override void OnInspectorGUI () {
		SimpleTiledWFC me = (SimpleTiledWFC)target;
		if (me.xml != null){
			if(GUILayout.Button("generate")){
				me.Generate();
			}
			if (me.model != null){
				if(GUILayout.Button("RUN")){
					me.model.Run(me.seed, me.iterations);
					me.Draw();
				}
			}
		}
		DrawDefaultInspector ();
	}
}
#endif