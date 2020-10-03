using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float speed = 80;
    public float zoomSpeed = 10;

    public float borderSize = 30;

    public float height = 100;
    
    // Start is called before the first frame update
    void Start()
    {
        Transform t = transform;
        var pos = t.position;
        
        t.rotation = Quaternion.AngleAxis(90, Vector3.right);
        t.position = new Vector3(pos.x, height, pos.z);
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

        var translation = new Vector3(x, 0, z).normalized * (speed * Time.deltaTime);
        translation.y = -Input.mouseScrollDelta.y * zoomSpeed;
        transform.Translate(translation, Space.World);
        
        
    }
}
