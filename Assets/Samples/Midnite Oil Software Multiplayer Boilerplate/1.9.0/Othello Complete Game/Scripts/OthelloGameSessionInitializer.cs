using MidniteOilSoftware.Multiplayer;
using MidniteOilSoftware.Multiplayer.Lobby;
using UnityEngine;

namespace MidniteOilSoftware.Core.Othello
{
    public class OthelloGameSessionInitializer : GameSessionInitializer
    {
        public override void InitializeSession()
        {
            base.InitializeSession();
            if (_enableDebugLog)
                Debug.Log("OthelloGameSessionInitializer:Multiplayer-Initializing Othello game session...");
            ProjectSceneManager.Instance.SetupSceneManagementAndLoadGameScene();
        }
    }
}
