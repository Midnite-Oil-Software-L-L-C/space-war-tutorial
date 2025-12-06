using MidniteOilSoftware.Multiplayer.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EventBus = MidniteOilSoftware.Core.EventBus;

namespace MidniteOilSoftware.Multiplayer.SpaceWar
{
    public class SpaceWarGameUI : MonoBehaviour
    {
        [SerializeField] TMP_Text[] _playerNames, _playerScores;
        [SerializeField] TMP_Text _getReadyText, _winnerText, _timeRemainingText;
        [SerializeField] GameObject _gameOverPanel;
        [SerializeField] Button _rematchButton, _exitButton;

        SpaceWarGameManager _gameManager;
        SpaceWarGameManager GameManager
        {
            get
            {
                if (!_gameManager)
                {
                    _gameManager = FindFirstObjectByType<SpaceWarGameManager>(FindObjectsInactive.Include);
                    if (!_gameManager)
                    {
                        Debug.LogError("SpaceWarGameUI:Multiplayer-Could not find SpaceWarGameManager in scene!", this);
                    }
                }

                return _gameManager;
            }
        }

        void Start()
        {
            _getReadyText.gameObject.SetActive(true);
            _gameManager = FindFirstObjectByType<SpaceWarGameManager>(FindObjectsInactive.Include);
            _gameOverPanel.SetActive(false);
            SubscribeToEvents();
            AddButtonListeners();
        }

        void OnDisable()
        {
            Debug.Log("SpaceWarGameUI.OnDisable()", this);
            UnsubscribeFromEvents();
            RemoveButtonListeners();
        }

        void LateUpdate()
        {
            if (!GameManager || GameManager.SpaceWarPlayers == null || GameManager.SpaceWarPlayers.Count == 0)
                return;

            for (var i = 0; i < _gameManager.SpaceWarPlayers.Count; i++)
            {
                if (i >= _playerNames.Length) continue;
                
                var player = GameManager.SpaceWarPlayers[i] as SpaceWarPlayer;
                _playerNames[i].text = player?.PlayerName.Value.ToString();
            }

            if (GameManager.TimeRemaining != null)
            {
                var timeRemaining = Mathf.Max(0f, GameManager.TimeRemaining.Value);
                var minutes = Mathf.FloorToInt(timeRemaining / 60f);
                var seconds = Mathf.FloorToInt(timeRemaining % 60f);
                _timeRemainingText.text = $"{minutes:00}:{seconds:00}";
            }
            
            if (GameManager.Scores == null || GameManager.Scores.Count == 0)
                return;

            for (var i = 0; i < _gameManager.SpaceWarPlayers.Count; i++)
            {
                if (i >= _playerScores.Length) continue;
                if (i >= GameManager.Scores.Count) continue;
                
                var score = GameManager.Scores[i];
                _playerScores[i].text = score.ToString();
            }
        }

        void SubscribeToEvents()
        {
            EventBus.Instance.Subscribe<GameStateChangedEvent>(GameStateChangedEventHandler);
        }

        void UnsubscribeFromEvents()
        {
            EventBus.Instance?.Unsubscribe<GameStateChangedEvent>(GameStateChangedEventHandler);
        }

        void AddButtonListeners()
        {
            RemoveButtonListeners();
            _rematchButton.onClick.AddListener(_gameManager.RematchServerRpc);
            _exitButton.onClick.AddListener(ExitGame);
        }

        void RemoveButtonListeners()
        {
            _rematchButton.onClick.RemoveAllListeners();
            _exitButton.onClick.RemoveAllListeners();
        }
        
        void ExitGame()
        {
            Debug.Log("SpaceWarGameUIExitGame()", this);
            GameManager.ExitGameServerRpc();
        }

        void GameStateChangedEventHandler(GameStateChangedEvent e)
        {
            Debug.Log($"SpaceWarGameUI.GameStateChangedEventHandler: {e.NewState}", this);
            
            switch (e.NewState)
            {
                case GameState.GameStarted:
                case GameState.GameRestarted:
                    _gameOverPanel.gameObject.SetActive(false);
                    _getReadyText.gameObject.SetActive(true);
                    Debug.Log("SpaceWarGameUI: Showing Get Ready text", this);
                    break;
                case GameState.PlayerTurnEnd:
                    _gameOverPanel.gameObject.SetActive(false);
                    _getReadyText.gameObject.SetActive(true);
                    Debug.Log("SpaceWarGameUI: Showing Get Ready text after turn end", this);
                    break;
                case GameState.PlayerTurnStart:
                    _getReadyText.gameObject.SetActive(false);
                    Debug.Log("SpaceWarGameUI: Hiding Get Ready text - Game started!", this);
                    break;
                case GameState.GameOver:
                    ShowGameOverPanel();
                    break;
            }
        }

        void ShowGameOverPanel()
        {
            _gameOverPanel.SetActive(true);
            // todo determine winner and display winner's name
            _winnerText.text = string.Empty;
        }
    }
}
