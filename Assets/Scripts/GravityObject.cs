using System;
using UnityEditor;
using UnityEngine;

public class GravityObject : MonoBehaviour
{
    public const float G = 0.1f;

    public float radius;
    public float radiusGravity;
    public float mass;
    public bool frozen = false;
    public bool gravityAffected = false;
    public bool moving = false;

    protected Vector3 acceleration = Vector3.zero;
    public Vector3 velocity = Vector3.zero;
    
    public Planet inOrbitAround;

    protected World world;

    private void Start()
    {
        world = GetComponentInParent<World>();
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
        if (!(distanceSquared < Mathf.Pow(other.radiusGravity, 2)))
        {
            return Vector3.zero;
        }

        var forceMagnitude = (G * mass * other.mass) / distanceSquared;
        var distance = Mathf.Sqrt(distanceSquared);
        var dax = forceMagnitude * delta.x / distance / mass;
        var daz = forceMagnitude * delta.z / distance / mass;
            
        return new Vector3(dax, 0, daz);
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