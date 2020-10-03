using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using Random = UnityEngine.Random;

public class GravityObject : MonoBehaviour
{
    protected const float G = 0.1f;

    public float radius;
    public float radiusGravity;
    public float mass;
    public bool frozen = false;
    public bool gravityAffected = false;
    public bool moving = false;

    private Vector3 acceleration = Vector3.zero;
    public Vector3 velocity = Vector3.zero;

    void Start()
    {
        Init();
    }

    protected virtual void Init()
    {
        foreach (var componentsInChild in transform.GetComponentsInChildren<MeshRenderer>())
        {
            componentsInChild.transform.localScale = new Vector3(radius*2, radius*2, radius*2);
        }
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
            foreach (var gravityObject in World.allGravityObjects)
            {
                if (gravityObject != this)
                {
                    ApplyGravity(gravityObject);
                }
            }
        }

        if (moving)
        {
            // ApplyGravity();
            velocity += acceleration * Time.deltaTime;
            transform.Translate(velocity * Time.deltaTime);
        }
    }

    public void ApplyGravity(GravityObject from)
    {
        var pos = transform.position;
        var fromPos = from.transform.position;
        var delta = pos - fromPos;
        var distanceSquared = delta.sqrMagnitude;
        var distanceBorder = Math.Pow(radius + from.radius, 2);
        if (distanceBorder > distanceSquared)
        {
            // We collided!
            Debug.Log("Collided with " + from.name);
            frozen = true;
            return;
            
        }

        if (distanceSquared < Math.Pow(from.radiusGravity, 2))
        {
            var forceMagnitude = (G * mass * from.mass) / distanceSquared;
            var distance = (float) Math.Sqrt(distanceSquared);
            acceleration.x -= forceMagnitude * delta.x / distance / mass;
            acceleration.z -= forceMagnitude * delta.z / distance / mass;
        }
    }
    
    
    void OnDrawGizmosSelected()
    {
        var pos = transform.position;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos, radiusGravity);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(pos, radius);
        
        Gizmos.color = Color.green;
        Gizmos.DrawLine(pos, pos + velocity);
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(pos, pos + acceleration * Time.deltaTime);
        
        if (this is Ball)
        {
            foreach (var planet in World.allGravityObjects)
            {
                Handles.color = Color.red;
                Handles.DrawWireDisc(planet.transform.position, Vector3.up, planet.radiusGravity);
            }
        }
    }


}