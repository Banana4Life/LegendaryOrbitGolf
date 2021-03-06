﻿/*
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

public class Polygon
{
    public const int VertexCount = 3;
    public readonly List<int> Vertices; // Indices of the three vertices that make up this Polygon.
    public readonly List<Vector2> UVs; // The uv coordinates we want to apply at each vertex.
    public readonly List<Polygon> Neighbors; // Links to this Polygon's three neighbors.
    public Color32 Color; // What color do we want this poly to be?

    public Polygon(int a, int b, int c)
    {
        Vertices = new List<int> { a, b, c };
        Neighbors = new List<Polygon>();
        UVs = new List<Vector2> { Vector2.zero, Vector2.zero, Vector2.zero };

        // Hot Pink is an excellent default color because you'll notice instantly if 
        // you forget to set it to something else.
        Color = new Color32(255, 0, 255, 255);
    }

    // IsNeighborOf is a convenience function to calculate if two polys share an edge.
    // We usually just need to calculate this once, and then we can use the m_Neighbors list
    // that's stored in each Polygon.

    public static bool IsNeighborOf(IEnumerable<int> vertices, Polygon otherPoly)
    {
        int sharedVertices = vertices.Count(vertex => otherPoly.Vertices.Contains(vertex));

        // A polygon and its neighbor will share exactly
        // two vertices. Ergo, if this poly shares two
        // vertices with the other, then they are neighbors.

        return sharedVertices == 2;
    }

    // As we build the planet, we'll insert strips of Polygons between others.
    // This means we need to replace the old neighbors in their Neighbors list
    // with the new ones we are inserting. This simple function does that.

    public static void ReplacePolygon(IList<Polygon> polygons, Polygon oldNeighbor, Polygon newNeighbor)
    {
        for (int i = 0; i < polygons.Count; i++)
        {
            if (oldNeighbor == polygons[i])
            {
                polygons[i] = newNeighbor;
                return;
            }
        }
    }
}

// A PolySet is a set of unique Polygons. Basically it's a HashSet, but we also give it
// some handy convenience functions.

public class PolySet : HashSet<Polygon>
{
    public PolySet() { }
    public PolySet(PolySet source) : base(source) { }

    // If this PolySet was created by stitching existing Polys, then we store the index of the
    // last original vertex before we did the stitching. This way we can tell new vertices apart
    // from old ones.
    public int StitchedVertexThreshold = -1;

    //Given a set of Polys, calculate the set of Edges
    //that surround them.

    public static EdgeSet CreateEdgeSet(HashSet<Polygon> polygons)
    {
        var edgeSet = new EdgeSet();

        foreach (Polygon poly in polygons)
        {
            foreach (Polygon neighbor in poly.Neighbors)
            {
                if (polygons.Contains(neighbor))
                {
                    continue;
                }

                // If our neighbor isn't in our PolySet, then
                // the edge between us and our neighbor is one
                // of the edges of this PolySet.
                var edge = new Edge(poly, neighbor);
                edgeSet.Add(edge);
            }
        }

        return edgeSet;
    }

    // RemoveEdges - Remove any poly from this set that borders the edge of the set, including those that just
    // touch the edge with a single vertex. The PolySet could be empty after this operation.

    public static PolySet RemoveEdges(HashSet<Polygon> polygons)
    {
        var newSet = new PolySet();

        var edgeSet = CreateEdgeSet(polygons);

        var edgeVertices = EdgeSet.GetUniqueVertices(edgeSet);

        foreach (Polygon poly in polygons)
        {
            if (poly.Vertices.Any(vertices => edgeVertices.Contains(vertices)))
            {
                continue;
            }

            newSet.Add(poly);
        }

        return newSet;
    }

    // GetUniqueVertices calculates a list of the vertex indices used by these Polygons
    // with no duplicates.

    public static IList<int> GetUniqueVertices(HashSet<Polygon> polygons)
    {
        var verts = new List<int>();
        foreach (Polygon poly in polygons)
        {
            foreach (int vert in poly.Vertices)
            {
                if (!verts.Contains(vert))
                {
                    verts.Add(vert);
                }
            }
        }

        return verts;
    }

    // ApplyAmbientOcclusionTerms-
    // Ambient Occlusion data is stored in the UV coordinates of polygons. (That's fine, because we're not texturing them, and so the
    // uv coordinates can just be extra data for us. If you're planning on texturing your planet, you can move the AO data to a second
    // uv map.

    public void ApplyAmbientOcclusionTerm(float aoForOriginalVerts, float aoForNewVerts)
    {
        foreach (Polygon poly in this)
        {
            for (int i = 0; i < Polygon.VertexCount; i++)
            {
                float ambientOcclusionTerm = (poly.Vertices[i] > StitchedVertexThreshold) ? aoForNewVerts : aoForOriginalVerts;

                Vector2 uv = poly.UVs[i];
                uv.y = ambientOcclusionTerm;
                poly.UVs[i] = uv;
            }
        }
    }

    // Apply Color to all our Polys. This is a pretty trivial function, but it makes the code a little more readable.

    public static void ApplyColor(HashSet<Polygon> polygons, Color32 c)
    {
        foreach (Polygon poly in polygons)
        {
            poly.Color = c;
        }
    }
}