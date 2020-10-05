using UnityEngine;

namespace Objects.Player
{
    [RequireComponent(typeof(PlayerController), typeof(SmoothCamera))]
    public class VehicleTracker : MonoBehaviour
    {
        private PlayerController _playerController;
        private SmoothCamera _smoothCamera;
        private Ball spaceVehicle;
        
        void Start()
        {
            _playerController = GetComponent<PlayerController>();
            _smoothCamera = GetComponent<SmoothCamera>();
        }

        void Update()
        {
            if (!spaceVehicle)
            {
                spaceVehicle = GameObject.Find("Ball").GetComponent<Ball>();
            }

            
            if (spaceVehicle.inOrbitAround)
            {
                var pos = spaceVehicle.inOrbitAround.transform.position;
                _smoothCamera.SetTarget(pos);
            }
            else
            {
                var spaceVehiclePosition = spaceVehicle.transform.position;
                Debug.Log(spaceVehicle);
                _smoothCamera.SetTarget(spaceVehiclePosition);
            }
        }
    }
}
