using MidniteOilSoftware.Multiplayer.Lobby;
using UnityEngine;

namespace MidniteOilSoftware.Multiplayer.SpaceWar
{
    public class SpaceWarGameSessionInitializer : GameSessionInitializer
    {
        public override void InitializeSession()
        {
            base.InitializeSession();
            if (_enableDebugLog)
                Debug.Log("SpaceWarGameSessionInitializer:Multiplayer-Initializing Space War game session...");
            ProjectSceneManager.Instance.SetupSceneManagementAndLoadGameScene();
        }
    }
}