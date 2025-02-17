using Mirror;
using System.Linq;
using UnityEngine;

public class CustomNetworkRoomManager : NetworkRoomManager
{
    public int RequiredPlayerCount = 4; // 플레이어 수를 조정 가능한 변수로 설정
    CustomNetworkManagerHUD hud;


    public override void Awake()
    {
        hud = GetComponent<CustomNetworkManagerHUD>();
    }

    public override void Start()
    {
        base.Start();

        if (Application.isBatchMode) // 서버 실행 (Headless Mode)
        {
            networkAddress = "0.0.0.0"; // 모든 네트워크 인터페이스에서 연결 대기
            StartServer();
            Debug.Log("Server started in headless mode.");
        }
        else // 클라이언트 실행
        {
            networkAddress = "192.168.115.189";
            Debug.Log("Client mode. Use the HUD to enter IP and connect.");
        }
    }
    /*
    public override void OnRoomServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnRoomServerAddPlayer(conn);

        // CustomNetworkRoomPlayer에서 이름을 받아오기
        var roomPlayer = conn.identity.GetComponent<CustomNetworkRoomPlayer>();

        if (roomPlayer != null)
        {
            // ✅ 플레이어 프리팹을 인스턴스화
            GameObject playerInstance = Instantiate(playerPrefab);

            // ✅ 인스턴스에서 PlayerManager 컴포넌트 가져오기
            var playerManager = playerInstance.GetComponent<PlayerManager>();

            if (playerManager != null)
            {
                // ✅ 입력받은 플레이어 이름을 인스턴스에 할당
                playerManager.PlayerName = roomPlayer.PlayerName;
                Debug.Log($"Assigned PlayerName: {roomPlayer.PlayerName} to PlayerManager.");
            }

            // ✅ 서버에 인스턴스 추가
            NetworkServer.AddPlayerForConnection(conn, playerInstance);
        }
    }
    */


    public override void OnRoomServerPlayersReady()
    {
        base.OnRoomServerPlayersReady();
        if (roomSlots.Count == RequiredPlayerCount && roomSlots.All(player => player.readyToBegin))
        {
            Debug.Log("All players are ready.");
        }
    }

    public override void OnRoomServerSceneChanged(string sceneName)
    {
        if (sceneName == GameplayScene)
        {
            GameObject serverManagerObj = new GameObject("ServerManager");
            ServerManager serverManager = serverManagerObj.AddComponent<ServerManager>();

            Debug.Log("ServerManager created and configured for server-only execution.");
        }

        base.OnRoomServerSceneChanged(sceneName);
        Debug.Log($"Scene Changed to {sceneName}.");
    }
}
