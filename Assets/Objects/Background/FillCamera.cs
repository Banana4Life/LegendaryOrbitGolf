using UnityEngine;

namespace Objects.Background
{
    public class FillCamera : MonoBehaviour
    {
        public Camera playerCamera;
        private Transform _cameraTransform;

        void Update()
        {
            _cameraTransform = playerCamera.transform;
            var pos = transform.position.y - _cameraTransform.position.y;
            var h = Mathf.Tan(playerCamera.fieldOfView * Mathf.Deg2Rad * 0.5f) * pos * 2f;
            transform.localScale = new Vector3(h * playerCamera.aspect,0f,h);
        }
    }
}
