using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using Random = UnityEngine.Random;

public class GravityObject : MonoBehaviour
{
    private static List<GravityObject> allGravityObjects = new List<GravityObject>();

    private const float G = 0.1f;

    public float radius;
    public float radiusGravity;
    public float mass;
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
        allGravityObjects.Add(this);
        
        foreach (var componentsInChild in transform.GetComponentsInChildren<MeshRenderer>())
        {
            componentsInChild.transform.localScale = new Vector3(radius*2, radius*2, radius*2);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (gravityAffected)
        {
            acceleration = Vector3.zero;
            foreach (var gravityObject in allGravityObjects)
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
            gravityAffected = false;
            moving = false;
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, this.radiusGravity);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, this.radius);
    }


}