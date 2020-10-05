using System;
using UnityEngine;

namespace Objects.Player
{
    public class GoalTracker : MonoBehaviour
    {
        public World world;

        private float angle = 0;
        // Update is called once per frame
        void Update()
        {
            var pos = transform.position;
            var goalPosition = world.goalPlanet.transform.position;
            var dir = goalPosition - pos;
            transform.rotation = Quaternion.AngleAxis((Mathf.Atan2(dir.x, dir.z) + Mathf.PI) * Mathf.Rad2Deg, Vector3.up);
        }
    }
}
