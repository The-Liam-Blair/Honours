# 2D Terrain Generation with Wave Function Collapse and Voronoi Regions

![3](https://github.com/The-Liam-Blair/Honours/assets/60526247/c1199bd4-2070-4cb5-adc5-7f57763c30d4)
![1](https://github.com/The-Liam-Blair/Honours/assets/60526247/428f0466-478b-4819-b9cb-f0ca0859d4e8)
![2](https://github.com/The-Liam-Blair/Honours/assets/60526247/43769100-fba3-4202-8768-f6d4f266ff41)
![4](https://github.com/The-Liam-Blair/Honours/assets/60526247/54c188b0-2233-4836-b3f2-228b2488f45c)

<br>

## About
This project served as my Bsc Games Development honours project at Napier University, which aimed to explore additional methods of using the Wave Function Collapse (WFC) algorithm as a method for generating 2D terrain.

Ultimately, the project concluded that the mixture of WFC and Voronoi Regions could sufficiently generate terrain clustered into 'biomes', or seperating terrain by environments, such as seperation of grassy regions and forests. Additionally, the introduction of a custom made algorithm, named 'Biome Spread', allowed the user to perform modifications to their terrain that allowed biomes to naturally expand beyond their set regions, making the terrain appear more natural.

<br>

## Using the Application
[The final build is available for download from the releases tab](https://github.com/The-Liam-Blair/Honours/releases/tag/v1.0.0)!

Unpack the .zip file and run the executable to run the application.

<br>

### Controls
- WASD : Move the camera.
- Q/E  : Zoom in/out.
- X    : Force stop the WFC generation (When the WFC algorithm can't find a solution).
- Esc  : Closes the application.

<br>
<br>

### The Output Window
This is the window you will be presented with on application start. You will have access to a view of your generations and buttons that can generate and modify outputs using Voronoi diagrams and WFC.

The generation is split into two distinct categories; generation of the Voronoi Regions and generation of the final WFC output.

<br>

1. Voronoi Region generation:
- 'Generate Voronoi Diagram' generates a new randomized Voronoi diagram.
- 'Perform Lloyd Relaxatiopn' will perform the Lloyd's Relaxation algorithm to smoothen an existing Voronoi diagram.
- 'Perform Biome Correction' will change the regions such that each adjacent region is following the adjacency ruleset, so that the WFC algorithm can then operate on the Voronoi diagram.

<br>

2. WFC generation:
- 'Generate WFC Output' will start the process of generating the final, textured terrain. This requires a Voronoi diagram that has been corrected by the biome correction button.
- The algorithm will also perform biome spread during this process if it is enabled.
- The WFC algorithm may fail during generation due to a contradiction: It will simply restart if a contradiction is reached.
- The 'Show/Hide WFC Output' will show/hide the WFC output, revealing/hiding the Voronoi diagram behind it.

<br>

### The Variables Modification Window
This window is accessed using the 'Modify Input Variables' button. This enables easy modification of almost all the variables used by the Voronoi diagram and WFC algorithm for maximum customisation!

From this window, you can modify:

- The output size,
- The number of Voronoi regions,
- The collapsed neighbour threshold (Which influences the potency of biome spread as the value is lowered),
- The number of Lloyd's Relaxation iterations performed with one click of the 'Perform Lloyd Relaxation' button,
- The biome distribution per biome on generating and modifying the Voronoi diagram,
- The biome spread randomness value per biome, and
- The number of WFC generation iterations per frame (A larger value = More tiles drawn per frame = faster generation!).

Once finished, click the 'Return to Generation' button to save all the changes and return to the output window.

<br>

### Generation Tips
- The Lloyd's Relaxation button can be used after performing biome correction to perform slight modifications to a high fidelity Voronoi diagram. Ensure that you perform biome correction before running the WFC algorithm, otherwise a contradiction is likely to occur.
- A collapsed neighbour threshold of 2 usually results in fractured terrain, where sizable portions of biomes may be converted into other tiles and result in some tiles in a biome being cut off from tiles of the same type in that biome.
- A collapsed neighbour threshold of 3 and 4 will have a weak visual effect on the terrain even with a high biome spread value, but can add additional padding to biomes, useful when the terrain is already high quality.
- A collapsed neighbour threshold of 1 is chaotic and can easily result in contradictions. Low biome spread values should be used, but can result in biomes visually stretching out into neighbouring biomes, and can even result in nearby biomes of the same type joining together! Set a high 'WFC Generation Iterations' value to more quickly get a valid generation!
