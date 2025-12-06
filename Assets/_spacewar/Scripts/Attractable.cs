using Unity.Netcode;
using UnityEngine;

namespace MidniteOilSoftware.Multiplayer.SpaceWar
{
    public class Attractable : NetworkBehaviour
    {
        private Rigidbody2D _rigidbody;

        void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            RegisterWithNearbyGravityWells();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            UnregisterFromAllGravityWells();
        }

        public void Attract(float gravityStrength, float gravityRadius, Vector3 gravityWellPosition)
        {
            if (!IsServer) return;
            var currentPosition = transform.position;
            var directionToWell = gravityWellPosition - currentPosition;
            var distance = directionToWell.magnitude;

            if (distance > gravityRadius || distance < 0.01f)
            {
                return;
            }

            var falloff = 1f - (distance / gravityRadius);
            var forceMagnitude = gravityStrength * falloff;

            var force = directionToWell.normalized * forceMagnitude;
            if (_rigidbody && !_rigidbody.bodyType.Equals(RigidbodyType2D.Kinematic))
            {
                Debug.Log($"Attractable {name}: Applying force {force} towards gravity well at {gravityWellPosition}.");
                _rigidbody.AddForce(force, ForceMode2D.Force);
            }
            else
            {
                Debug.Log($"Attractable {name}: Moving kinematic object by {force * Time.deltaTime} towards gravity well at {gravityWellPosition}.");
                transform.position += force * Time.deltaTime;
            }
        }

        private void RegisterWithNearbyGravityWells()
        {
            var gravityWells = FindObjectsByType<GravityWell>(FindObjectsSortMode.None);
            foreach (var well in gravityWells)
            {
                RegisterWithGravityWellServerRpc(well.NetworkObjectId);
            }
        }

        private void UnregisterFromAllGravityWells()
        {
            var gravityWells = FindObjectsByType<GravityWell>(FindObjectsSortMode.None);
            foreach (var well in gravityWells)
            {
                UnregisterFromGravityWellServerRpc(well.NetworkObjectId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void RegisterWithGravityWellServerRpc(ulong gravityWellNetworkObjectId)
        {
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(gravityWellNetworkObjectId,
                    out var networkObject)) return;
            if (networkObject.TryGetComponent<GravityWell>(out var gravityWell))
            {
                gravityWell.RegisterAttractable(this);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void UnregisterFromGravityWellServerRpc(ulong gravityWellNetworkObjectId)
        {
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(gravityWellNetworkObjectId,
                    out var networkObject)) return;
            if (networkObject.TryGetComponent<GravityWell>(out var gravityWell))
            {
                gravityWell.UnregisterAttractable(this);
            }
        }
    }
}