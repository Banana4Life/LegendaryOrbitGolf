using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using Random = UnityEngine.Random;

public class GravityObject : MonoBehaviour
{
    public const float G = 0.1f;

    public float radius;
    public float radiusGravity;
    public float mass;
    public bool frozen = false;
    public bool gravityAffected = false;
    public bool moving = false;

    private Vector3 acceleration = Vector3.zero;
    public Vector3 velocity = Vector3.zero;
    
    public GameObject inOrbitAround;

    private World world;

    private void Start()
    {
        world = GetComponentInParent<World>();
    }

    // Update is called once per frame
    void Update()
    {
        if (frozen)
        {
            return;
        }
        if (gravityAffected)
        {
            acceleration = Vector3.zero;
            foreach (var planet in world.allPlanets)
            {
                var delta = transform.position - planet.transform.position;
                if (CheckCollided(delta, radius + planet.radius))
                {
                    if (this is Ball)
                    {
                        ((Ball) this).OnCollided();
                    }
                    return;
                }

                if (CheckCollided(delta, radiusGravity + planet.radiusGravity))
                {
                    inOrbitAround = planet.gameObject;
                }
                acceleration -= CalcGravityAcceleration(delta, mass, planet);
            }
            if (inOrbitAround)
            {
                var delta = transform.position - inOrbitAround.transform.position;
                if (!CheckCollided(delta, radiusGravity + inOrbitAround.GetComponent<GravityObject>().radiusGravity))
                {
                    inOrbitAround = null;
                }
            }
        }
        

        if (moving)
        {
            // ApplyGravity();
            var dt = (float) Math.Round(Time.deltaTime, 2);
            velocity += acceleration * dt;
            transform.Translate(velocity * dt);
        }
        
        OnUpdate(world);

    }

    protected virtual void OnUpdate(World world)
    {
    }

    public static bool CheckCollided(Vector3 delta, float radii)
    {
        var distanceSquared = delta.sqrMagnitude;
        var distanceBorder = Math.Pow(radii, 2);
        return distanceBorder > distanceSquared;
    }

    public static Vector3 CalcGravityAcceleration(Vector3 delta, float mass, GravityObject other)
    {
        var distanceSquared = delta.sqrMagnitude;
        if (distanceSquared < Math.Pow(other.radiusGravity, 2))
        {
            var forceMagnitude = (G * mass * other.mass) / distanceSquared;
            var distance = (float) Math.Sqrt(distanceSquared);
            var dax = forceMagnitude * delta.x / distance / mass;
            var daz = forceMagnitude * delta.z / distance / mass;
            
            return new Vector3(dax, 0, daz);
        }
        return Vector3.zero;
    }
    
    
    void OnDrawGizmosSelected()
    {
        var pos = transform.position;
        
        Handles.color = Color.red;
        Handles.DrawWireDisc(pos, Vector3.up, radiusGravity);

        Handles.color = Color.blue;
        Handles.DrawWireDisc(pos, Vector3.up, radius);
        
        Gizmos.color = Color.green;
        Gizmos.DrawLine(pos, pos + velocity);
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(pos, pos + acceleration * Time.deltaTime);
        
        if (this is Ball)
        {
            foreach (var planet in world.allPlanets)
            {
                Handles.color = Color.red;
                Handles.DrawWireDisc(planet.transform.position, Vector3.up, planet.radiusGravity);
            }
            
            Gizmos.color = Color.gray;
            if (inOrbitAround)
            {
                Gizmos.DrawLine(transform.position, inOrbitAround.transform.position);
            }
        }
    }


}