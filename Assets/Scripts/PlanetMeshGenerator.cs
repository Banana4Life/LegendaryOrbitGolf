/*
MIT License

Copyright (c) 2018 Pouchmouse

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Random = UnityEngine.Random;

[CustomEditor(typeof(PlanetMeshGenerator))]
class PlanetMeshGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Save Meshes"))
        {
            ((PlanetMeshGenerator) target).SaveAsPrefab();
        }
    }
}

// The Planet class is responsible for generating a tiny procedural planet. It does this by subdividing an Icosahedron, then
// randomly selecting groups of Polygons to extrude outwards. These become the lowlands and hills of the planet, while the
// unextruded Polygons become the ocean.

public class PlanetMeshGenerator : MonoBehaviour
{
    // These public parameters can be tweaked to give different styles to your planet.

    public Material groundMaterial;
    public Material oceanMaterial;

    public int numberOfSubdivides = 3;
    public int numberOfContinents = 5;
    public float continentSizeMax = 1.0f;
    public float continentSizeMin = 0.1f;
    public bool generateHills = true;
    public bool generateDeepOceans = true;
    public Color32 colorLand;
    public Color32 colorSides;
    public Color32 colorOcean;
    public Color32 colorDeepOcean;
    public string prefabName;

    public void SaveAsPrefab() 
    {
        var vertices = InitVertices();
        var polygons = Subdivide(InitPolygons(), vertices, numberOfSubdivides);
        CalculateNeighbors(polygons);

        Mesh oceanMesh = GetOceanMesh(polygons, vertices, colorOcean);
        Mesh groundMesh = GetGroundMesh(polygons, vertices, continentSizeMin, continentSizeMax, numberOfContinents, colorLand, colorSides, colorOcean, colorDeepOcean, generateHills, generateDeepOceans);

        const string prefabsPath = "Assets/Prefabs";
        var worldPath = $"{prefabsPath}/Worlds";
        if (!AssetDatabase.IsValidFolder(worldPath))
        {
            var guid = AssetDatabase.CreateFolder(prefabsPath, "Worlds");
        }
        
        var meshPath = $"{worldPath}/Meshs";
        if (!AssetDatabase.IsValidFolder(meshPath))
        {
            var guid = AssetDatabase.CreateFolder(worldPath, "Meshs");
        }

        AssetDatabase.CreateAsset(groundMesh, $"{meshPath}/{prefabName}_{groundMesh.name}.mesh");
        AssetDatabase.CreateAsset(oceanMesh, $"{meshPath}/{prefabName}_{oceanMesh.name}.mesh");
        AssetDatabase.SaveAssets();
        
        var prefabPath = $"{worldPath}/{prefabName}.prefab";
        var go = AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject)) as GameObject; 
        if (go == null || EditorUtility.DisplayDialog($"Update '{prefabName}.prefab'?", "Prefab exists, update?", "Yes", "No")) 
        {
            go = new GameObject(prefabName);
            CreateGameObject(go, oceanMesh, oceanMaterial);
            CreateGameObject(go, groundMesh, groundMaterial);
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            DestroyImmediate(go);
        }
    }

    private static IList<Vector3> InitVertices()
    {
        // An icosahedron has 12 vertices, and
        // since they're completely symmetrical the
        // formula for calculating them is kind of
        // symmetrical too:
        float t = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;

        return new List<Vector3>()
        {
            new Vector3(-1, t, 0).normalized,
            new Vector3(1, t, 0).normalized,
            new Vector3(-1, -t, 0).normalized,
            new Vector3(1, -t, 0).normalized,
            new Vector3(0, -1, t).normalized,
            new Vector3(0, 1, t).normalized,
            new Vector3(0, -1, -t).normalized,
            new Vector3(0, 1, -t).normalized,
            new Vector3(t, 0, -1).normalized,
            new Vector3(t, 0, 1).normalized,
            new Vector3(-t, 0, -1).normalized,
            new Vector3(-t, 0, 1).normalized
        };
    }

    private static IList<Polygon> InitPolygons()
    {
        // And here's the formula for the 20 sides,
        // referencing the 12 vertices we just created.

        return new List<Polygon>
        {
            new Polygon(0, 11, 5),
            new Polygon(0, 5, 1),
            new Polygon(0, 1, 7),
            new Polygon(0, 7, 10),
            new Polygon(0, 10, 11),
            new Polygon(1, 5, 9),
            new Polygon(5, 11, 4),
            new Polygon(11, 10, 2),
            new Polygon(10, 7, 6),
            new Polygon(7, 1, 8),
            new Polygon(3, 9, 4),
            new Polygon(3, 4, 2),
            new Polygon(3, 2, 6),
            new Polygon(3, 6, 8),
            new Polygon(3, 8, 9),
            new Polygon(4, 9, 5),
            new Polygon(2, 4, 11),
            new Polygon(6, 2, 10),
            new Polygon(8, 6, 7),
            new Polygon(9, 8, 1)
        };
    }

    private static void CreateGameObject(GameObject parent, Mesh mesh, Material material)
    {
        var gameObject = new GameObject(mesh.name);
        gameObject.transform.parent = parent.transform;
        gameObject.transform.localPosition = Vector3.zero;
        
        var surfaceRenderer = gameObject.AddComponent<MeshRenderer>();
        surfaceRenderer.material = material;   
        
        var terrainFilter = gameObject.AddComponent<MeshFilter>();
        terrainFilter.mesh = mesh;
    }

    private static Mesh GetGroundMesh(IList<Polygon> polygons, IList<Vector3> vertices, float continentSizeMin, float continentSizeMax, int numberOfContinents, Color32 landColor, Color32 sideColor,
        Color32 oceanColor, Color32 deepOceanColor, bool isGenerateHills, bool isGenerateDeepOceans)
    {
        // Now we build a set of Polygons that will become the land. We do this by generating
        // randomly sized spheres on the surface of the planet, and adding any Polygon that falls
        // inside that sphere.

        PolySet landPolys = CreateLand(polygons, vertices, continentSizeMin, continentSizeMax, numberOfContinents);

        // While we're here, let's make a group of oceanPolys. It's pretty simple: Any Polygon that isn't in the landPolys set
        // must be in the oceanPolys set instead.

        PolySet oceanPolys = CreateOcean(polygons, landPolys);

        // Let's create the ocean surface as a separate mesh.
        // First, let's make a copy of the oceanPolys so we can
        // still use them to also make the ocean floor later.

        var oceanSurface = new PolySet(oceanPolys);

        PolySet sides = Inset(polygons, vertices, oceanSurface, 0.05f);
        PolySet.ApplyColor(sides, oceanColor);
        sides.ApplyAmbientOcclusionTerm(1.0f, 0.0f);

        // Time to return to the oceans.

        sides = Extrude(polygons, vertices, oceanPolys, -0.02f);
        PolySet.ApplyColor(sides, oceanColor);
        sides.ApplyAmbientOcclusionTerm(0.0f, 1.0f);

        sides = Inset(polygons, vertices, oceanPolys, 0.02f);
        PolySet.ApplyColor(sides, oceanColor);
        sides.ApplyAmbientOcclusionTerm(1.0f, 0.0f);
        
        if (isGenerateDeepOceans)
        {
            var deepOceanPolys = PolySet.RemoveEdges(oceanPolys);

            sides = Extrude(polygons, vertices, deepOceanPolys, -0.05f);
            PolySet.ApplyColor(sides, deepOceanColor);

            PolySet.ApplyColor(deepOceanPolys, deepOceanColor);
        }

        // Back to land for a while! We start by making it green. =)

        PolySet.ApplyColor(landPolys, landColor);

        // The Extrude function will raise the land Polygons up out of the water.
        // It also generates a strip of new Polygons to connect the newly raised land
        // back down to the water level. We can color this vertical strip of land brown like dirt.

        sides = Extrude(polygons, vertices, landPolys, 0.05f);
        PolySet.ApplyColor(sides, sideColor);
        sides.ApplyAmbientOcclusionTerm(1.0f, 0.0f);

        if (isGenerateHills)
        {
            GenerateHills(polygons, vertices, landPolys, landColor, sideColor);
        }

        return GenerateMesh("Ground Mesh", polygons, vertices);
    }

    private static Mesh GetOceanMesh(IList<Polygon> polygons, IList<Vector3> vertices, Color32 color)
    {
        foreach (Polygon p in polygons)
        {
            p.Color = color;
        }

        return GenerateMesh("Ocean Surface", polygons, vertices);
    }

    private static PolySet CreateLand(IList<Polygon> polygons, IList<Vector3> vertices, float sizeMin, float sizeMax, int ofContinents)
    {
        var landPolys = new PolySet();

        // Grab polygons that are inside random spheres. These will be the basis of our planet's continents.

        for (int i = 0; i < ofContinents; i++)
        {
            float continentSize = Random.Range(sizeMin, sizeMax);

            PolySet newLand = GetPolysInSphere(vertices, Random.onUnitSphere, continentSize, polygons);

            landPolys.UnionWith(newLand);
        }

        return landPolys;
    }

    private static PolySet CreateOcean(IEnumerable<Polygon> polygons, PolySet landPolys)
    {
        var oceanPolys = new PolySet();

        foreach (Polygon poly in polygons.Where(poly => !landPolys.Contains(poly)))
        {
            oceanPolys.Add(poly);
        }

        return oceanPolys;
    }

    private static IList<Polygon> Subdivide(IList<Polygon> polygons, IList<Vector3> vertices, int recursions)
    {
        var midPointCache = new Dictionary<int, int>();

        for (int i = 0; i < recursions; i++)
        {
            var newPolys = new List<Polygon>();
            foreach (var poly in polygons)
            {
                int a = poly.Vertices[0];
                int b = poly.Vertices[1];
                int c = poly.Vertices[2];

                // Use GetMidPointIndex to either create a
                // new vertex between two old vertices, or
                // find the one that was already created.

                int ab = GetMidPointIndex(vertices, midPointCache, a, b);
                int bc = GetMidPointIndex(vertices, midPointCache, b, c);
                int ca = GetMidPointIndex(vertices, midPointCache, c, a);

                // Create the four new polygons using our original
                // three vertices, and the three new midpoints.
                newPolys.Add(new Polygon(a, ab, ca));
                newPolys.Add(new Polygon(b, bc, ab));
                newPolys.Add(new Polygon(c, ca, bc));
                newPolys.Add(new Polygon(ab, bc, ca));
            }

            // Replace all our old polygons with the new set of
            // subdivided ones.

            polygons = newPolys;
        }

        return polygons;
    }

    private static void GenerateHills(ICollection<Polygon> polygons, IList<Vector3> vertices, PolySet landPolys, Color32 landColor, Color32 sideColor)
    {
        // Grab additional polygons to generate hills, but only from the set of polygons that are land.

        PolySet hillPolys = PolySet.RemoveEdges(landPolys);

        PolySet insetSides = Inset(polygons, vertices, hillPolys, 0.03f);
        PolySet.ApplyColor(insetSides, landColor);
        insetSides.ApplyAmbientOcclusionTerm(0.0f, 1.0f);

        PolySet extrudeSides = Extrude(polygons, vertices, hillPolys, 0.05f);
        PolySet.ApplyColor(extrudeSides, sideColor);

        //Hills have dark ambient occlusion on the bottom, and light on top.
        extrudeSides.ApplyAmbientOcclusionTerm(1.0f, 0.0f);
    }

    private static int GetMidPointIndex(IList<Vector3> vertices, IDictionary<int, int> cache, int indexA, int indexB)
    {
        // We create a key out of the two original indices
        // by storing the smaller index in the upper two bytes
        // of an integer, and the larger index in the lower two
        // bytes. By sorting them according to whichever is smaller
        // we ensure that this function returns the same result
        // whether you call
        // GetMidPointIndex(cache, 5, 9)
        // or...
        // GetMidPointIndex(cache, 9, 5)

        int smallerIndex = Mathf.Min(indexA, indexB);
        int greaterIndex = Mathf.Max(indexA, indexB);
        int key = (smallerIndex << 16) + greaterIndex;

        // If a midpoint is already defined, just return it.

        if (cache.TryGetValue(key, out int ret))
        {
            return ret;
        }

        // If we're here, it's because a midpoint for these two
        // vertices hasn't been created yet. Let's do that now!

        Vector3 p1 = vertices[indexA];
        Vector3 p2 = vertices[indexB];
        Vector3 middle = Vector3.Lerp(p1, p2, 0.5f).normalized;

        ret = vertices.Count;
        vertices.Add(middle);

        // Add our new midpoint to the cache so we don't have
        // to do this again. =)

        cache.Add(key, ret);
        return ret;
    }

    private static void CalculateNeighbors(IList<Polygon> polygons)
    {
        foreach (Polygon poly in polygons)
        {
            foreach (Polygon otherPoly in polygons.Where(otherPoly => poly != otherPoly && Polygon.IsNeighborOf(poly.Vertices, otherPoly)))
            {
                poly.Neighbors.Add(otherPoly);
            }
        }
    }

    private static IList<int> CloneVertices(IList<Vector3> vertices, IEnumerable<int> oldVerts)
    {
        var newVerts = new List<int>();
        foreach (int oldVert in oldVerts)
        {
            Vector3 clonedVert = vertices[oldVert];
            newVerts.Add(vertices.Count);
            vertices.Add(clonedVert);
        }

        return newVerts;
    }

    private static PolySet StitchPolys(ICollection<Polygon> polygons, IList<Vector3> vertices, PolySet polys, out EdgeSet stitchedEdge)
    {
        var stichedPolys = new PolySet { StitchedVertexThreshold = vertices.Count };

        stitchedEdge = PolySet.CreateEdgeSet(polys);
        IList<int> originalVerts = EdgeSet.GetUniqueVertices(stitchedEdge);
        IList<int> newVerts = CloneVertices(vertices, originalVerts);

        stitchedEdge.Split(originalVerts, newVerts);

        foreach (Edge edge in stitchedEdge)
        {
            // Create new polys along the stitched edge. These
            // will connect the original poly to its former
            // neighbor.

            var stitchPoly1 = new Polygon(edge.OuterVerts[0],
                edge.OuterVerts[1],
                edge.InnerVerts[0]);
            var stitchPoly2 = new Polygon(edge.OuterVerts[1],
                edge.InnerVerts[1],
                edge.InnerVerts[0]);
            // Add the new stitched faces as neighbors to
            // the original Polys.
            Polygon.ReplacePolygon(edge.InnerPoly.Neighbors, edge.OuterPoly, stitchPoly2);
            Polygon.ReplacePolygon(edge.OuterPoly.Neighbors, edge.InnerPoly, stitchPoly1);

            polygons.Add(stitchPoly1);
            polygons.Add(stitchPoly2);

            stichedPolys.Add(stitchPoly1);
            stichedPolys.Add(stitchPoly2);
        }

        //Swap to the new vertices on the inner polys.
        foreach (Polygon poly in polys)
        {
            for (int i = 0; i < 3; i++)
            {
                int vertID = poly.Vertices[i];
                if (!originalVerts.Contains(vertID))
                {
                    continue;
                }

                int vertIndex = originalVerts.IndexOf(vertID);
                poly.Vertices[i] = newVerts[vertIndex];
            }
        }

        return stichedPolys;
    }

    private static PolySet Extrude(ICollection<Polygon> polygons, IList<Vector3> vertices, PolySet polys, float height)
    {
        PolySet stitchedPolys = StitchPolys(polygons, vertices, polys, out EdgeSet _);
        IList<int> verts = PolySet.GetUniqueVertices(polys);

        // Take each vertex in this list of polys, and push it
        // away from the center of the Planet by the height
        // parameter.

        foreach (int vert in verts)
        {
            Vector3 v = vertices[vert];
            v = v.normalized * (v.magnitude + height);
            vertices[vert] = v;
        }

        return stitchedPolys;
    }

    private static PolySet Inset(ICollection<Polygon> polygons, IList<Vector3> vertices, PolySet polys, float insetDistance)
    {
        PolySet stitchedPolys = StitchPolys(polygons, vertices, polys, out EdgeSet stitchedEdge);

        Dictionary<int, Vector3> inwardDirections = EdgeSet.GetInwardDirections(stitchedEdge, vertices);

        // Push each vertex inwards, then correct
        // it's height so that it's as far from the center of
        // the planet as it was before.

        foreach (KeyValuePair<int, Vector3> kvp in inwardDirections)
        {
            int vertIndex = kvp.Key;
            Vector3 inwardDirection = kvp.Value;

            Vector3 vertex = vertices[vertIndex];
            float originalHeight = vertex.magnitude;

            vertex += inwardDirection * insetDistance;
            vertex = vertex.normalized * originalHeight;
            vertices[vertIndex] = vertex;
        }

        return stitchedPolys;
    }

    private static PolySet GetPolysInSphere(IList<Vector3> vertices, Vector3 center, float radius, IEnumerable<Polygon> source)
    {
        var newSet = new PolySet();

        foreach (Polygon p in source)
        {
            if (p.Vertices.Select(vertexIndex => Vector3.Distance(center, vertices[vertexIndex])).Any(distanceToSphere => distanceToSphere <= radius))
            {
                newSet.Add(p);
            }
        }

        return newSet;
    }

    private static Mesh GenerateMesh(string meshName, IList<Polygon> polygons, IList<Vector3> inVertices)
    {
        const int polygonVertexCount = Polygon.VertexCount;
        int vertexCount = polygons.Count * polygonVertexCount;

        var indices = new int[vertexCount];

        var vertices = new Vector3[vertexCount];
        var normals = new Vector3[vertexCount];
        var colors = new Color32[vertexCount];
        var uvs = new Vector2[vertexCount];

        for (int i = 0; i < polygons.Count; i++)
        {
            var poly = polygons[i];

            int index = i * polygonVertexCount;
            SetIndexMeshValues(inVertices, index, 0, poly, indices, vertices, normals, colors, uvs);
            SetIndexMeshValues(inVertices, index, 1, poly, indices, vertices, normals, colors, uvs);
            SetIndexMeshValues(inVertices, index, 2, poly, indices, vertices, normals, colors, uvs);
        }

        var terrainMesh = new Mesh { vertices = vertices, normals = normals, colors32 = colors, uv = uvs };
        terrainMesh.SetTriangles(indices, 0);
        terrainMesh.name = meshName;

        return terrainMesh;
    }

    private static void SetIndexMeshValues(IList<Vector3> inVertices, int index, int offset, Polygon poly, IList<int> indices, IList<Vector3> vertices, IList<Vector3> normals, IList<Color32> colors, IList<Vector2> uvs)
    {
        int index0 = index + offset;
        indices[index0] = index0;
        vertices[index0] = inVertices[poly.Vertices[offset]];
        normals[index0] = vertices[index0].normalized;
        colors[index0] = poly.Color;
        uvs[index0] = poly.UVs[offset];
    }
}