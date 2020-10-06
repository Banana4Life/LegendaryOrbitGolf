using UnityEngine;

namespace Objects.Background
{
    public class FillCamera : MonoBehaviour
    {
        public Camera playerCamera;

        void Update()
        {
            transform.localScale = Helper.ToVector3(Helper.FrustumDimensions(playerCamera, playerCamera.farClipPlane));
        }
    }
}