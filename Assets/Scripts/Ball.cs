using System;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

[CustomEditor(typeof(Ball))]
class BallEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Place in Orbit"))
        {
            ((Ball) target).PlaceInOrbit();
        }
    }
}
public class Ball : GravityObject
{
    public ParticleSystem movingParticleSystem;
    public ParticleSystem breakParticleSystem;

    public bool inStableOrbit;
    public void PlaceInOrbit()
    {
        var planets = World.allPlanets.FindAll(p => p.mass > 0);
        if (planets.Count == 0)
        {
            return;
        }
        var planet = planets[Random.Range(0, planets.Count)];
        var gravityObject = planet.GetComponent<GravityObject>();
        transform.position = planet.transform.position;
        var distance = Random.Range(gravityObject.radius + radius *2, gravityObject.radiusGravity - radius);
        transform.Translate(distance, 0, 0);
        var a = Random.Range((distance + planet.radius + radius) / 2, (distance + gravityObject.radiusGravity - planet.radius - radius) / 2);
        var orbitModifier = (2 / distance - 1 / a);
        velocity = Vector3.forward * (float) Math.Sqrt(G * planet.mass * orbitModifier);
        frozen = false;
        inStableOrbit = true;
        
        movingParticleSystem.Clear();
        breakParticleSystem.Clear();
    }

    private Vector2 oldPos = Vector2.zero;
    private Vector2 orbitPoint = Vector2.zero;
    public int revolutions = 0;

    protected override void OnUpdate()
    {
        if (velocity != Vector3.zero)
        {
            movingParticleSystem.transform.LookAt(transform.position + velocity);
        }

        var emissionModule = movingParticleSystem.emission;
        emissionModule.enabled = !frozen && velocity.sqrMagnitude > 0.5;


        var nearbyPlanets = World.allPlanets.FindAll(p => (p.transform.position - transform.position).sqrMagnitude < Math.Pow(p.radiusGravity, 2));
        var newPos = new Vector2(transform.position.x, transform.position.z);
        foreach (var nearbyPlanet in nearbyPlanets)
        {
            var planetPos = nearbyPlanet.transform.position;

            Vector2 intersection;
            if (LineSegmentsIntersection(oldPos, newPos, new Vector2(planetPos.x, planetPos.z), new Vector2(planetPos.x, planetPos.z + nearbyPlanet.radiusGravity), out intersection))
            {
                if ((orbitPoint - intersection).sqrMagnitude < 0.1)
                {
                    revolutions++;
                    if (revolutions > 3)
                    {
                        inStableOrbit = true;
                    }
                }
                else
                {
                    inStableOrbit = false;
                    orbitPoint = intersection;
                    revolutions = 0;
                }
            }
        }

        oldPos = newPos;
    }

    public void Freeze()
    {
        frozen = true;
    }

    public void UnFreeze()
    {
        frozen = false;
    }

    public void Bump(Vector3 dv)
    {
        orbitPoint = oldPos;
        inStableOrbit = false;
        frozen = false;
        velocity += dv;
    }

    public void CheatJumpTo(Vector3 pos)
    {
        transform.position = pos;
        velocity = Vector3.zero;
        frozen = true;
    }

    public void EngangeBreaks()
    {
        velocity *= 0.8f;
        breakParticleSystem.Play();
        // TODO sounds
    }
    
    // https://github.com/setchi/Unity-LineSegmentsIntersection
    // The MIT License (MIT)
    //
    // Copyright (c) 2017 setchi
    //
    // Permission is hereby granted, free of charge, to any person obtaining a copy
    // of this software and associated documentation files (the "Software"), to deal
    // in the Software without restriction, including without limitation the rights
    // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    // copies of the Software, and to permit persons to whom the Software is
    // furnished to do so, subject to the following conditions:
    //
    // The above copyright notice and this permission notice shall be included in all
    // copies or substantial portions of the Software.
    //
    // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    // SOFTWARE.
    public static bool LineSegmentsIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
    {
        intersection = Vector2.zero;

        var d = (p2.x - p1.x) * (p4.y - p3.y) - (p2.y - p1.y) * (p4.x - p3.x);

        if (d == 0.0f)
        {
            return false;
        }

        var u = ((p3.x - p1.x) * (p4.y - p3.y) - (p3.y - p1.y) * (p4.x - p3.x)) / d;
        var v = ((p3.x - p1.x) * (p2.y - p1.y) - (p3.y - p1.y) * (p2.x - p1.x)) / d;

        if (u < 0.0f || u > 1.0f || v < 0.0f || v > 1.0f)
        {
            return false;
        }

        intersection.x = p1.x + u * (p2.x - p1.x);
        intersection.y = p1.y + u * (p2.y - p1.y);

        return true;
    }

}