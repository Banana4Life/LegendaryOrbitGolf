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
            transform.localScale = Helper.ToVector3(Helper.FrustumDimensions(playerCamera, 1.0f));
        }
    }
}