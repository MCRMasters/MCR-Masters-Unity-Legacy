using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Game.Shared;
using System.Linq;

public class PlayerManager : NetworkBehaviour
{
    [SyncVar]
    public int PlayerIndex;

    [SyncVar]
    public string PlayerName;

    public PlayerStatus PlayerStatus = new PlayerStatus();


    private List<int> PlayerHandTiles = new();
    private List<Block> PlayerCallBlocksList = new();
    private List<int>[] EnemyCallBlocksList = new List<int>[3];
    private int[] EnemyHandTilesCount = new int[3];
    private WinningCondition PlayerWinningCondition = new WinningCondition();
    private List<int> PlayerKawaTiles = new List<int>();
    private List<int>[] EnemyKawaTiles = new List<int>[3];
    private int[] EnemyIndexMap = new int[3];



    private static ServerManager serverManager;

    public GameObject[] TilePrefabArray;
    public GameObject TileBackPrefab;
    public GameObject EnemyHaipaiToi;
    public GameObject EnemyHaipaiKami;
    public GameObject EnemyHaipaiShimo;
    public GameObject PlayerHaipai;

    public GameObject PlayerKawa;
    public GameObject EnemyKawaToi;
    public GameObject EnemyKawaKami;
    public GameObject EnemyKawaShimo;

    public GameObject PlayerTsumoTile;
    public GameObject EnemyTsumoTile;

    public override void OnStartClient()
    {
        base.OnStartClient();
        // GameObject 초기화
        PlayerHaipai = GameObject.Find("PlayerHaipai");
        EnemyHaipaiToi = GameObject.Find("EnemyHaipaiToi");
        EnemyHaipaiKami = GameObject.Find("EnemyHaipaiKami");
        EnemyHaipaiShimo = GameObject.Find("EnemyHaipaiShimo");

        PlayerKawa = GameObject.Find("PlayerKawa");
        EnemyKawaToi = GameObject.Find("EnemyKawaToi");
        EnemyKawaKami = GameObject.Find("EnemyKawaKami");
        EnemyKawaShimo = GameObject.Find("EnemyKawaShimo");
    }

    public void SetPlayerTurn(bool isTurn)
    {
        PlayerStatus.IsPlayerTurn = isTurn;
        Debug.Log($"Player {PlayerIndex}: IsPlayerTurn set to {isTurn}");
    }

    private void SpawnEnemyFirstHand()
    {
        // 스폰할 타일의 개수
        const int tileCount = 13;

        // 각 상대의 패를 순서대로 스폰
        SpawnTilesForEnemy(EnemyHaipaiToi, tileCount);
        SpawnTilesForEnemy(EnemyHaipaiKami, tileCount);
        SpawnTilesForEnemy(EnemyHaipaiShimo, tileCount);

        Debug.Log("Enemy first hands spawned successfully.");
    }

    // 개별 상대에 타일 스폰 로직
    private void SpawnTilesForEnemy(GameObject enemyHaipai, int tileCount)
    {
        if (enemyHaipai == null)
        {
            Debug.LogWarning("EnemyHaipai is null. Skipping tile spawning.");
            return;
        }

        for (int i = 0; i < tileCount; i++)
        {
            GameObject spawnedTile = Instantiate(TileBackPrefab, enemyHaipai.transform);
            spawnedTile.name = $"TileBack_{i + 1}";
        }

        Debug.Log($"Spawned {tileCount} TileBacks for {enemyHaipai.name}.");
    }


    [TargetRpc]
    public void TargetSpawnFirstHand(NetworkConnection target, List<int> closedTiles)
    {
        // 클라이언트에서 초기 손패를 스폰하거나 UI에 반영
        Debug.Log($"TargetSpawnFirstHand received. ");

        for (int tileId = 0; tileId < closedTiles.Count; tileId++)
        {
            for (int tileCount = 0; tileCount < closedTiles[tileId]; tileCount++)
            {
                // 타일을 스폰하고 UI에 추가하는 로직
                GameObject tilePrefab = TilePrefabArray[tileId];
                if (tilePrefab != null && PlayerHaipai != null)
                {
                    GameObject spawnedTile = Instantiate(tilePrefab, PlayerHaipai.transform);
                    spawnedTile.name = TileDictionary.NumToString[tileId];
                }
            }
        }
        SpawnEnemyFirstHand();
        Debug.Log("First hand spawned successfully on client.");
    }


