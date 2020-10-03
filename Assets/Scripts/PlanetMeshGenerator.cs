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
using UnityEngine;
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
    
    // Internally, the Planet object stores its meshes as a child GameObjects:
    private GameObject _groundMesh;
    private GameObject _oceanMesh;

    // The subdivided icosahedron that we use to generate our planet is represented as a list
    // of Polygons, and a list of Vertices for those Polygons:
    private List<Polygon> _polygons;
    private List<Vector3> _vertices;

    public void SaveAsPrefab()
    {
        AssetDatabase.CreateAsset(_groundMesh.GetComponent<MeshFilter>().mesh, "Assets/Prefabs/groundmesh.asset");
        AssetDatabase.CreateAsset(_oceanMesh.GetComponent<MeshFilter>().mesh, "Assets/Prefabs/oceanmesh.asset");
        AssetDatabase.SaveAssets();
        PrefabUtility.SaveAsPrefabAsset(gameObject, "Assets/Prefabs/PlanetThing1.prefab");
    }
    public void Start()
    {
        // Create an icosahedron, subdivide it three times so that we have plenty of polys
        // to work with.

        InitAsIcosohedron();
        Subdivide(numberOfSubdivides);

        // When we begin extruding polygons, we'll need each one to know who its immediate
        //neighbors are. Calculate that now.

        CalculateNeighbors();

        foreach (Polygon p in _polygons)
        {
            p.m_Color = colorOcean;
        }

        // Now we build a set of Polygons that will become the land. We do this by generating
        // randomly sized spheres on the surface of the planet, and adding any Polygon that falls
        // inside that sphere.

        PolySet landPolys = CreateLand();

        // While we're here, let's make a group of oceanPolys. It's pretty simple: Any Polygon that isn't in the landPolys set
        // must be in the oceanPolys set instead.

        PolySet oceanPolys = CreateOcean(landPolys);

        // Let's create the ocean surface as a separate mesh.
        // First, let's make a copy of the oceanPolys so we can
        // still use them to also make the ocean floor later.

        var oceanSurface = new PolySet(oceanPolys);

        PolySet sides = Inset(oceanSurface, 0.05f);
        sides.ApplyColor(colorOcean);
        sides.ApplyAmbientOcclusionTerm(1.0f, 0.0f);

        if (_oceanMesh != null)
        {
            Destroy(_oceanMesh);
        }

        _oceanMesh = GenerateMesh("Ocean Surface", oceanMaterial);

        // Time to return to the oceans.
        
        if (generateDeepOceans)
        {
            var deepOceanPolys = oceanPolys.RemoveEdges();

            sides = Extrude(deepOceanPolys, -0.05f);
            sides.ApplyColor(colorDeepOcean);

            deepOceanPolys.ApplyColor(colorDeepOcean);
        }
        
        // Back to land for a while! We start by making it green. =)

        landPolys.ApplyColor(colorLand);

        // The Extrude function will raise the land Polygons up out of the water.
        // It also generates a strip of new Polygons to connect the newly raised land
        // back down to the water level. We can color this vertical strip of land brown like dirt.

        sides = Extrude(landPolys, 0.05f);

        sides.ApplyColor(colorSides);

        sides.ApplyAmbientOcclusionTerm(1.0f, 0.0f);

        if (generateHills)
        {
            // Grab additional polygons to generate hills, but only from the set of polygons that are land.
        
            PolySet hillPolys = landPolys.RemoveEdges();

            sides = Inset(hillPolys, 0.03f);
            sides.ApplyColor(colorLand);
            sides.ApplyAmbientOcclusionTerm(0.0f, 1.0f);

            sides = Extrude(hillPolys, 0.05f);
            sides.ApplyColor(colorSides);

            //Hills have dark ambient occlusion on the bottom, and light on top.
            sides.ApplyAmbientOcclusionTerm(1.0f, 0.0f);
        }

        // Okay, we're done! Let's generate an actual game mesh for this planet.

        if (_groundMesh != null)
        {
            Destroy(_groundMesh);
        }

        _groundMesh = GenerateMesh("Ground Mesh", groundMaterial);
    }

    private PolySet CreateOcean(PolySet landPolys)
    {
        var oceanPolys = new PolySet();

        foreach (Polygon poly in _polygons.Where(poly => !landPolys.Contains(poly)))
        {
            oceanPolys.Add(poly);
        }

        return oceanPolys;
    }

    private PolySet CreateLand()
    {
        var landPolys = new PolySet();

        // Grab polygons that are inside random spheres. These will be the basis of our planet's continents.

        for (int i = 0; i < numberOfContinents; i++)
        {
            float continentSize = Random.Range(continentSizeMin, continentSizeMax);

            PolySet newLand = GetPolysInSphere(Random.onUnitSphere, continentSize, _polygons);

            landPolys.UnionWith(newLand);
        }

        return landPolys;
    }

    private void InitAsIcosohedron()
    {
        _polygons = new List<Polygon>();
        _vertices = new List<Vector3>();

        // An icosahedron has 12 vertices, and
        // since they're completely symmetrical the
        // formula for calculating them is kind of
        // symmetrical too:

        float t = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;

        _vertices.Add(new Vector3(-1, t, 0).normalized);
        _vertices.Add(new Vector3(1, t, 0).normalized);
        _vertices.Add(new Vector3(-1, -t, 0).normalized);
        _vertices.Add(new Vector3(1, -t, 0).normalized);
        _vertices.Add(new Vector3(0, -1, t).normalized);
        _vertices.Add(new Vector3(0, 1, t).normalized);
        _vertices.Add(new Vector3(0, -1, -t).normalized);
        _vertices.Add(new Vector3(0, 1, -t).normalized);
        _vertices.Add(new Vector3(t, 0, -1).normalized);
        _vertices.Add(new Vector3(t, 0, 1).normalized);
        _vertices.Add(new Vector3(-t, 0, -1).normalized);
        _vertices.Add(new Vector3(-t, 0, 1).normalized);

        // And here's the formula for the 20 sides,
        // referencing the 12 vertices we just created.

        _polygons.Add(new Polygon(0, 11, 5));
        _polygons.Add(new Polygon(0, 5, 1));
        _polygons.Add(new Polygon(0, 1, 7));
        _polygons.Add(new Polygon(0, 7, 10));
        _polygons.Add(new Polygon(0, 10, 11));
        _polygons.Add(new Polygon(1, 5, 9));
        _polygons.Add(new Polygon(5, 11, 4));
        _polygons.Add(new Polygon(11, 10, 2));
        _polygons.Add(new Polygon(10, 7, 6));
        _polygons.Add(new Polygon(7, 1, 8));
        _polygons.Add(new Polygon(3, 9, 4));
        _polygons.Add(new Polygon(3, 4, 2));
        _polygons.Add(new Polygon(3, 2, 6));
        _polygons.Add(new Polygon(3, 6, 8));
        _polygons.Add(new Polygon(3, 8, 9));
        _polygons.Add(new Polygon(4, 9, 5));
        _polygons.Add(new Polygon(2, 4, 11));
        _polygons.Add(new Polygon(6, 2, 10));
        _polygons.Add(new Polygon(8, 6, 7));
        _polygons.Add(new Polygon(9, 8, 1));
    }

    private void Subdivide(int recursions)
    {
        var midPointCache = new Dictionary<int, int>();

        for (int i = 0; i < recursions; i++)
        {
            var newPolys = new List<Polygon>();
            foreach (var poly in _polygons)
            {
                int a = poly.m_Vertices[0];
                int b = poly.m_Vertices[1];
                int c = poly.m_Vertices[2];

                // Use GetMidPointIndex to either create a
                // new vertex between two old vertices, or
                // find the one that was already created.

                int ab = GetMidPointIndex(midPointCache, a, b);
                int bc = GetMidPointIndex(midPointCache, b, c);
                int ca = GetMidPointIndex(midPointCache, c, a);

                // Create the four new polygons using our original
                // three vertices, and the three new midpoints.
                newPolys.Add(new Polygon(a, ab, ca));
                newPolys.Add(new Polygon(b, bc, ab));
                newPolys.Add(new Polygon(c, ca, bc));
                newPolys.Add(new Polygon(ab, bc, ca));
            }

            // Replace all our old polygons with the new set of
            // subdivided ones.
            _polygons = newPolys;
        }
    }

    private int GetMidPointIndex(Dictionary<int, int> cache, int indexA, int indexB)
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

        Vector3 p1 = _vertices[indexA];
        Vector3 p2 = _vertices[indexB];
        Vector3 middle = Vector3.Lerp(p1, p2, 0.5f).normalized;

        ret = _vertices.Count;
        _vertices.Add(middle);

        // Add our new midpoint to the cache so we don't have
        // to do this again. =)

        cache.Add(key, ret);
        return ret;
    }

    private void CalculateNeighbors()
    {
        foreach (Polygon poly in _polygons)
        {
            foreach (Polygon otherPoly in _polygons.Where(otherPoly => poly != otherPoly && poly.IsNeighborOf(otherPoly)))
            {
                poly.m_Neighbors.Add(otherPoly);
            }
        }
    }

    private List<int> CloneVertices(List<int> oldVerts)
    {
        var newVerts = new List<int>();
        foreach (int oldVert in oldVerts)
        {
            Vector3 clonedVert = _vertices[oldVert];
            newVerts.Add(_vertices.Count);
            _vertices.Add(clonedVert);
        }

        return newVerts;
    }

    private PolySet StitchPolys(PolySet polys, out EdgeSet stitchedEdge)
    {
        var stichedPolys = new PolySet { m_StitchedVertexThreshold = _vertices.Count };

        stitchedEdge = polys.CreateEdgeSet();
        var originalVerts = stitchedEdge.GetUniqueVertices();
        var newVerts = CloneVertices(originalVerts);

        stitchedEdge.Split(originalVerts, newVerts);

        foreach (Edge edge in stitchedEdge)
        {
            // Create new polys along the stitched edge. These
            // will connect the original poly to its former
            // neighbor.

            var stitchPoly1 = new Polygon(edge.m_OuterVerts[0],
                edge.m_OuterVerts[1],
                edge.m_InnerVerts[0]);
            var stitchPoly2 = new Polygon(edge.m_OuterVerts[1],
                edge.m_InnerVerts[1],
                edge.m_InnerVerts[0]);
            // Add the new stitched faces as neighbors to
            // the original Polys.
            edge.m_InnerPoly.ReplaceNeighbor(edge.m_OuterPoly, stitchPoly2);
            edge.m_OuterPoly.ReplaceNeighbor(edge.m_InnerPoly, stitchPoly1);

            _polygons.Add(stitchPoly1);
            _polygons.Add(stitchPoly2);

            stichedPolys.Add(stitchPoly1);
            stichedPolys.Add(stitchPoly2);
        }

        //Swap to the new vertices on the inner polys.
        foreach (Polygon poly in polys)
        {
            for (int i = 0; i < 3; i++)
            {
                int vertID = poly.m_Vertices[i];
                if (!originalVerts.Contains(vertID))
                {
                    continue;
                }

                int vertIndex = originalVerts.IndexOf(vertID);
                poly.m_Vertices[i] = newVerts[vertIndex];
            }
        }

        return stichedPolys;
    }

    private PolySet Extrude(PolySet polys, float height)
    {
        PolySet stitchedPolys = StitchPolys(polys, out EdgeSet _);
        List<int> verts = polys.GetUniqueVertices();

        // Take each vertex in this list of polys, and push it
        // away from the center of the Planet by the height
        // parameter.

        foreach (int vert in verts)
        {
            Vector3 v = _vertices[vert];
            v = v.normalized * (v.magnitude + height);
            _vertices[vert] = v;
        }

        return stitchedPolys;
    }

    private PolySet Inset(PolySet polys, float insetDistance)
    {
        PolySet stitchedPolys = StitchPolys(polys, out EdgeSet stitchedEdge);

        Dictionary<int, Vector3> inwardDirections = stitchedEdge.GetInwardDirections(_vertices);

        // Push each vertex inwards, then correct
        // it's height so that it's as far from the center of
        // the planet as it was before.

        foreach (KeyValuePair<int, Vector3> kvp in inwardDirections)
        {
            int vertIndex = kvp.Key;
            Vector3 inwardDirection = kvp.Value;

            Vector3 vertex = _vertices[vertIndex];
            float originalHeight = vertex.magnitude;

            vertex += inwardDirection * insetDistance;
            vertex = vertex.normalized * originalHeight;
            _vertices[vertIndex] = vertex;
        }

        return stitchedPolys;
    }

    private PolySet GetPolysInSphere(Vector3 center, float radius, IEnumerable<Polygon> source)
    {
        var newSet = new PolySet();

        foreach (Polygon p in source)
        {
            if (p.m_Vertices.Select(vertexIndex => Vector3.Distance(center, _vertices[vertexIndex])).Any(distanceToSphere => distanceToSphere <= radius))
            {
                newSet.Add(p);
            }
        }

        return newSet;
    }

    private GameObject GenerateMesh(string gameObjectName, Material material)
    {
        var meshObject = new GameObject(gameObjectName);
        meshObject.transform.parent = transform;
        meshObject.transform.localPosition = Vector3.zero;

        var surfaceRenderer = meshObject.AddComponent<MeshRenderer>();
        surfaceRenderer.material = material;

        const int polygonVertexCount = Polygon.VertexCount;
        int vertexCount = _polygons.Count * polygonVertexCount;

        var indices = new int[vertexCount];

        var vertices = new Vector3[vertexCount];
        var normals = new Vector3[vertexCount];
        var colors = new Color32[vertexCount];
        var uvs = new Vector2[vertexCount];

        for (int i = 0; i < _polygons.Count; i++)
        {
            var poly = _polygons[i];

            int index = i * polygonVertexCount;
            SetIndexMeshValues(index, 0, poly, indices, vertices, normals, colors, uvs);
            SetIndexMeshValues(index, 1, poly, indices, vertices, normals, colors, uvs);
            SetIndexMeshValues(index, 2, poly, indices, vertices, normals, colors, uvs);
        }

        var terrainMesh = new Mesh { vertices = vertices, normals = normals, colors32 = colors, uv = uvs };
        terrainMesh.SetTriangles(indices, 0);

        var terrainFilter = meshObject.AddComponent<MeshFilter>();
        terrainFilter.mesh = terrainMesh;

        return meshObject;
    }

    private void SetIndexMeshValues(int index, int offset, Polygon poly, int[] indices, Vector3[] vertices, Vector3[] normals, Color32[] colors, Vector2[] uvs)
    {
        int index0 = index + offset;
        indices[index0] = index0;
        vertices[index0] = _vertices[poly.m_Vertices[offset]];
        normals[index0] = vertices[index0].normalized;
        colors[index0] = poly.m_Color;
        uvs[index0] = poly.m_UVs[offset];
    }
}