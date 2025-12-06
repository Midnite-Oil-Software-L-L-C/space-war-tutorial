using MidniteOilSoftware.Core;
using MidniteOilSoftware.Multiplayer.Events;
using Unity.Netcode;
using UnityEngine;

namespace MidniteOilSoftware.Multiplayer.SpaceWar
{
    public class UINetworkSync : NetworkBehaviour
    {
        [SerializeField] GameObject _mainMenuBackground;
        [SerializeField] bool _enableDebugLog = true;

        void Start()
        {
            EventBus.Instance.Subscribe<GameSessionInitializedEvent>(OnGameSessionInitialized);
            EventBus.Instance.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        void OnDisable()
        {
            EventBus.Instance?.Unsubscribe<GameSessionInitializedEvent>(OnGameSessionInitialized);
            EventBus.Instance?.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        void OnGameSessionInitialized(GameSessionInitializedEvent e)
        {
            if (_enableDebugLog)
                Debug.Log("UINetworkSync: GameSessionInitializedEvent received", this);
            
            if (IsHost)
            {
                HideMainMenuBackgroundClientRpc();
            }
        }

        void OnGameStateChanged(GameStateChangedEvent e)
        {
            if (_enableDebugLog)
                Debug.Log($"UINetworkSync: GameStateChangedEvent received - {e.NewState}", this);
            
            if (!IsHost) return;

            switch (e.NewState)
            {
                case GameState.GameStarted:
                case GameState.PlayerTurnStart:
                    HideMainMenuBackgroundClientRpc();
                    break;
            }
        }

        [Rpc(SendTo.Everyone)]
        void HideMainMenuBackgroundClientRpc()
        {
            if (!_mainMenuBackground) return;
            if (_enableDebugLog)
                Debug.Log("UINetworkSync: Hiding main menu background", this);
            _mainMenuBackground.SetActive(false);
        }

        [Rpc(SendTo.Everyone)]
        void ShowMainMenuBackgroundClientRpc()
        {
            if (!_mainMenuBackground) return;
            if (_enableDebugLog)
                Debug.Log("UINetworkSync: Showing main menu background", this);
            _mainMenuBackground.SetActive(true);
        }
    }
}
