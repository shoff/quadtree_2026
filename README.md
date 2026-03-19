# Quadtree Terrain Engine

**A hierarchical quadtree-based 3D terrain rendering engine, resurrected from the XNA era and rebuilt for modern .NET.**

---

## The Story

Back in the mid-2000s, Microsoft released **XNA Game Studio** -- a framework that made game development accessible to C# developers for the first time. It was revolutionary. Hobbyists, students, and indie developers suddenly had a real toolchain for building games on Windows and Xbox 360.

One of the gems that emerged from that community was the **Quadtree Terrain Library** by **Clyde F. Coulter, Jr.**, hosted on the now-defunct CodePlex (Microsoft's open source hosting platform, shut down in 2017). Version 0.3.5 of this library demonstrated a clean, educational implementation of one of the most important spatial data structures in game development: the **quadtree**.

The library could take a simple grayscale heightmap image, generate a massive 3D terrain from it, and render only the parts the player could actually see -- all in real time, using hierarchical frustum culling. It supported placing hundreds of objects on the terrain, attaching AI-driven game components, simulating gravity, and visualizing the quadtree structure itself as a heads-up display.

Then Microsoft killed XNA. The last version (4.0) shipped in 2010. The framework was officially discontinued in 2013. XNA Game Studio required Visual Studio extensions that stopped working. The content pipeline broke. Projects stopped compiling. An entire ecosystem of educational game code was left to rot.

**This project brings it back.**

The original C#/XNA 1.0 code has been carefully ported to **MonoGame 3.8** targeting **.NET 9**, preserving the architecture, algorithms, and behavior of the original while modernizing every API call. It compiles. It runs. On macOS, Linux, and Windows.

---

## What Is a Quadtree (and Why Should You Care)?

A **quadtree** is a tree data structure where each internal node has exactly four children, representing the four quadrants of a 2D space (northwest, northeast, southwest, southeast). In terrain rendering, this creates a hierarchy:

```
                    [Root: Entire Terrain]
                   /      |      |       \
              [NW]      [NE]    [SW]     [SE]
             / | | \   / | | \
           ...      ...      ...
          [Leaf nodes: actual mesh sectors]
```

**Why this matters for games:**

- **Frustum Culling** -- The camera can only see a cone-shaped region of the world (the "view frustum"). A quadtree lets you quickly discard entire branches of terrain that are outside this cone. If the NW quadrant is completely behind the camera, you skip it *and all its children* in a single check. For a 512x512 heightmap with 524,288 triangles, this typically means rendering only 10-30 sectors instead of all of them.

- **Spatial Queries** -- "What is the terrain height at position (4676, 7194)?" Instead of searching the entire heightmap, the quadtree recursively narrows down to the exact sector containing that point in O(log n) time. This is essential for keeping players and objects on the ground.

- **Object Management** -- Static objects (rocks, buildings) and dynamic objects (AI tanks, NPCs) are stored in the quadtree node that contains their position. When a node isn't visible, its objects aren't drawn either. Move an object? It automatically migrates to the correct node.

This pattern is used extensively in production game engines, GIS systems, collision detection, and any application dealing with large 2D spatial datasets.

---

## Architecture

```
QuadtreeMonoGame/
|
+-- QuadtreeComponentLib/          Core terrain engine library
|   +-- Quadtree.cs                All engine classes (~750 lines)
|       +-- Quadtree               Main public API (DrawableGameComponent)
|       +-- QuadTreeNode           Recursive spatial subdivision
|       +-- QuadtreeGameComponent  Base class for attachable game objects
|       +-- QuadtreeModelInfo      Data container for static models
|       +-- SimpleMesh             Procedural mesh rendering
|       +-- ProceduralModels       Box and tank mesh generators
|       +-- NodeDrawn              Debug visualization data
|
+-- TankComponent/                 AI tank game component
|   +-- TankComponent.cs           Autonomous roaming tank (~100 lines)
|
+-- TestQuadtree1/                 Demo game application
|   +-- Game1.cs                   Main game loop (~350 lines)
|   +-- Program.cs                 Entry point
|   +-- Content/
|       +-- Hm2.jpg                512x512 grayscale heightmap
|       +-- LandscapeTex02.jpg     Terrain overlay texture
|       +-- Content.mgcb           Content pipeline manifest
|
+-- QuadtreeMonoGame.slnx          Solution file
```

### Class Relationships

```
Game1 (demo)
  |
  +-- creates --> Quadtree (DrawableGameComponent)
  |                 |
  |                 +-- builds --> QuadTreeNode (recursive tree)
  |                 |               |
  |                 |               +-- stores --> QuadtreeModelInfo[] (static rocks)
  |                 |               +-- stores --> QuadtreeGameComponent[] (tanks)
  |                 |               +-- owns   --> VertexBuffer, IndexBuffer (terrain mesh)
  |                 |
  |                 +-- owns --> BasicEffect (terrain shader: lighting, fog, texture)
  |
  +-- creates --> TankGameComponent[] (50 AI tanks)
                    |
                    +-- inherits --> QuadtreeGameComponent
                    +-- references --> SimpleMesh (procedural tank geometry)
```

---

## Features

### Terrain Rendering
- **Heightmap-based terrain generation** from any grayscale image (JPEG, PNG)
- **Per-vertex normal calculation** with 6-neighbor averaging for smooth lighting
- **Texture mapping** across the entire terrain surface
- **Configurable cell size** and **sector size** for performance tuning
- Supports terrains up to system memory limits

### Spatial Optimization
- **Hierarchical frustum culling** using bounding box projection
- Sectors rendered as independent vertex/index buffer pairs
- Parent nodes culled = all children skipped (massive savings)
- Typical visible set: **10-30 sectors** out of hundreds

### Object Placement
- **Static models** (`AddModel`) -- rocks, buildings, props placed once
- **Dynamic components** (`AddComponent`) -- AI entities that move and update
- Objects automatically positioned at terrain surface height
- Objects automatically migrate between quadtree nodes when moved
- **Visibility-driven rendering** -- objects in culled nodes are never drawn

### Physics & Interaction
- **Height queries** (`GetHeightAt`) -- bilinear interpolation within grid cells
- **Gravity simulation** with terminal velocity
- **First-person camera** with terrain-following look-at
- **Boundary clamping** to keep avatar on the terrain

### Visual Effects
- **Directional lighting** with specular highlights
- **Distance fog** (cornflower blue, configurable start/end)
- **Ambient light** with configurable color
- **Node visualization HUD** -- wireframe overlay showing which quadtree sectors are being rendered in real time

### Demo Scene
- **512x512 heightmap** terrain (~15,360 x 15,360 world units)
- **500 static rock objects** scattered across the landscape
- **50 autonomous AI tanks** roaming the terrain with simple kinematics
- **Interactive first-person navigation** with jump/jetpack

---

## Controls

| Key | Action |
|-----|--------|
| `W` / `Up Arrow` | Move forward |
| `S` / `Down Arrow` | Move backward |
| `Q` / `Left Arrow` | Rotate left |
| `E` / `Right Arrow` | Rotate right |
| `A` | Strafe left |
| `D` | Strafe right |
| `Space` | Jump / Jetpack |
| `Shift` | Speed boost (2x) |
| `N` | Toggle quadtree node visualization HUD |
| `Escape` | Exit |

Gamepad input (D-pad and back button) is also supported.

---

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (or later)
- That's it. No Visual Studio required. No content pipeline tools needed.

### Build & Run

```bash
git clone https://github.com/shoff/quadtree_2026.git
cd quadtree_2026
dotnet build
dotnet run --project TestQuadtree1
```

The game window will open at 640x480 with the terrain, rocks, tanks, and node visualization overlay.

### Platform Notes

| Platform | Status |
|----------|--------|
| macOS (Apple Silicon & Intel) | Tested, works |
| Linux (x64) | Supported via MonoGame DesktopGL |
| Windows (x64) | Supported via MonoGame DesktopGL |

---

## Using the Library in Your Own Project

### 1. Add a reference to `QuadtreeComponentLib`

```xml
<ProjectReference Include="../QuadtreeComponentLib/QuadtreeComponentLib.csproj" />
```

### 2. Create and initialize the quadtree

```csharp
using QuadtreeComponent;

// In Initialize():
quadtree = new Quadtree(this);
Components.Add(quadtree);

// In LoadContent():
Texture2D heightMap = /* load your heightmap */;
quadtree.Create(heightMap, cellSize: 30.0f, sectorSize: 16);
quadtree.Texture = /* load your terrain texture */;
```

### 3. Set view/projection each frame

```csharp
// In Draw():
quadtree.View = viewMatrix;
quadtree.Projection = projectionMatrix;
base.Draw(gameTime);  // Quadtree renders automatically as a GameComponent
```

### 4. Query terrain height

```csharp
float terrainHeight = quadtree.GetHeightAt(player.X, player.Z);
// Returns -987654321.0f if the position is off the terrain
```

### 5. Place static objects

```csharp
SimpleMesh rockMesh = ProceduralModels.CreateBox(GraphicsDevice, 1f, 1f, 1f);
rockMesh.CopyEffectFrom(quadtree.Effect);  // Match lighting/fog

quadtree.AddModel(rockMesh, position, scale, rotation);
```

### 6. Attach dynamic game components

```csharp
// Create a class that inherits QuadtreeGameComponent
public class Enemy : QuadtreeGameComponent
{
    public override void Update(GameTime gameTime) { /* AI logic */ }
    public override void Draw(GameTime gameTime, Matrix view, Matrix projection)
    {
        // Only called when this object is VISIBLE in the quadtree
    }
}

// Add it:
var enemy = new Enemy(this);
enemy.Position = startPosition;  // Y=0 means "place on terrain surface"
quadtree.AddComponent(enemy);
```

### 7. Configure visual effects

```csharp
quadtree.Effect.FogEnabled = true;
quadtree.Effect.FogColor = Color.CornflowerBlue.ToVector3();
quadtree.Effect.FogStart = 2500f;
quadtree.Effect.FogEnd = 4300f;
quadtree.AmbientLight = new Vector3(0.4f, 0.4f, 0.5f);
```

---

## Creating Your Own Heightmaps

A heightmap is simply a grayscale image where pixel brightness = terrain elevation:

- **Black (0)** = lowest point
- **White (255)** = highest point
- **Image dimensions** = terrain grid resolution (512x512 is a good starting point)

The engine extracts the blue channel of each pixel and multiplies by a scale factor (default 2.5), giving a height range of 0 to 637.5 world units for an 8-bit heightmap.

### Quick method (any image editor)

1. Create a new grayscale image (512x512)
2. Paint with soft brushes -- lighter = higher
3. Apply Gaussian blur for smooth hills
4. Add noise for surface roughness
5. Save as JPEG and drop it in the `Content/` folder

### Terrain texture

Create a color image (ideally 2-4x the heightmap resolution) that will be stretched across the entire terrain surface. Use the heightmap as a guide -- snow on peaks, grass in valleys, rock on slopes.

---

## How the Quadtree Works (Technical Deep Dive)

### Build Phase

Given a heightmap of dimensions W x H and a sector size S:

1. The root node spans the entire terrain
2. If the node is larger than S x S, it subdivides into 4 children
3. Recursion continues until each leaf node is at most S x S cells
4. Leaf nodes create GPU vertex/index buffers for their mesh sector
5. All nodes compute bounding boxes (8 corners with min/max height) for culling

For a 512x512 heightmap with sector size 16, this creates a tree approximately 5 levels deep with ~1,024 leaf nodes, each containing a 17x17 vertex mesh (289 vertices, 512 triangles).

### Frustum Culling (per frame)

```
For each node, starting at root:
  1. Transform 8 bounding box corners by ViewProjection matrix
  2. Check if ALL 8 corners fail any single frustum plane test:
     - All behind near plane? CULL (return 0)
     - All left of left plane? CULL
     - All right of right plane? CULL
     - All beyond far plane? CULL
  3. If no plane rejects all 8: partially visible (-1) or fully visible (1)
  4. If culled: skip this node AND all children (the big win)
  5. If fully visible: draw this node, skip culling children
  6. If partial: draw this node, continue checking children
```

### Height Interpolation

`GetHeightAt(x, z)` performs bilinear interpolation:

1. Navigate the tree to find the leaf node containing (x, z)
2. Determine which grid cell (mapX, mapY) the point falls in
3. Compute fractional position (deltaX, deltaY) within the cell
4. Determine which of the cell's 2 triangles contains the point
5. Interpolate height from the 3 triangle vertices

---

## What Changed from XNA to MonoGame

This isn't just a recompile. XNA 1.0 and MonoGame 3.8 are separated by nearly two decades of API evolution. Here are the significant changes:

| XNA 1.0 (2006) | MonoGame 3.8 (2024) | Why |
|---|---|---|
| `LoadGraphicsContent(bool loadAll)` | `LoadContent()` | Resource management modes removed |
| `BasicEffect(device, null)` | `BasicEffect(device)` | Effect pool parameter eliminated |
| `effect.Begin()` / `pass.Begin()` / `pass.End()` / `effect.End()` | `pass.Apply()` | Simplified effect pass model |
| `gd.Vertices[0].SetSource(buffer, offset, stride)` | `gd.SetVertexBuffer(buffer)` | Vertex stream API simplified |
| `gd.RenderState.CullMode = CullMode.X` | `gd.RasterizerState = new RasterizerState { CullMode = X }` | Immutable state objects |
| `new VertexBuffer(gd, sizeInBytes, usage, mode)` | `new VertexBuffer(gd, typeof(T), count, usage)` | Type-safe buffer creation |
| `new IndexBuffer(gd, size, usage, mode, elementSize)` | `new IndexBuffer(gd, elementSize, count, usage)` | Simplified constructor |
| `VertexDeclaration` required | Built into vertex types | No manual vertex format declaration |
| `ResourceManagementMode.Automatic` | Removed | GPU memory managed automatically |
| FBX models via XNA Content Pipeline | `SimpleMesh` procedural geometry | Original FBX files are pre-2011 format, incompatible with modern Assimp |
| Textures via Content Pipeline | `Texture2D.FromStream()` from raw files | No build-time content processing needed |
| `gameTime.ElapsedGameTime.Milliseconds` (int) | `gameTime.ElapsedGameTime.TotalSeconds` (double) | More precise timing |

---

## Project History

| Year | Event |
|------|-------|
| **2006** | Microsoft releases XNA Game Studio 1.0 |
| **2007** | Clyde F. Coulter, Jr. publishes Quadtree Library v0.1.1 on CodePlex |
| **2008** | Library evolves to v0.3.5 with AddComponent, AddModel, and TankGameComponent |
| **2010** | XNA 4.0 released (final version) |
| **2013** | Microsoft discontinues XNA |
| **2017** | CodePlex shuts down; original source becomes harder to find |
| **2026** | This port: MonoGame 3.8, .NET 9, cross-platform, fully modernized |

---

## Original Author

The quadtree terrain library was created by **Clyde F. Coulter, Jr.** and published under a non-commercial use license on CodePlex at `http://www.codeplex.com/quadtreeload`. The original code was written for XNA Game Studio v1.0 and demonstrated professional-quality spatial partitioning techniques in an accessible, well-commented C# codebase.

## License

The original library was released for non-commercial use. This port maintains that spirit -- use it to learn, experiment, build prototypes, and understand how spatial data structures work in real-time 3D rendering.

---

*Ported to MonoGame with the assistance of Claude (Anthropic).*
