using Unity.Netcode;
using UnityEngine;

namespace MidniteOilSoftware.Multiplayer.SpaceWar
{
    public class PlayerProjectile : NetworkBehaviour
    {
        [SerializeField] float _lifetime = 1f;
        [SerializeField] float _speed = 20f;

        bool _isDestroyed = false;
        Rigidbody2D _rigidBody;
        Transform _transform;

        void Awake()
        {
            _rigidBody = GetComponent<Rigidbody2D>();
            _transform = transform;
        }

        void Update()
        {
            if (_isDestroyed || !IsServer) return;
            _transform.position += _transform.up * (_speed * Time.deltaTime);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                Invoke(nameof(DestroyProjectile), _lifetime);
            }

            if (_rigidBody)
            {
                _rigidBody.linearVelocity = transform.up * _speed;
            }
        }

        public void InitializeProjectile(int projectileLayer)
        {
            if (!IsServer) return;

            gameObject.layer = projectileLayer;

            if (TryGetComponent<Collider2D>(out var projectileCollider))
            {
                projectileCollider.enabled = true;
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log(
                $"PlayerProjectile {name}: Triggered with {other.name}, _isDestroyed={_isDestroyed}, IsServer={IsServer}");
            if (_isDestroyed) return;
            if (!IsServer) return;

            if (other.GetComponentInParent(typeof(IDestroyable)) is not IDestroyable destroyable)
            {
                Debug.Log($"{name} hit {other.name} which is not destroyable.");
                DestroyProjectile();
                return;
            }

            Debug.Log($"{name} calling DestroyTarget on {other.name}");
            destroyable?.DestroyTarget();
            DestroyProjectile();
        }

        void DestroyProjectile()
        {
            if (!IsServer) return;
            if (_isDestroyed) return;

            _isDestroyed = true;

            if (NetworkObject && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn();
            }
        }
    }
}