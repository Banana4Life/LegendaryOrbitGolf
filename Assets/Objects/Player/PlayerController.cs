using UnityEngine;

namespace Objects.Player
{
    public class PlayerController : MonoBehaviour
    {
        public bool enableEdgeScrolling = true;
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

        private Vector2 DetectEdgeScroll()
        {
            if (!(enableEdgeScrolling && Screen.fullScreen && !Application.isEditor))
            {
                return Vector2.zero;
            }        
            var mousePos = Input.mousePosition;
            float x = 0;
            float z = 0;
            if (mousePos.x >= 0 && mousePos.x < borderSize)
            {
                x = -1;
            }
            if (mousePos.x < Screen.width && mousePos.x >= Screen.width - borderSize)
            {
                x = 1;
            }

            if (mousePos.y >= 0 && mousePos.y < borderSize)
            {
                z = -1;
            }

            if (mousePos.y < Screen.height && mousePos.y >= Screen.height - borderSize)
            {
                z = 1;
            }
            
            return new Vector2(x, z);
        }

        Vector2 DetectKeyboardScroll()
        {
            float x = 0;
            float z = 0;
        
            if (Input.GetKey(KeyCode.A))
            {
                x = -1;
            }
            if (Input.GetKey(KeyCode.D))
            {
                x = 1;
            }

            if (Input.GetKey(KeyCode.S))
            {
                z = -1;
            }

            if (Input.GetKey(KeyCode.W))
            {
                z = 1;
            }
            
            return new Vector2(x, z);
        }

        private Vector2 Clamp(Vector2 x)
        {
            return new Vector2(Mathf.Clamp(x.x, -1, 1), Mathf.Clamp(x.y, -1, 1));
        }

        // Update is called once per frame
        void Update()
        {

            var scroll = Clamp(DetectEdgeScroll() + DetectKeyboardScroll()).normalized;

            var t = transform;
            t.Translate(new Vector3(scroll.x, 0, scroll.y) * (speed * Time.deltaTime));

            var currentCameraHeight = _cameraTransform.localPosition.y;
            var newCameraHeight = Mathf.Clamp( currentCameraHeight - (Input.mouseScrollDelta.y * zoomSpeed), minZoom, maxZoom);
            _cameraTransform.localPosition = new Vector3(0, newCameraHeight, 0);

            if (Input.GetButton("Jump"))
            {
                t.position = t.parent.GetComponentInChildren<Ball>().transform.position;
            }
        }
    }
}
