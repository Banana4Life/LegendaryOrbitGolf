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

    public Vector3 velocity = Vector3.zero;
    
    public Planet inOrbitAround;

    void OnDrawGizmosSelected()
    {
        var pos = transform.position;
        
        Handles.color = Color.red;
        Handles.DrawWireDisc(pos, Vector3.up, radiusGravity);

        Handles.color = Color.blue;
        Handles.DrawWireDisc(pos, Vector3.up, radius);
        
        Gizmos.color = Color.green;
        Gizmos.DrawLine(pos, pos + velocity);
        // Gizmos.color = Color.magenta;
        // Gizmos.DrawLine(pos, pos + acceleration * Time.deltaTime);
        
        if (this is Ball)
        {
            Gizmos.color = Color.gray;
            if (inOrbitAround)
            {
                Gizmos.DrawLine(transform.position, inOrbitAround.transform.position);
            }
        }
    }
}