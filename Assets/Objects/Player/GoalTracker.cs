using System;
using UnityEditor;
using UnityEngine;

namespace Objects.Player
{
    public class GoalTracker : MonoBehaviour
    {
        public World world;

        void Update()
        {
            if (world.ballObject)
            {
                if (world.goalPlanet != transform.GetComponentInParent<VehicleTracker>()._lastOrbitedPlanet)
                {
                    var pos = transform.position;
                    var goalPosition = world.goalPlanet.transform.position;
                    var dir = goalPosition - pos;
                    transform.rotation = Quaternion.AngleAxis((Mathf.Atan2(dir.x, dir.z) + Mathf.PI) * Mathf.Rad2Deg, Vector3.up);
                    gameObject.GetComponentInChildren<MeshRenderer>().enabled = true;
                }
                else
                {
                    gameObject.GetComponentInChildren<MeshRenderer>().enabled = false;
                }
            }
        }

        // private void OnDrawGizmos()
        // {
        //     var transformPosition = transform.position;
        //     transformPosition.y = 0;
        //     Handles.DrawWireDisc(transformPosition, Vector3.up, 0.5f);
        // }
    }
}