    [Command]
    public void CmdTsumoTile(int tile)
    {
        if (!PlayerStatus.IsPlayerTurn)
        {
            Debug.LogError("It's not the player's turn.");
            return;
        }

        // Server-side 로직
        Debug.Log($"CmdTsumoTile called by Player {PlayerIndex} for tile {TileDictionary.NumToString[tile]}.");
        RpcTsumoTile(tile);
    }

    [ClientRpc]
    public void RpcTsumoTile(int tile)
    {
        Debug.Log($"RpcTsumoTile received for tile {TileDictionary.NumToString[tile]}.");
        // 클라이언트에서 UI 업데이트나 추가 로직 구현 가능
    }


    [Command]
    public void CmdDiscardTile(GameObject tile)
    {
        if (tile == null) return;
        if (!PlayerStatus.IsPlayerTurn)
        {
            Debug.LogError("It's not the player's turn.");
            return;
        }

        string prefabName = tile.name;
        string prefix = prefabName.Substring(0, 2);

        if (!TileDictionary.StringToNum.TryGetValue(prefix, out int index))
        {
            Debug.LogError($"Invalid tile prefix: {prefix}");
            return;
        }

        int playerIndex = PlayerIndex;
        RpcDiscardTile(index, playerIndex);

        Destroy(tile);
    }

    [ClientRpc]
    public void RpcDiscardTile(int index, int playerIndex)
    {
        if (PlayerTsumoTile == null)
        {
            Debug.LogError("PlayerTsumo is null.");
            return;
        }
        int tileId = TileDictionary.StringToNum[PlayerTsumoTile.name.Substring(0, 2)];

        // tileId로 TilePrefabArray에서 해당 프리팹 가져오기
        if (tileId < 0 || tileId >= TilePrefabArray.Length)
        {
            Debug.LogError($"Invalid tileId {tileId}. It is out of range: {TilePrefabArray.Length}.");
            return;
        }

        GameObject tilePrefab = TilePrefabArray[tileId]; // TileId로 프리팹 참조
        if (tilePrefab == null)
        {
            Debug.LogError($"TilePrefab for tileId {tileId} is null.");
            return;
        }

        GameObject kawaPrefab = null;

        // playerIndex를 기준으로 Player/Enemy 구분
        if (playerIndex == PlayerIndex)
        {
            kawaPrefab = PlayerKawa;
        }
        else
        {
            int relativeIndex = GetRelativeIndex(playerIndex);
            switch (relativeIndex)
            {
                case 0: // Shimo (오른쪽)
                    kawaPrefab = EnemyKawaShimo;
                    break;
                case 1: // Toi (맞은편)
                    kawaPrefab = EnemyKawaToi;
                    break;
                case 2: // Kami (왼쪽)
                    kawaPrefab = EnemyKawaKami;
                    break;
                default:
                    Debug.LogError($"Invalid relative index: {relativeIndex}");
                    return;
            }
        }

        if (kawaPrefab != null)
        {
            // Tile prefab 생성 및 Grid Layout Group에 추가
            GameObject spawnedTile = Instantiate(tilePrefab, kawaPrefab.transform);
            spawnedTile.name = TileDictionary.NumToString[tileId]; // 타일 이름 설정
            spawnedTile.transform.localScale = Vector3.one; // 크기 조정
        }
        else
        {
            Debug.LogError("KawaPrefab is null. Cannot spawn the tile.");
        }
    }



    // 상대적인 인덱스를 계산 (Shimo, Kami, Toi 순서)
    private int GetRelativeIndex(int otherPlayerIndex)
    {
        for (int i = 0; i < EnemyIndexMap.Length; i++)
        {
            if (EnemyIndexMap[i] == otherPlayerIndex)
            {
                return i; // Shimo (0), Kami (1), Toi (2) 순서로 반환
            }
        }
        return -1; // 예상치 못한 경우
    }


