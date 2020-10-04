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

// An Edge is a boundary between two Polygons. We're going to be working with loops of Edges, so
// each Edge will have a Polygon that's inside the loop and a Polygon that's outside the loop.
// We also want to Split apart the inner and outer Polygons so that they no longer share the same
// vertices. This means the Edge will need to keep track of what the outer Polygon's vertices are
// along its border with the inner Polygon, and what the inner Polygon's vertices are for that
// same border.

public class Edge
{
    public readonly Polygon InnerPoly; //The Poly that's inside the Edge. The one we'll be extruding or insetting.
    public readonly Polygon OuterPoly; //The Poly that's outside the Edge. We'll be leaving this one alone.

    public readonly List<int> OuterVerts; //The vertices along this edge, according to the Outer poly.
    public readonly List<int> InnerVerts; //The vertices along this edge, according to the Inner poly.

    public readonly int InwardDirectionVertex; //The third vertex of the inner polygon. That is, the one that doesn't touch this edge.

    public Edge(Polygon innerPoly, Polygon outerPoly)
    {
        InnerPoly = innerPoly;
        OuterPoly = outerPoly;
        InnerVerts = new List<int>(2);

        // Examine all three of the inner poly's vertices. Add the vertices that it shares with the
        // outer poly to the m_InnerVerts list. We also make a note of which vertex wasn't on the edge
        // and store it for later in m_InwardDirectionVertex.

        foreach (int vertex in innerPoly.Vertices)
        {
            if (outerPoly.Vertices.Contains(vertex))
            {
                InnerVerts.Add(vertex);
            }
            else
            {
                InwardDirectionVertex = vertex;
            }
        }

        // Calculate the 'inward direction', a vector that goes from the midpoint of the edge, to the third vertex on
        // the inner poly (the vertex that isn't part of the edge). This will come in handy later if we want to push
        // vertices directly away from the edge.

        // For consistency, we want the 'winding order' of the edge to be the same as that of the inner
        // polygon. So the vertices in the edge are stored in the same order that you would encounter them if
        // you were walking clockwise around the polygon. That means the pair of edge vertices will be:
        // [1st inner poly vertex, 2nd inner poly vertex] or
        // [2nd inner poly vertex, 3rd inner poly vertex] or
        // [3rd inner poly vertex, 1st inner poly vertex]
        //
        // The formula above will give us [1st inner poly vertex, 3rd inner poly vertex] though, so
        // we check for that situation and reverse the vertices.

        if (InnerVerts[0] == innerPoly.Vertices[0] && InnerVerts[1] == innerPoly.Vertices[2])
        {
            int temp = InnerVerts[0];
            InnerVerts[0] = InnerVerts[1];
            InnerVerts[1] = temp;
        }

        // No manipulations have happened yet, so the outer and inner Polygons still share the same vertices.
        // We can instantiate m_OuterVerts as a copy of m_InnerVerts.

        OuterVerts = new List<int>(InnerVerts);
    }
}

// EdgeSet is a collection of unique edges. Basically it's a HashSet, but we have
// extra convenience functions that we'd like to include in it.

public class EdgeSet : HashSet<Edge>
{
    // Split - Given a list of original vertex indices and a list of replacements,
    //         update m_InnerVerts to use the new replacement vertices.

    public void Split(IList<int> oldVertices, IList<int> newVertices)
    {
        foreach (Edge edge in this)
        {
            for (int i = 0; i < 2; i++)
            {
                edge.InnerVerts[i] = newVertices[oldVertices.IndexOf(edge.OuterVerts[i])];
            }
        }
    }

    // GetUniqueVertices - Get a list of all the vertices referenced
    // in this edge loop, with no duplicates.

    public static IList<int> GetUniqueVertices(HashSet<Edge> edges)
    {
        var vertices = new List<int>();

        foreach (int vert in edges.SelectMany(edge => edge.OuterVerts.Where(vert => !vertices.Contains(vert))))
        {
            vertices.Add(vert);
        }

        return vertices;
    }

    // GetInwardDirections - For each vertex on this edge, calculate the direction that
    // points most deeply inwards. That's the average of the inward direction of each edge
    // that the vertex appears on.

    public static Dictionary<int, Vector3> GetInwardDirections(HashSet<Edge> edges, IList<Vector3> vertexPositions)
    {
        var inwardDirections = new Dictionary<int, Vector3>();
        var numContributions = new Dictionary<int, int>();

        foreach (Edge edge in edges)
        {
            Vector3 innerVertexPosition = vertexPositions[edge.InwardDirectionVertex];

            Vector3 edgePosA = vertexPositions[edge.InnerVerts[0]];
            Vector3 edgePosB = vertexPositions[edge.InnerVerts[1]];
            Vector3 edgeCenter = Vector3.Lerp(edgePosA, edgePosB, 0.5f);

            Vector3 innerVector = (innerVertexPosition - edgeCenter).normalized;

            for (int i = 0; i < 2; i++)
            {
                int edgeVertex = edge.InnerVerts[i];

                if (inwardDirections.ContainsKey(edgeVertex))
                {
                    inwardDirections[edgeVertex] += innerVector;
                    numContributions[edgeVertex]++;
                }
                else
                {
                    inwardDirections.Add(edgeVertex, innerVector);
                    numContributions.Add(edgeVertex, 1);
                }
            }
        }

        // Now we average the contributions that each vertex received, and we can return the result.

        foreach (KeyValuePair<int, int> kvp in numContributions)
        {
            int vertexIndex = kvp.Key;
            int contributionsToThisVertex = kvp.Value;
            inwardDirections[vertexIndex] = (inwardDirections[vertexIndex] / contributionsToThisVertex).normalized;
        }

        return inwardDirections;
    }
}