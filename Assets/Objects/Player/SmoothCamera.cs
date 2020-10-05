using UnityEngine;

namespace Objects.Player
{
    public class SmoothCamera : MonoBehaviour
    {
        public Camera playerCamera;
        public float rate = 0.2f;
        
        private Vector2 _target;
        private bool _hasTarget;
        private float _zoomTarget;
        private bool _hasZoomTarget;

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

        public void SetZoomTarget(float height)
        {
            _zoomTarget = height;
            _hasZoomTarget = true;
        }

        public void SetZoom(float height)
        {
            _hasZoomTarget = false;
            var t = playerCamera.transform;
            var pos = t.localPosition;
            t.localPosition = new Vector3(pos.x, height, pos.z);
        }


        void Update()
        {
            if (_hasTarget)
            {
                var t = transform;
                var pos = t.position;
                t.position = Helper.ToVector3(Vector2.Lerp(Helper.ToVector2(pos), _target, rate), pos.y);
            }

            if (_hasZoomTarget)
            {
                var camTransform = playerCamera.transform;
                var camPos = camTransform.localPosition;

                float newHeight;
                if (Mathf.Abs(camPos.y - _zoomTarget) < rate)
                {
                    Debug.Log("zoom target reached");
                    newHeight = _zoomTarget;
                    _hasZoomTarget = false;
                }
                else
                {
                    newHeight = Mathf.Lerp(camPos.y, _zoomTarget, rate);
                }
                camTransform.localPosition = new Vector3(camPos.x, newHeight, camPos.z);
            }
        }
    }
}