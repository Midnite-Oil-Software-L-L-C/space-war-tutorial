using System.Collections.Generic;
using MidniteOilSoftware.Core;
using MidniteOilSoftware.Multiplayer.Events;
using Unity.Netcode;
using UnityEngine;

namespace MidniteOilSoftware.Multiplayer.SpaceWar
{
    public class SpaceWarGameManager : GameManager
    {
        [SerializeField] float _gameTimeLimit = 120f;
        
        public List<NetworkPlayer> SpaceWarPlayers => Players;
        public NetworkList<int> Scores { get; } = new();
        public NetworkVariable<float> TimeRemaining { get; } = new NetworkVariable<float>(0f);
        
        Timer _turnStartTimer;

        protected override void Start()
        {
            base.Start();
            if (!IsHost) return;
            _turnStartTimer = TimerManager.Instance.CreateTimer<CountdownTimer>(3f);
            _turnStartTimer.OnTimerStop += OnTurnStartTimerComplete;
        }

        void OnDisable()
        {
            if (!IsHost)
            {
                EventBus.Instance?.Raise<LeftGameEvent>(new LeftGameEvent());
                return;
            }
            _turnStartTimer.OnTimerStop -= OnTurnStartTimerComplete;
        }

        void Update()
        {
            if (!IsServer) return;
            if (!IsPlaying.Value) return;
            if (!(TimeRemaining.Value > 0f)) return;
            TimeRemaining.Value -= Time.deltaTime;
            if (!(TimeRemaining.Value <= 0f)) return;
            TimeRemaining.Value = 0f;
            SetGameState(GameState.GameOver);
        }

        protected override void JoinedGame(NetworkPlayer player)
        {
            base.JoinedGame(player);
            var spaceWarPlayer = player as SpaceWarPlayer;
            if (spaceWarPlayer) return;
            Debug.LogError($"Player {player.name} is not a SpaceWarPlayer!");
        }
        
        protected override void ServerOnlyHandleGameStateChange()
        {
            if (_enableDebugLog)
                Debug.Log(
                    $"SpaceWarGameManager:Multiplayer-ServerOnlyHandleGameStateChange. CurrentState = {CurrentState}");

            switch (CurrentState)
            {
                case GameState.WaitingForPlayers:
                    TimeRemaining.Value = _gameTimeLimit;
                    IsPlaying.Value = false;
                    break;
                case GameState.GameStarted:
                case GameState.GameRestarted:
                    // Initialize all scores to zero
                    Scores.Clear();
                    for (var i = 0; i < Players.Count; i++)
                    {
                        Scores.Add(0);
                    }
                    StartTurnStartCountdown();
                    break;
                case GameState.PlayerTurnStart:
                    IsPlaying.Value = true;
                    break;
                case GameState.PlayerTurnEnd:
                    if (IsGameOver())
                    {
                        IsPlaying.Value = false;
                        SetGameState(GameState.GameOver, 0.25f);
                        return;
                    }
                    StartTurnStartCountdown();
                    break;                
            }
        }

        void StartTurnStartCountdown()
        {
            _turnStartTimer.Start();
        }

        void OnTurnStartTimerComplete()
        {
            SetGameState(GameState.PlayerTurnStart, 0.25f);
        }

        protected override bool IsGameOver()
        {
            return CurrentState == GameState.GameOver;
        }

        [Rpc(SendTo.Server)]
        public void RematchServerRpc()
        {
            SetGameState(GameState.GameRestarted);
        }

        protected override void CleanupSession()
        {
            if (_enableDebugLog)
                Debug.Log("SpaceWarGameManager:Multiplayer-Cleaning up SpaceWarGameManager session");

            base.CleanupSession();
        }

        public void PlayerDied(SpaceWarPlayer spaceWarPlayer)
        {
            // Increment score of the player that didn't die
            var playerIndex = SpaceWarPlayers.IndexOf(spaceWarPlayer);
            if (playerIndex == -1)
            {
                Debug.LogError($"SpaceWarGameManager:Multiplayer-PlayerDied - Could not find player {spaceWarPlayer.name} in SpaceWarPlayers list");
                return;
            }
            var scoringPlayerIndex = (playerIndex + 1) % SpaceWarPlayers.Count;
            Scores[scoringPlayerIndex]++;
            
            // set game state to end the turn
            SetGameState(GameState.PlayerTurnEnd);
        }
    }
}
