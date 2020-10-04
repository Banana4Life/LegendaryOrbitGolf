using System;
using System.Collections.Generic;
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
            ((Ball) target).PlaceInOrbit(GameObject.Find("World").GetComponent<World>());
        }
    }
}
public class Ball : GravityObject
{
    public ParticleSystem movingParticleSystem;
    public ParticleSystem breakParticleSystem;

    public bool dead;

    private Vector3 savePosition;
    private Vector3 saveVelocity;
    
    public bool inStableOrbit;
    public void PlaceInOrbit(World world)
    {
        var planets = world.allPlanets.FindAll(p => p.mass > 0);
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

        savePosition = transform.position;
        saveVelocity = velocity;
    }

    private Vector2 oldPos = Vector2.zero;
    private Vector2 orbitPoint = Vector2.zero;
    public int revolutions = 0;

    public void OnCollided()
    {
        frozen = true;
        dead = true;
    }
    protected override void OnUpdate(World world)
    {
        if (velocity != Vector3.zero)
        {
            movingParticleSystem.transform.LookAt(transform.position + velocity);
        }

        var emissionModule = movingParticleSystem.emission;
        emissionModule.enabled = !frozen && velocity.sqrMagnitude > 0.5;

        var nearbyPlanets = world.allPlanets.FindAll(p => (p.transform.position - transform.position).sqrMagnitude < Math.Pow(p.radiusGravity, 2));
        var newPos = new Vector2(transform.position.x, transform.position.z);
        foreach (var nearbyPlanet in nearbyPlanets)
        {
            var planetPos = nearbyPlanet.transform.position;

            Vector2 intersection;
            if (LinesUtil.LineSegmentsIntersection(oldPos, newPos, new Vector2(planetPos.x, planetPos.z), new Vector2(planetPos.x, planetPos.z + nearbyPlanet.radiusGravity), out intersection))
            {
                if ((orbitPoint - intersection).sqrMagnitude < 0.1)
                {
                    revolutions++;
                    if (revolutions > 2)
                    {
                        if (!inStableOrbit)
                        {
                            savePosition = transform.position;
                            saveVelocity = velocity;
                        }
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
        savePosition = pos;
        saveVelocity = velocity;
    }

    public void EngangeBreaks()
    {
        velocity *= 0.8f;
        breakParticleSystem.Play();
        // TODO sounds
    }

    public void PrepareBump()
    {
        Freeze();
        if (dead)
        {
            dead = false;
            transform.position = savePosition;
            velocity = saveVelocity;
        }
        else
        {
            if (inStableOrbit)
            {
                savePosition = transform.position;
                saveVelocity = velocity;    
            }
        }
    }
}