using UnityEngine;

namespace Objects.Background
{
    public class FillCamera : MonoBehaviour
    {
        public Camera playerCamera;
    
        void Update()
        {
            float pos = (playerCamera.nearClipPlane + 0.01f);
            var cameraTransform = playerCamera.transform;
            transform.position = cameraTransform.position + cameraTransform.forward * pos;
            var h = Mathf.Tan(playerCamera.fieldOfView * Mathf.Deg2Rad * 0.5f) * pos * 2f;
            transform.localScale = new Vector3(h * playerCamera.aspect,0f,h);
        }
    }
}
