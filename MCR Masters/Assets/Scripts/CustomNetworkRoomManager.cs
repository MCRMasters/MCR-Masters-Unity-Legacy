using Mirror;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Security.Cryptography;

public class CustomNetworkRoomManager : NetworkRoomManager
{
    public int RequiredPlayerCount = 4; // 플레이어 수를 조정 가능한 변수로 설정
    private ServerManager serverManagerInstance;
    public override void OnRoomServerPlayersReady()
    {
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
        if (roomSlots == null)
        {
            Debug.LogWarning("roomSlots is null. Ensure it is initialized properly.");
            return;
        }

        // ServerManager 생성
        var serverManagerGameObject = new GameObject("ServerManager");
        serverManagerInstance = serverManagerGameObject.AddComponent<ServerManager>();

        // 플레이어 인덱스와 이름 할당
        AssignPlayerIndicesAndNames(serverManagerGameObject);
    }

    private void AssignPlayerIndicesAndNames(GameObject serverManagerGameObject)
    {
        // 플레이어 인덱스를 랜덤으로 섞기
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

        int i = 0; // 인덱스 할당용
        foreach (var roomSlot in roomSlots)
        {
            if (roomSlot == null)
            {
                Debug.LogWarning("A room slot is null. Skipping this slot.");
                continue;
            }

            var roomPlayer = roomSlot.GetComponent<PlayerManager>();
            if (roomPlayer != null)
            {
                roomPlayer.PlayerIndex = indices[i];
                roomPlayer.PlayerName = $"Player {indices[i] + 1}";
                Debug.Log($"Assigned PlayerIndex: {roomPlayer.PlayerIndex}, PlayerName: {roomPlayer.PlayerName}");
                i++;
                Debug.Log($"Connect Player:{roomPlayer.PlayerName} and ServerManager here");
            }
            else
            {
                Debug.LogWarning($"RoomSlot {roomSlot.name} does not have a PlayerManager component. Adding one now.");
                roomSlot.gameObject.AddComponent<PlayerManager>();
            }
        }
    }
}
