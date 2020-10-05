using UnityEngine;

namespace Objects.Player
{
    public class SmoothCamera : MonoBehaviour
    {
        public float rate = 0.2f;
        private Vector2 _target;
        private bool _hasTarget;

        public void SetTarget(Vector2 v)
        {
            _target = v;
            _hasTarget = true;
        }

        public void SetTarget(float x, float z)
        {
            SetTarget(new Vector2(x, z));
        }

        public void SetRelativeTarget(Vector2 v)
        {
            SetTarget(Helper.ToVector2(transform.position) + v);
        }

        public void SetTarget(Vector3 v)
        {
            SetTarget(Helper.ToVector2(v));
        }
        
        void Update()
        {
            if (_hasTarget)
            {
                var t = transform;
                t.position = Helper.ToVector3(Vector2.Lerp(Helper.ToVector2(t.position), _target, rate), t.position.y);
            }
        }
    }
}
