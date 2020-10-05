using UnityEngine;

namespace Objects.Player
{
    [RequireComponent(typeof(PlayerController), typeof(SmoothCamera))]
    public class VehicleTracker : MonoBehaviour
    {
        private PlayerController _playerController;
        private SmoothCamera _smoothCamera;
        private Ball _spaceVehicle;
        private Planet _lastOrbitedPlanet;
        
        void Start()
        {
            _playerController = GetComponent<PlayerController>();
            _smoothCamera = GetComponent<SmoothCamera>();
        }

        void Update()
        {
            if (!_spaceVehicle)
            {
                _spaceVehicle = GameObject.Find("Ball").GetComponent<Ball>();
            }

            
            if (_spaceVehicle.inOrbitAround)
            {
                var planet = _spaceVehicle.inOrbitAround;
                if (planet != _lastOrbitedPlanet)
                {
                    _lastOrbitedPlanet = planet;
                    var dimensions = new Vector2(2 * planet.radiusGravity, 2 * planet.radiusGravity) * 1.2f;
                    var distance = Helper.DistanceToFillFrustum(_playerController.playerCamera, dimensions);
                    _smoothCamera.SetZoomTarget(distance);

                    var pos = _spaceVehicle.inOrbitAround.transform.position;
                    _smoothCamera.SetTarget(pos);
                }
            }
            else
            {
                _lastOrbitedPlanet = null;
                var spaceVehiclePosition = _spaceVehicle.transform.position;
                _smoothCamera.SetTarget(spaceVehiclePosition);
            }
        }
    }
}
