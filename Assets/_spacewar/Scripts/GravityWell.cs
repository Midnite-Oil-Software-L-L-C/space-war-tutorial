using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MidniteOilSoftware.Multiplayer.SpaceWar
{
    public class GravityWell : NetworkBehaviour
    {
        [SerializeField] float _gravityStrength = 4f;
        [SerializeField] float _gravityRadius = 5f;
        [SerializeField] LayerMask _affectedLayers;
        [SerializeField] float _rotationSpeed = 30f;

        private readonly List<Attractable> _registeredAttractables = new List<Attractable>();

        public void RegisterAttractable(Attractable attractable)
        {
            Debug.Log($"RegisterAttractable called on GravityWell {name} for Attractable {attractable?.name}, IsServer={IsServer}");
            if (!IsServer) return;

            if (!attractable || _registeredAttractables.Contains(attractable)) return;
            Debug.Log($"Registered Attractable {attractable.name} to GravityWell {name}");
            _registeredAttractables.Add(attractable);
        }

        public void UnregisterAttractable(Attractable attractable)
        {
            if (!IsServer) return;

            if (!attractable) return;
            _registeredAttractables.Remove(attractable);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsServer) return;

            Debug.Log($"GravityWell {name}: OnNetworkSpawn - Registering existing attractables.");
            var attractables = FindObjectsByType<Attractable>(FindObjectsSortMode.None);
            foreach (var attractable in attractables)
            {
                var layerMask = 1 << attractable.gameObject.layer;
                if ((_affectedLayers.value & layerMask) != 0)
                {
                    Debug.Log(
                        $"GravityWell {name}: OnNetworkSpawn - Registering attractable {attractable.name} on layer {layerMask}.");
                    RegisterAttractable(attractable);
                }
                else
                {
                    Debug.Log(
                        $"GravityWell {name}: OnNetworkSpawn - Skipping attractable {attractable.name} on layer {layerMask}.");
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            _registeredAttractables.Clear();
            base.OnNetworkDespawn();
        }

        void Update()
        {
            transform.Rotate(Vector3.forward, _rotationSpeed * Time.deltaTime);

            if (!IsServer) return;

            for (var i = _registeredAttractables.Count - 1; i >= 0; i--)
            {
                if (!_registeredAttractables[i])
                {
                    _registeredAttractables.RemoveAt(i);
                }
                else
                {
                    Debug.Log($"Attracting Attractable {_registeredAttractables[i].name} from GravityWell {name}");
                    _registeredAttractables[i].Attract(_gravityStrength, _gravityRadius, transform.position);
                }
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer) return;

            if (other.TryGetComponent<IDestroyable>(out var destroyable))
            {
                Debug.Log($"GravityWell {name}: Destroying {other.name}");
                destroyable.DestroyTarget();
            }
            else if (other.transform.parent != null &&
                     other.transform.parent.TryGetComponent<IDestroyable>(out var parentDestroyable))
            {
                Debug.Log($"GravityWell {name}: Destroying {other.name} via parent {other.transform.parent.name}");
                parentDestroyable.DestroyTarget();
            }
        }
    }
}