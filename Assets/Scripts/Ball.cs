﻿using System;
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
    public void PlaceInOrbit()
    {
        var planets = World.allPlanets;
        var planet = planets[Random.Range(0, planets.Count)];
        var gravityObject = planet.GetComponent<GravityObject>();
        transform.position = planet.transform.position;
        var distance = Random.Range(gravityObject.radius + radius *2, gravityObject.radiusGravity - radius);
        transform.Translate(distance, 0, 0);
        var a = Random.Range((distance + planet.radius + radius) / 2, (distance + gravityObject.radiusGravity - planet.radius - radius) / 2);
        var orbitModifier = (2 / distance - 1 / a);
        velocity = Vector3.forward * (float) Math.Sqrt(G * planet.mass * orbitModifier);
        frozen = false;
    }

    protected override void OnUpdate()
    {
        if (velocity != Vector3.zero)
        {
            movingParticleSystem.transform.LookAt(transform.position + velocity);
        }

        var emissionModule = movingParticleSystem.emission;
        emissionModule.enabled = !frozen && velocity.sqrMagnitude > 0.5;
 
        
    }


}