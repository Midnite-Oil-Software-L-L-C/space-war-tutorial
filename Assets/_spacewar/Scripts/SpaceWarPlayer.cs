using MidniteOilSoftware.Core;
using MidniteOilSoftware.Multiplayer.Events;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace MidniteOilSoftware.Multiplayer.SpaceWar
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class SpaceWarPlayer : NetworkPlayer, IDestroyable
    {
        [SerializeField] FighterVisuals[] _fighterPrefabs;
        [SerializeField] Vector3[] _spawnPositions, _spawnRotations;
        [SerializeField] GameObject _explosionPrefab;

        readonly NetworkVariable<int> _fighterIndex = new();
        FighterVisuals _fighterVisual;
        PlayerInput _playerInput;
        SpaceWarGameManager _swGameManager;
        Rigidbody2D _rigidbody;

        SpaceWarGameManager GameManager
        {
            get
            {
                if (!_swGameManager)
                {
                    _swGameManager = FindFirstObjectByType<SpaceWarGameManager>(FindObjectsInactive.Include);
                    if (!_swGameManager)
                    {
                        Debug.LogError("SpaceWarPlayer:Multiplayer-Could not find SpaceWarGameManager in scene!", this);
                    }
                }

                return _swGameManager;
            }
        }

        void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsServer)
            {
                _fighterIndex.Value = Random.Range(0, _fighterPrefabs.Length);
                SetPlayerLayer();
            }

            _fighterIndex.OnValueChanged += OnFighterIndexChanged;

            if (!IsOwner && _playerInput)
            {
                _playerInput.enabled = false;
            }
            
            if ((int)ConnectionId >= _spawnPositions.Length)
            {
                Debug.LogError($"SpaceWarPlayer:Multiplayer-No spawn position defined for player {ConnectionId}");
                return;
            }

            var spawnPos = _spawnPositions[(int)ConnectionId];
            var spawnRot = 0f;
            
            if (ConnectionId < (ulong)_spawnRotations.Length)
            {
                spawnRot = _spawnRotations[(int)ConnectionId].z;
            }
            else
            {
                Debug.LogWarning($"SpaceWarPlayer:Multiplayer-No spawn rotation defined for player {ConnectionId}");
            }

            transform.position = spawnPos;
            transform.rotation = Quaternion.Euler(0, 0, spawnRot);

            SpawnFighter(_fighterIndex.Value);
            
            var screenWrapper = GetComponent<ScreenWrapper>();
            if (!screenWrapper)
            {
                gameObject.AddComponent<ScreenWrapper>();
            }
            
            EventBus.Instance.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
            

            Debug.Log($"[{(IsOwner ? "OWNER" : "REMOTE")}] Player {name}:{ConnectionId} spawned at {spawnPos} rotation {spawnRot}° fighter {_fighterIndex.Value}", this);
        }

        void OnFighterIndexChanged(int previousValue, int newValue)
        {
            if (previousValue == newValue || !_fighterVisual) return;
            Destroy(_fighterVisual.gameObject);
            SpawnFighter(newValue);
        }

        void SetPlayerLayer()
        {
            var playerLayer = (ConnectionId == 0) 
                ? LayerMask.NameToLayer("Player 1") 
                : LayerMask.NameToLayer("Player 2");
            
            gameObject.layer = playerLayer;
        }

        void SpawnFighter(int index)
        {
            if (index < 0 || index >= _fighterPrefabs.Length)
            {
                Debug.LogError($"[{(IsOwner ? "OWNER" : "REMOTE")}] Player {name}:{ConnectionId} Invalid fighter index: {index}", this);
                return;
            }

            Debug.Log($"[{(IsOwner ? "OWNER" : "REMOTE")}] Player {name}:{ConnectionId} SpawnFighter({index}) creating fighter prefab", this);
            _fighterVisual = Instantiate(_fighterPrefabs[index], this.transform);
            _fighterVisual.gameObject.layer = gameObject.layer;
            _fighterVisual.EnableVisuals(false);
        }
        
        public override void OnNetworkDespawn()
        {
            _fighterIndex.OnValueChanged -= OnFighterIndexChanged;
            EventBus.Instance?.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
            base.OnNetworkDespawn();
        }
        

        void OnGameStateChanged(GameStateChangedEvent e)
        {
            Debug.Log($"[{(IsOwner ? "OWNER" : "REMOTE")}] Player {name}:{ConnectionId} OnGameStateChanged({e.NewState}) IsServer={IsServer}", this);
            
            switch (e.NewState)
            {
                case GameState.GameStarted:
                case GameState.GameRestarted:
                case GameState.WaitingForPlayers:
                    CancelInvoke(nameof(EnableShipVisuals));
                    ResetToSpawnPosition();
                    _fighterVisual?.EnableVisuals(false);
                    Debug.Log($"[{(IsOwner ? "OWNER" : "REMOTE")}] Player {name}:{ConnectionId} reset and hiding visuals", this);
                    break;
                case GameState.PlayerTurnEnd:
                    CancelInvoke(nameof(EnableShipVisuals));
                    _fighterVisual?.EnableVisuals(false);
                    ResetToSpawnPosition();
                    Debug.Log($"[{(IsOwner ? "OWNER" : "REMOTE")}] Player {name}:{ConnectionId} hiding visuals and resetting", this);
                    break;
                case GameState.PlayerTurnStart:
                    Debug.Log($"[{(IsOwner ? "OWNER" : "REMOTE")}] Player {name}:{ConnectionId} scheduling visuals in 0.5s", this);
                    ResetToSpawnPosition();
                    Invoke(nameof(EnableShipVisuals), 0.5f);
                    break;
            }
        }

        void EnableShipVisuals()
        {
            Debug.Log($"[{(IsOwner ? "OWNER" : "REMOTE")}] Player {name}:{ConnectionId} enabling visuals. _fighterVisual={((_fighterVisual != null) ? "exists" : "NULL")}", this);
            _fighterVisual?.EnableVisuals(true);
        }

        void ResetToSpawnPosition()
        {
            if (_rigidbody)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.angularVelocity = 0f;
            }
            
            transform.position = _spawnPositions[(int)ConnectionId];
            transform.rotation = Quaternion.Euler(0, 0, _spawnRotations[(int)ConnectionId].z);
        }

        public void DestroyTarget()
        {
            if (!IsServer) return;
            Debug.Log($"DestroyTarget called on SpaceWarPlayer {name}", this);
            if (_explosionPrefab)
            {
                var explosion = Instantiate(_explosionPrefab, transform.position, Quaternion.identity);
                if (explosion.TryGetComponent<NetworkObject>(out var networkObject))
                {
                    networkObject.Spawn();
                }
                else
                {
                    Debug.LogWarning("SpaceWarPlayer:DestroyTarget - Explosion prefab is missing NetworkObject component. It will only appear on the server.");
                }
            }
            
            GameManager?.PlayerDied(this);
        }
    }
}
