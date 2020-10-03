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
    protected override void Init()
    {
        radius = 0.25f;
        mass = 10;
        gravityAffected = true;
        moving = true;
        base.Init();
        frozen = true;
    }

    public void PlaceInOrbit()
    {
        var planets = allGravityObjects.FindAll(go => go is Planet);
        var planet = planets[Random.Range(0, planets.Count)];
        var gravityObject = planet.GetComponent<GravityObject>();
        transform.position = planet.transform.position;
        var distance = Random.Range(gravityObject.radius + radius *2, gravityObject.radiusGravity - radius);
        transform.Translate(distance, 0, 0);
        velocity = Vector3.forward * (float) Math.Sqrt(G * planet.mass * 1 / distance);
    }


}