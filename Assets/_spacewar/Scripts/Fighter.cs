using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

namespace MidniteOilSoftware.Multiplayer.SpaceWar
{
    public class Fighter : NetworkBehaviour
    {
        [Header("Movement Settings")] 
        [SerializeField] float _turnSpeed = 200f;
        [SerializeField] float _thrustSpeed = 120f;
        
        [Header("Weapon Settings")]
        [SerializeField] PlayerProjectile _projectilePrefab;
        [SerializeField] float _fireRate = 0.5f;
        
        Transform _transform;
        Rigidbody2D _rigidBody;
        readonly NetworkVariable<bool> _thrusting = new(writePerm: NetworkVariableWritePermission.Owner);
        float _rotationInput;
        FighterVisuals _visuals;
        float _nextFireTime;
        int _projectileLayer;

        SpaceWarGameManager _gameManager;

        SpaceWarGameManager SWGameManager
        {
            get
            {
                if (!_gameManager)
                {
                    _gameManager = FindFirstObjectByType<SpaceWarGameManager>(FindObjectsInactive.Include);
                }
                return _gameManager;
            }
        }

        void Awake()
        {
            _transform = transform;
            _rigidBody = GetComponent<Rigidbody2D>();

            if (!_rigidBody)
            {
                Debug.LogError("Fighter requires a Rigidbody2D on parent GameObject!", this);
            }
            _transform.localPosition = Vector3.zero;
            _transform.localRotation = Quaternion.identity;
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Initialize();
        }

        void Initialize()
        {
            _visuals = GetComponentInChildren<FighterVisuals>();

            if (!IsServer) return;
            var ownerClientId = GetComponentInParent<NetworkBehaviour>()?.OwnerClientId ?? 0;
            _projectileLayer = (ownerClientId == 0) 
                ? LayerMask.NameToLayer("Player 1 Projectile") 
                : LayerMask.NameToLayer("Player 2 Projectile");
        }

        void Update()
        {
            if (!IsOwner) return;
            if (!SWGameManager || !SWGameManager.IsPlaying.Value) return;
            HandleLegacyInput();
        }

        void LateUpdate()
        {
            _visuals?.ShowExhaust(_thrusting.Value);
        }

        void FixedUpdate()
        {
            if (!IsOwner || !_rigidBody) return;

            HandleRotation();
            HandleThrust();
        }

        void HandleLegacyInput()
        {
            _rotationInput = 0f;

            if (Keyboard.current == null) return;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                _rotationInput = 1f;
            }
            else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                _rotationInput = -1f;
            }

            _thrusting.Value = (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed);
            if (Time.time < _nextFireTime) return;
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                FireProjectileServerRpc();
            }
        }

        [Rpc(SendTo.Server)]
        void FireProjectileServerRpc()
        {
            _nextFireTime = Time.time + _fireRate;
            if (!_projectilePrefab || !_visuals?.MuzzleTransform)
            {
                Debug.LogWarning("Fighter:FireProjectileServerRpc - Missing projectile prefab or muzzle transform.");
                return;
            }
            
            var projectile = Instantiate(_projectilePrefab, _visuals.MuzzleTransform.position, _transform.rotation);
            if (projectile.TryGetComponent<NetworkObject>(out var networkObject))
            {
                networkObject.Spawn();
                
                if (projectile.TryGetComponent<PlayerProjectile>(out var playerProjectile))
                {
                    playerProjectile.InitializeProjectile(_projectileLayer);
                }
            }
            else
            {
                Debug.LogError("Fighter:FireProjectileServerRpc - Projectile prefab is missing NetworkObject component!");
                Destroy(projectile.gameObject);
            }
        }

        void HandleRotation()
        {
            if (Mathf.Approximately(_rotationInput, 0f)) return;

            var currentRotation = transform.rotation.eulerAngles.z;
            var newRotation = currentRotation + (_rotationInput * _turnSpeed * Time.fixedDeltaTime);
            _transform.rotation = Quaternion.Euler(0, 0, newRotation);
        }

        void HandleThrust()
        {
            if (!IsOwner || !_thrusting.Value) return;

            var thrustAmount = _thrustSpeed * Time.fixedDeltaTime;
            var force = _transform.up * thrustAmount;
            _rigidBody.AddForce(force);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer) return;

            if (other.TryGetComponent<IDestroyable>(out var otherDestroyable))
            {
                Debug.Log($"Fighter {name}: Collided with {other.name} (IDestroyable)");
                otherDestroyable.DestroyTarget();
            }
            else if (other.transform.parent && 
                     other.transform.parent.TryGetComponent<IDestroyable>(out var otherParentDestroyable))
            {
                Debug.Log($"Fighter {name}: Collided with {other.name}'s parent (IDestroyable)");
                otherParentDestroyable.DestroyTarget();
            }
        }
    }
}
