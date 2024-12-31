using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Game.Shared;

public class PlayerManager : NetworkBehaviour
{
    [SyncVar]
    public int PlayerIndex;

    [SyncVar]
    public string PlayerName;

    public PlayerStatus PlayerStatus = new PlayerStatus();


    private List<int>[] handTilesList;
    private List<Block>[] callBlocksList;
    private List<int>[] EnemyCallBlocksList = new List<int>[3];
    private WinningCondition PlayerWinningCondition = new WinningCondition();
    private List<int> PlayerKawaTiles = new List<int>();
    private List<int>[] EnemyKawaTiles = new List<int>[3];




    private static ServerManager serverManager;

    public override void OnStartServer()
    {
        base.OnStartServer();

        // ServerManager 찾기
        if (serverManager == null)
        {
            serverManager = UnityEngine.Object.FindAnyObjectByType<ServerManager>();

            if (serverManager == null)
            {
                Debug.LogError("ServerManager not found in the scene.");
                return;
            }

            Debug.Log("ServerManager assigned to PlayerManager.");
        }

        // ServerManager에 플레이어 등록
        serverManager.IncrementPlayerCount();
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        if (serverManager != null)
        {
            // ServerManager에서 플레이어 제거
            serverManager.DecrementPlayerCount();
        }
    }



    [Server]
    public void InitializePlayerOnClient(int playerIndex, Wind seatWind, Wind roundWind)
    {
        var networkIdentity = GetComponent<NetworkIdentity>();

        // Debugging NetworkIdentity and Connection
        if (networkIdentity == null)
        {
            Debug.LogError($"PlayerManager[{playerIndex}]: NetworkIdentity is null.");
            return;
        }
        Debug.Log($"PlayerManager[{playerIndex}]: NetworkIdentity found. NetId: {networkIdentity.netId}");

        if (networkIdentity.connectionToClient == null)
        {
            Debug.LogError($"PlayerManager[{playerIndex}]: connectionToClient is null.");
            return;
        }
        Debug.Log($"PlayerManager[{playerIndex}]: ConnectionToClient is valid. ConnectionId: {networkIdentity.connectionToClient.connectionId}");

        // PlayerStatus 생성자 사용
        PlayerStatus = new PlayerStatus(seatWind, roundWind);
        Debug.Log($"PlayerManager[{playerIndex}]: PlayerStatus initialized. SeatWind: {seatWind}, RoundWind: {roundWind}");

        // TargetRpc 호출
        TargetInitializePlayer(networkIdentity.connectionToClient, playerIndex, seatWind, roundWind);
        Debug.Log($"PlayerManager[{playerIndex}]: TargetInitializePlayer called.");
    }

    [TargetRpc]
    public void TargetInitializePlayer(NetworkConnection target, int playerIndex, Wind seatWind, Wind roundWind)
    {
        Debug.Log($"TargetInitializePlayer called for PlayerManager[{playerIndex}]. SeatWind: {seatWind}, RoundWind: {roundWind}");

        PlayerStatus = new PlayerStatus(seatWind, roundWind);
        Debug.Log($"PlayerManager[{playerIndex}]: PlayerStatus set on client.");
    }



    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("In client, player prefab started.");
    }




    public bool CheckIfPlayerTurn()
    {
        return PlayerStatus.IsPlayerTurn;
    }
}
