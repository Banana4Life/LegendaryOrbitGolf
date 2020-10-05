using UnityEngine;

namespace Objects.Player
{
    [RequireComponent(typeof(SmoothCamera))]
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
        private SmoothCamera _smoothCamera;
    
        // Start is called before the first frame update
        void Start()
        {
            _cameraTransform = playerCamera.transform;
            _smoothCamera = GetComponent<SmoothCamera>();
        
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
                x += -1;
            }
            if (mousePos.x < Screen.width && mousePos.x >= Screen.width - borderSize)
            {
                x += 1;
            }

            if (mousePos.y >= 0 && mousePos.y < borderSize)
            {
                z += -1;
            }

            if (mousePos.y < Screen.height && mousePos.y >= Screen.height - borderSize)
            {
                z += 1;
            }

            if (x == 0 && z == 0)
            {
                return Vector2.zero;
            }
            
            return new Vector2(x, z);
        }

        Vector2 DetectKeyboardScroll()
        {
            float x = 0;
            float z = 0;
        
            if (Input.GetKey(KeyCode.A))
            {
                x += -1;
            }
            if (Input.GetKey(KeyCode.D))
            {
                x += 1;
            }

            if (Input.GetKey(KeyCode.S))
            {
                z += -1;
            }

            if (Input.GetKey(KeyCode.W))
            {
                z += 1;
            }

            if (x == 0 && z == 0)
            {
                return Vector2.zero;
            }
            
            return new Vector2(x, z);
        }
        
        void Update()
        {
            var t = transform;
            var scroll = Helper.Clamp(DetectEdgeScroll() + DetectKeyboardScroll()).normalized;
            

            if (scroll != Vector2.zero)
            {
                _smoothCamera.SetRelativeTarget(scroll * (speed * Time.deltaTime));
            }

            var scrollValue = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scrollValue) > float.Epsilon)
            {
                var currentCameraHeight = _cameraTransform.localPosition.y;
                var newCameraHeight = Mathf.Clamp(currentCameraHeight - (scrollValue * zoomSpeed), minZoom, maxZoom);
                _cameraTransform.localPosition = new Vector3(0, newCameraHeight, 0);
                _smoothCamera.SetZoom(newCameraHeight);
            }

            if (Input.GetButton("Jump"))
            {
                _smoothCamera.SetTarget(t.parent.GetComponentInChildren<Ball>().transform.position);
            }
        }
    }
}