    [TargetRpc]
    public void TargetSetEnemyIndexMap(NetworkConnection target, int[] playerIndices, int startIndex)
    {

        // 다음 인덱스를 계산하고 EnemyIndexMap에 저장
        for (int i = 0; i < EnemyIndexMap.Length; i++)
        {
            EnemyIndexMap[i] = playerIndices[(startIndex + i + 1) % playerIndices.Length];
        }

        Debug.Log($"EnemyIndexMap updated for Player position {startIndex}: {string.Join(", ", EnemyIndexMap)}");
    }



    [Server]
    public void InitializeRoundState()
    {
        PlayerHandTiles.Clear();

        PlayerCallBlocksList.Clear();

        for (int i = 0; i < EnemyCallBlocksList.Length; i++)
        {
            if (EnemyCallBlocksList[i] == null)
            {
                EnemyCallBlocksList[i] = new List<int>();
            }
            else
            {
                EnemyCallBlocksList[i].Clear();
            }
        }

        for (int i = 0; i < EnemyHandTilesCount.Length; i++)
        {
            EnemyHandTilesCount[i] = 13;
        }

        PlayerWinningCondition = new WinningCondition();

        PlayerKawaTiles.Clear();

        for (int i = 0; i < EnemyKawaTiles.Length; i++)
        {
            if (EnemyKawaTiles[i] == null)
            {
                EnemyKawaTiles[i] = new List<int>();
            }
            else
            {
                EnemyKawaTiles[i].Clear();
            }
        }
    }


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

        // 모든 플레이어가 등록되었는지 확인
        //if (serverManager.GetActivePlayerCount() == 4)
        //{
        //    Debug.Log("All players are registered. Proceeding to initialize game logic.");
        //    serverManager.InitializePlayers();
        //}
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

    [TargetRpc]
    public void TargetPerformTsumo(NetworkConnection target, int tile)
    {
        // 클라이언트에서 Command 호출
        CmdTsumoTile(tile);
    }



    [Server]
    public void InitializePlayerOnClient(Wind seatWind, Wind roundWind)
    {
        Debug.Log($"InitializePlayerOnClient called for PlayerManager[{PlayerIndex}]. SeatWind: {seatWind}, RoundWind: {roundWind}");

        // PlayerStatus 생성자 사용
        PlayerStatus = new PlayerStatus(seatWind, roundWind);
        Debug.Log($"PlayerManager[{PlayerIndex}]: PlayerStatus initialized. SeatWind: {seatWind}, RoundWind: {roundWind}");

        var networkIdentity = GetComponent<NetworkIdentity>();
        if (networkIdentity == null)
        {
            Debug.LogError($"PlayerManager[{PlayerIndex}]: NetworkIdentity is null.");
            return;
        }

        Debug.Log($"PlayerManager[{PlayerIndex}]: NetworkIdentity found. NetId: {networkIdentity.netId}");

        if (networkIdentity.connectionToClient == null)
        {
            Debug.LogError($"PlayerManager[{PlayerIndex}]: connectionToClient is null.");
            return;
        }

        Debug.Log($"PlayerManager[{PlayerIndex}]: connectionToClient is valid. ConnectionId: {networkIdentity.connectionToClient.connectionId}");

        // TargetRpc 호출
        TargetInitializePlayer(networkIdentity.connectionToClient, seatWind, roundWind);
        Debug.Log($"PlayerManager[{PlayerIndex}]: TargetInitializePlayer called.");
    }


    [TargetRpc]
    public void TargetInitializePlayer(NetworkConnection target, Wind seatWind, Wind roundWind)
    {
        Debug.Log($"TargetInitializePlayer called for PlayerManager[{PlayerIndex}]. SeatWind: {seatWind}, RoundWind: {roundWind}");

        PlayerStatus = new PlayerStatus(seatWind, roundWind);
        Debug.Log($"PlayerManager[{PlayerIndex}]: PlayerStatus set on client.");
    }




    public bool CheckIfPlayerTurn()
    {
        return PlayerStatus.IsPlayerTurn;
    }
}