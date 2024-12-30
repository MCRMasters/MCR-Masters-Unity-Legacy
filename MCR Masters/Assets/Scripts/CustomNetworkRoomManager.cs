using Mirror;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Security.Cryptography;
using UnityEngine.UI;
using TMPro;

public class CustomNetworkRoomManager : NetworkRoomManager
{
    public int RequiredPlayerCount = 4; // 플레이어 수를 조정 가능한 변수로 설정
    //private ServerManager serverManagerInstance;
    public GameObject serverManagerPrefab;
    private ServerManager serverManager;

    public override void Start()
    {
        base.Start();

        if (Application.isBatchMode) // 서버 실행 (Headless Mode)
        {
            networkAddress = "0.0.0.0"; // 모든 네트워크 인터페이스에서 연결 대기
            StartServer();
            Debug.Log("Server started in headless mode.");
        }
        else // 클라이언트 실행 (HUD 사용)
        {
            Debug.Log("Client mode. Use the HUD to enter IP and connect.");
        }
    }




    public override void OnRoomServerPlayersReady()
    {
        base.OnRoomServerPlayersReady();
        // 모든 플레이어가 준비 상태인지 확인
        if (roomSlots.Count == RequiredPlayerCount && roomSlots.All(player => player.readyToBegin))
        {
            Debug.Log("All players are ready. Starting the game...");
            ServerChangeScene(GameplayScene);
        }
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);
        var roomPlayer = conn.identity.GetComponent<CustomNetworkRoomPlayer>();
        if (roomPlayer != null && roomPlayer.GetComponent<PlayerManager>() == null)
        {
            roomPlayer.gameObject.AddComponent<PlayerManager>();
            Debug.Log("PlayerManager component added to RoomPlayer.");
        }
        Debug.Log($"Player connected: {conn.connectionId}. Total players: {roomSlots.Count}");
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
        Debug.Log($"Player disconnected: {conn.connectionId}. Total players: {roomSlots.Count}");
    }

    public override void OnRoomServerSceneChanged(string sceneName)
    {
        base.OnRoomServerSceneChanged(sceneName);

        if (roomSlots == null)
        {
            Debug.LogWarning("roomSlots is null. Ensure it is initialized properly.");
            return;
        }

        if (sceneName != GameplayScene)
            return;

        // ServerManager 생성
        GameObject serverManagerGameObject = new GameObject("ServerManager");
        
        var serverManagerInstance = Instantiate(serverManagerPrefab);
        NetworkServer.Spawn(serverManagerInstance.gameObject);
        Debug.Log("ServerManager spawned successfully.");
        Debug.Log($"ServerManager prefab: {serverManagerPrefab}");
        Debug.Log($"ServerManager instance: {serverManagerInstance}");
        Debug.Log($"ServerManager component: {serverManagerInstance?.GetComponent<ServerManager>()}");

        if (serverManagerInstance == null)
        {
            Debug.LogError("Failed to initialize ServerManager instance.");
            return;

        }
        serverManager = serverManagerInstance.GetComponent<ServerManager>();
        if (serverManager == null)
        {
            Debug.LogError("ServerManager component is missing on serverManagerInstance.");
        }
        // 플레이어 인덱스와 이름 할당
        AssignPlayerIndicesAndNames();
        if (serverManager != null)
        {
            serverManager.GameStarted();
        }
    }


    private void AssignPlayerIndicesAndNames()
    {
        // ServerManager 인스턴스가 초기화되었는지 확인
        if (serverManager == null)
        {
            Debug.LogError("ServerManager 인스턴스가 null입니다. 플레이어를 할당할 수 없습니다.");
            return;
        }

        // ServerManager의 PlayerManager 배열 초기화
        serverManager.PlayerManagers = new PlayerManager[roomSlots.Count];

        // 플레이어 인덱스 섞기
        var indices = Enumerable.Range(0, roomSlots.Count).ToList();
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            for (int n = indices.Count - 1; n > 0; n--)
            {
                byte[] box = new byte[4];
                rng.GetBytes(box);
                int k = BitConverter.ToInt32(box, 0) & int.MaxValue % (n + 1);
                (indices[n], indices[k]) = (indices[k], indices[n]);
            }
        }

        int i = 0; // 인덱스 할당용 카운터
        foreach (var roomSlot in roomSlots)
        {
            if (roomSlot == null)
            {
                Debug.LogWarning("룸 슬롯 중 하나가 null입니다. 이 슬롯을 건너뜁니다.");
                continue;
            }
            var roomPlayer = roomSlot.GetComponent<CustomNetworkRoomPlayer>();
            if (roomPlayer != null && roomPlayer.netIdentity != null)
            {
                var playerManager = roomPlayer.netIdentity.GetComponent<PlayerManager>();

                if (playerManager == null)
                {
                    Debug.LogWarning("PlayerManager not found on RoomSlot. Adding a new PlayerManager.");
                    playerManager = roomSlot.gameObject.AddComponent<PlayerManager>();
                }
                if (playerManager != null)
                {
                    playerManager.PlayerIndex = indices[i];
                    playerManager.PlayerName = $"Player {indices[i] + 1}";

                    Debug.Log($"PlayerIndex: {playerManager.PlayerIndex}, PlayerName: {playerManager.PlayerName} 할당 완료");

                    // ServerManager 배열에 PlayerManager 할당
                    //serverManagerInstance.CmdInitializePlayerManagers(roomSlot.GetComponent<NetworkIdentity>());

                    serverManager.PlayerManagers[i] = playerManager;

                    // PlayerManager에 ServerManager 연결
                    playerManager.ServerManager = serverManager;

                    Debug.Log($"Connect Player:{playerManager.PlayerName} and ServerManager here" + serverManager.PlayerManagers[i].PlayerName);
                }
                else
                {
                    Debug.LogWarning($"RoomSlot {roomSlot.name}에 PlayerManager 컴포넌트가 없습니다. 새로 추가합니다.");
                    roomSlot.gameObject.AddComponent<PlayerManager>();
                }


            }
            i++;
        }
    }
}
