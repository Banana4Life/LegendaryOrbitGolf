using UnityEngine;

namespace Objects.Player
{
    public class PlayerController : MonoBehaviour
    {
        public float speed = 80;
        public float zoomSpeed = 10;
        public float minZoom = 0;
        public float maxZoom = 100;
        public float startZoom = 0.5f;

        public float borderSize = 30;

        public Camera playerCamera;
        private Transform _cameraTransform;

        public GameObject background;
    
        // Start is called before the first frame update
        void Start()
        {
        
            _cameraTransform = playerCamera.transform;
            var pos = _cameraTransform.position;
        
            _cameraTransform.rotation = Quaternion.AngleAxis(90, Vector3.right);
            _cameraTransform.position = new Vector3(0, Mathf.Lerp(minZoom, maxZoom, startZoom), 0);
        }

        // Update is called once per frame
        void Update()
        {
            var mousePos = Input.mousePosition;

            float x = 0;
            float z = 0;
        
            if (mousePos.x >= 0 && mousePos.x < borderSize || Input.GetKey(KeyCode.A))
            {
                x = -1;
            }
            if (mousePos.x < Screen.width && mousePos.x >= Screen.width - borderSize || Input.GetKey(KeyCode.D))
            {
                x = 1;
            }

            if (mousePos.y >= 0 && mousePos.y < borderSize || Input.GetKey(KeyCode.S))
            {
                z = -1;
            }

            if (mousePos.y < Screen.height && mousePos.y >= Screen.height - borderSize || Input.GetKey(KeyCode.W))
            {
                z = 1;
            }

            transform.Translate(new Vector3(x, 0, z).normalized * (speed * Time.deltaTime));

            var currentCameraHeight = _cameraTransform.localPosition.y;
            var newCameraHeight = Mathf.Clamp( currentCameraHeight - (Input.mouseScrollDelta.y * zoomSpeed), minZoom, maxZoom);
            _cameraTransform.localPosition = new Vector3(0, newCameraHeight, 0);
        }
    }
}
