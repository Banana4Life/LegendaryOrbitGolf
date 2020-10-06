using UnityEngine;

public class PlanetRotate : MonoBehaviour
{
    public float rotationSpeed = 20.0f;
    private Vector3 _axis;
    
    // Start is called before the first frame update
    void Start()
    {
        _axis = Random.insideUnitSphere;
    }

    // Update is called once per frame
    void Update()
    {
        transform.RotateAround(transform.position, _axis, rotationSpeed * Time.deltaTime);
    }
}
