using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Game.Shared;
using System.Linq;
using Mirror.Examples.MultipleMatch;
using System.Collections;
using System;
using static Unity.Burst.Intrinsics.X86.Avx;
using System.Threading.Tasks;

public class PlayerManager : NetworkBehaviour
{
    [SyncVar]
    public int PlayerIndex;

    [SyncVar]
    public string PlayerName;

    [SyncVar(hook = nameof(OnPlayerStatusChanged))]
    public PlayerStatus playerStatus;

    private void OnPlayerStatusChanged(PlayerStatus oldStatus, PlayerStatus newStatus)
    {
        Debug.Log($"on Player {PlayerIndex}, PlayerStatus changed from {oldStatus} to {newStatus}");
    }

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
    public GameObject[] EnemyHaipaiList;
    public GameObject PlayerHaipai;

    public GameObject PlayerKawa;
    public GameObject EnemyKawaToi;
    public GameObject EnemyKawaKami;
    public GameObject EnemyKawaShimo;
    public GameObject[] EnemyKawaList;

    public GameObject GameStatusUI;


    private int isValidDiscardResponse = -1; // Default is -1
    private bool isWaitingForResponse = false;

    private int tilesLeft;
    private int roundIndex;



    [ClientRpc]
    public void RpcUpdateTilesLeft(int updatedTilesLeft)
    {
        tilesLeft = updatedTilesLeft;
    }

    // 클라이언트에서 RoundIndex와 RoundWind 데이터를 업데이트
    [ClientRpc]
    public void RpcUpdateRoundIndex(int updatedRoundIndex)
    {
        roundIndex = updatedRoundIndex;
    }

    // RoundIndex와 RoundWind 값을 반환하는 메서드
    public int GetRoundIndex()
    {
        return roundIndex;
    }

    public int GetTilesLeft()
    {
        return tilesLeft;
    }


    public IEnumerator CheckVaildDiscardAsync(int tileID, Action<bool> callback)
    {
        if (!isOwned)
        {
            Debug.LogError("[CheckVaildDiscardAsync] This method can only be executed by the owning client.");
            callback?.Invoke(false);
            yield break;
        }

        Debug.Log($"[CheckVaildDiscardAsync] Sending discard check for tileID: {tileID}");

        isWaitingForResponse = true;
        isValidDiscardResponse = -1;

        // 서버에 유효성 검사 요청
        CmdCheckValidDiscard(tileID);
        Debug.Log($"[CheckVaildDiscardAsync] Requested CmdCheckValidDiscard Tile: {TileDictionary.NumToString[tileID]}, isWaitingForResponse: {isWaitingForResponse}, isValidDiscardResponse:  {isValidDiscardResponse}");
        // 응답이 올 때까지 대기
        while (isWaitingForResponse)
        {
            yield return null; // 한 프레임 대기
        }

        Debug.Log($"[CheckVaildDiscardAsync] Received discard validation response: {isValidDiscardResponse}");
        callback?.Invoke(isValidDiscardResponse == 1);
    }


    [Command]
    private void CmdCheckValidDiscard(int tileID)
    {
        if (serverManager == null)
        {
            Debug.LogError("[CmdCheckValidDiscard] ServerManager is not assigned!");
            return;
        }
        // debug code
        // Perform the server-side validation
        int result = serverManager.IsVaildDiscard(tileID, PlayerIndex);

        TargetUpdateDiscardResult(connectionToClient, result);
        Debug.Log($"[CmdCheckValidDiscard] Result of discard validation: {result}");
    }


    [TargetRpc]
    private void TargetUpdateDiscardResult(NetworkConnection target, int result)
    {
        isValidDiscardResponse = result;
        isWaitingForResponse = false;

        Debug.Log($"[TargetUpdateDiscardResult] Received discard validation result: {result}");
    }

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
        EnemyKawaList = new GameObject[3];
        EnemyKawaList[0] = EnemyKawaShimo;
        EnemyKawaList[1] = EnemyKawaToi;
        EnemyKawaList[2] = EnemyKawaKami;
        EnemyHaipaiList = new GameObject[3];
        EnemyHaipaiList[0] = EnemyHaipaiShimo;
        EnemyHaipaiList[1] = EnemyHaipaiToi;
        EnemyHaipaiList[2] = EnemyHaipaiKami;

        GameStatusUI = GameObject.Find("GameStatusUI");
    }

    public void SetPlayerTurn(bool isTurn)
    {
        var newStatus = playerStatus;
        newStatus.IsPlayerTurn = isTurn;
        playerStatus = newStatus;
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
        TileGrid tileGrid = enemyHaipai.GetComponent<TileGrid>();
        if (tileGrid == null)
        {
            Debug.LogError("TileGrid component not found on enemyHaipai!");
            return;
        }
        for (int i = 0; i < tileCount; i++)
        {
            GameObject spawnedTile = Instantiate(TileBackPrefab, enemyHaipai.transform);
            spawnedTile.name = $"TileBack_{i + 1}";
            var spawnedTileEvent = spawnedTile.GetComponent<TileEvent>();
            spawnedTileEvent.SetUndraggable();
            tileGrid.AddTileToLastIndex(spawnedTile);
        }
        //tileGrid.UpdateLayoutByName();
        Debug.Log($"Spawned {tileCount} TileBacks for {enemyHaipai.name}.");
    }


    [TargetRpc]
    public void TargetSpawnFirstHand(NetworkConnection target, List<int> closedTiles)
    {
        // 클라이언트에서 초기 손패를 스폰하거나 UI에 반영
        Debug.Log($"TargetSpawnFirstHand received. ");

        if (PlayerHaipai == null)
        {
            Debug.LogError("PlayerHaipai is null!");
            return;
        }
        else
        {
            Debug.Log($"PlayerHaipai is: {PlayerHaipai.name}");
            var components = PlayerHaipai.GetComponents<Component>();
            foreach (var component in components)
            {
                Debug.Log($"Component: {component.GetType().Name}");
            }

        }


        TileGrid tileGrid = PlayerHaipai.GetComponent<TileGrid>();
        if (tileGrid == null)
        {
            Debug.LogError("TileGrid component not found!");
            return;
        }
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
        
        if (tileGrid)
        {
            tileGrid.UpdateLayoutByName();
        }
        SpawnEnemyFirstHand();
        Debug.Log("First hand spawned successfully on client.");
    }



    [TargetRpc]
    public void TargetTsumoTile(NetworkConnection target, int tile, int playerIndex)
    {
        Debug.Log($"TargetTsumoTile called with tile {tile}.");



        TileGrid tileGrid = PlayerHaipai.GetComponent<TileGrid>();
        if (tileGrid == null) return;
        GameObject spawnedTile = Instantiate(TilePrefabArray[tile], PlayerHaipai.transform);
        if(spawnedTile == null) return;
        spawnedTile.name = TileDictionary.NumToString[tile];
        tileGrid.ShowTsumoTile(spawnedTile);
        Debug.Log($"Displaying Tsumo tile for player {playerIndex}.");
        Debug.Log($"[TargetTsumoTile] Is Player Turn: {playerStatus.IsPlayerTurn}");
        CmdDisplayEnemyTsumoTile(tile, playerIndex);
    }

    [Command]
    public void CmdDisplayEnemyTsumoTile(int tile, int playerIndex)
    {
        // 현재 씬에서 모든 PlayerManager 객체를 찾음
        PlayerManager[] allPlayerManagers = UnityEngine.Object.FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);

        if (allPlayerManagers.Length == 0)
        {
            Debug.LogWarning("No PlayerManager instances found in the scene.");
            return;
        }
        RpcDisplayEnemyTsumoTile(tile, playerIndex);
    }

    public void DisplayEnemyTsumoTile(int tile, int playerIndex)
    {
        if (isServer)
            return;
        int relativeIndex = GetRelativeIndex(playerIndex);
        if (relativeIndex < 0)
        {
            return;
        }
        EnemyHandTilesCount[relativeIndex]++;
        Debug.Log($"[RpcDisplayEnemyTsumoTile] Get relative index {relativeIndex}");
        GameObject haipaiPrefab = null;
        switch (relativeIndex)
        {
            case 0:
                haipaiPrefab = EnemyHaipaiShimo;
                break;
            case 1:
                haipaiPrefab = EnemyHaipaiToi;
                break;
            case 2:
                haipaiPrefab = EnemyHaipaiKami;
                break;
        }
        TileGrid tileGrid = haipaiPrefab.GetComponent<TileGrid>();
        if (tileGrid == null) return;
        var spawnedTile = Instantiate(TileBackPrefab, haipaiPrefab.transform);
        spawnedTile.name = "TileBack_14";
        if (spawnedTile == null) return;
        tileGrid.ShowTsumoTile(spawnedTile);
        return;
    }

    [ClientRpc]
    public void RpcDisplayEnemyTsumoTile(int tile, int playerIndex)
    {
        Debug.Log($"[RpcDisplayEnemyTsumoTile] in function. Player Index here: {PlayerIndex}, Enemy's Index who tsumo: {playerIndex}, tile: {TileDictionary.NumToString[tile]}");
        // 현재 씬에서 모든 PlayerManager 객체를 찾음
        PlayerManager[] allPlayerManagers = UnityEngine.Object.FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);

        if (allPlayerManagers.Length == 0)
        {
            Debug.LogWarning("No PlayerManager instances found in the scene.");
            return;
        }

        Debug.Log($"[RpcDisplayEnemyTsumoTile] Found {allPlayerManagers.Length} PlayerManager instances:");
        foreach (var playerManager in allPlayerManagers)
        {
            if (playerManager == null)
                continue;
            if (playerManager.PlayerIndex == playerIndex)
                continue;
            if (playerManager.isOwned)
            {
                Debug.Log($"[RpcDisplayEnemyTsumoTile] player manager owned, Player Index here: {playerManager.PlayerIndex}, Enemy's Index who tsumo: {playerIndex}");
                playerManager.DisplayEnemyTsumoTile(tile, playerIndex);
                return;
            }
        }
        
    }


    public void DebugAllPlayerManagers()
    {
        // 현재 씬에서 모든 PlayerManager 객체를 찾음
        PlayerManager[] allPlayerManagers = UnityEngine.Object.FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);

        if (allPlayerManagers.Length == 0)
        {
            Debug.LogWarning("No PlayerManager instances found in the scene.");
            return;
        }

        Debug.Log($"Found {allPlayerManagers.Length} PlayerManager instances:");
        foreach (var playerManager in allPlayerManagers)
        {
            // PlayerManager의 정보를 출력
            Debug.Log($"PlayerManager: PlayerIndex = {playerManager.PlayerIndex}, PlayerName = {playerManager.PlayerName}, NetId = {playerManager.GetComponent<NetworkIdentity>()?.netId}");
        }
    }




    [Command]
    public void CmdDiscardTile(string tileName, bool isTsumoTile)
    {
        string prefix = tileName.Substring(0, 2);

        if (!TileDictionary.StringToNum.TryGetValue(prefix, out int tileId))
        {
            Debug.LogError($"Invalid tile prefix: {prefix}");
            return;
        }

        if (PlayerKawa == null)
        {
            Debug.LogError("PlayerKawa is null. Cannot get TileGrid component.");
            return;
        }

        PlayerManager[] allPlayerManagers = UnityEngine.Object.FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);

        if (allPlayerManagers.Length == 0)
        {
            Debug.LogWarning("No PlayerManager instances found in the scene.");
            return;
        }

        Debug.Log($"Found {allPlayerManagers.Length} PlayerManager instances:");
        foreach (var playerManager in allPlayerManagers)
        {
            NetworkConnection networkConnection = playerManager.GetComponent<NetworkIdentity>().connectionToClient;
            Debug.Log($"Assigned PlayerIndex {playerManager.PlayerIndex} to PlayerManager with NetId: {playerManager.GetComponent<NetworkIdentity>().netId}");
            if (networkConnection != null)
            {
                TargetDiscardTile(networkConnection, tileId, PlayerIndex, isTsumoTile);
            }
        }
    }



    public IEnumerator HandleTileDiscardCoroutine(int tileId, int playerIndex, bool IsTsumoTile, Action onComplete)
    {
        // tileId로 TilePrefabArray에서 해당 프리팹 가져오기
        if (tileId < 0 || tileId >= TilePrefabArray.Length)
        {
            Debug.LogError($"Invalid tileId {tileId}. It is out of range: {TilePrefabArray.Length}.");
            onComplete?.Invoke();
            yield break;
        }

        GameObject kawaPrefab = null;
        GameObject haipaiPrefab = null;
        Debug.Log($"Discarded player is {playerIndex}, here player is {PlayerIndex}");
        if (playerIndex == PlayerIndex)
        {
            kawaPrefab = PlayerKawa;
        }
        else
        {
            int relativeIndex = GetRelativeIndex(playerIndex);
            Debug.Log($"Get relative index {relativeIndex}");
            switch (relativeIndex)
            {
                case 0:
                    kawaPrefab = EnemyKawaShimo;
                    haipaiPrefab = EnemyHaipaiShimo;
                    break;
                case 1:
                    kawaPrefab = EnemyKawaToi;
                    haipaiPrefab = EnemyHaipaiToi;
                    break;
                case 2:
                    kawaPrefab = EnemyKawaKami;
                    haipaiPrefab = EnemyHaipaiKami;
                    break;
                default:
                    Debug.LogError($"Invalid relative index: {relativeIndex}");
                    onComplete?.Invoke();
                    yield break;
            }
        }

        if (kawaPrefab == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        if (haipaiPrefab != null)
        {
            TileGrid haipaiTileGrid = haipaiPrefab.GetComponent<TileGrid>();
            if (haipaiTileGrid != null)
            {
                haipaiTileGrid.ShowTedashi(!IsTsumoTile);
            }
        }

        // 타일 생성
        GameObject spawnedTile = Instantiate(TilePrefabArray[tileId], kawaPrefab.transform);
        if (spawnedTile == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        spawnedTile.name = TileDictionary.NumToString[tileId];
        TileEvent tileEvent = spawnedTile.GetComponent<TileEvent>();
        if (tileEvent != null)
        {
            tileEvent.SetUndraggable();
        }

        Debug.Log($"Kawa Name: {kawaPrefab.name}, Components: {string.Join(", ", kawaPrefab.GetComponents<Component>().Select(c => c.GetType().Name))}");

        // TileGrid에 타일 추가
        TileGrid tileGrid = kawaPrefab.GetComponent<TileGrid>();
        if (tileGrid == null)
        {
            Debug.LogError("TileGrid component not found on PlayerKawa.");
            onComplete?.Invoke();
            yield break;
        }

        tileGrid.AddTileToLastIndex(spawnedTile);
        Debug.Log($"Tile {spawnedTile.name} successfully added to {kawaPrefab.name}.");
        onComplete?.Invoke(); // 작업 완료 알림
    }


    private bool IsTargetDiscardTileRunning = false; // TargetDiscardTile 실행 상태를 나타내는 플래그


    [TargetRpc]
    public void TargetDiscardTile(NetworkConnection target, int tileId, int playerIndex, bool IsTsumoTile)
    {
        PlayerManager[] allPlayerManagers = UnityEngine.Object.FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);

        foreach (var playerManager in allPlayerManagers)
        {
            if (playerManager.isOwned)
            {
                Debug.Log($"Executing TargetRpc on owned PlayerManager. PlayerIndex: {playerManager.PlayerIndex}");
                // TargetDiscardTile 시작
                playerManager.IsTargetDiscardTileRunning = true;

                // 완료 여부를 확인하기 위한 플래그
                bool isComplete = false;

                // 코루틴 실행 및 완료 시 플래그 설정
                playerManager.StartCoroutine(playerManager.HandleTileDiscardCoroutine(tileId, playerIndex, IsTsumoTile, () =>
                {
                    Debug.Log($"HandleTileDiscardCoroutine for tileId {tileId} and playerIndex {playerIndex} completed.");
                    isComplete = true;
                    playerManager.IsTargetDiscardTileRunning = false;
                }));

                // 대기 루프
                playerManager.StartCoroutine(WaitUntilComplete(() => isComplete));
                return;
            }
        }
    }

    // 대기 루프를 구현한 코루틴
    private IEnumerator WaitUntilComplete(Func<bool> completionCheck)
    {
        yield return new WaitUntil(completionCheck);
        Debug.Log("TargetDiscardTile operation completed.");
    }



    // 상대적인 인덱스를 계산 (Shimo, Kami, Toi 순서)
    public int GetRelativeIndex(int otherPlayerIndex)
    {
        for (int i = 0; i < EnemyIndexMap.Length; i++)
        {
            if (EnemyIndexMap[i] == otherPlayerIndex)
            {
                return i; // Shimo (0), Toi (1), Kami (2) 순서로 반환
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


    private void emptyGrid(GameObject gameObject)
    {
        TileGrid tileGrid = gameObject.GetComponent<TileGrid>();
        if (tileGrid == null)
        {
            Debug.LogError($"TileGrid component not found on {gameObject.name}.");
            return;
        }
        Debug.Log($"Empty Grid Function on {gameObject.name}");
        tileGrid.EmptyAll();
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

        // GameObject 초기화
        PlayerHaipai = GameObject.Find("PlayerHaipai");
        EnemyHaipaiToi = GameObject.Find("EnemyHaipaiToi");
        EnemyHaipaiKami = GameObject.Find("EnemyHaipaiKami");
        EnemyHaipaiShimo = GameObject.Find("EnemyHaipaiShimo");

        PlayerKawa = GameObject.Find("PlayerKawa");
        EnemyKawaToi = GameObject.Find("EnemyKawaToi");
        EnemyKawaKami = GameObject.Find("EnemyKawaKami");
        EnemyKawaShimo = GameObject.Find("EnemyKawaShimo");
        EnemyKawaList = new GameObject[3];
        EnemyKawaList[0] = EnemyKawaShimo;
        EnemyKawaList[1] = EnemyKawaToi;
        EnemyKawaList[2] = EnemyKawaKami;
        EnemyHaipaiList = new GameObject[3];
        EnemyHaipaiList[0] = EnemyHaipaiShimo;
        EnemyHaipaiList[1] = EnemyHaipaiToi;
        EnemyHaipaiList[2] = EnemyHaipaiKami;

        // ServerManager에 플레이어 등록
        serverManager.IncrementPlayerCount();
    }


    private IEnumerator WaitForTargetDiscardTile()
    {
        while (IsTargetDiscardTileRunning)
        {
            Debug.Log("TargetDiscardTile is still running. Waiting...");
            yield return null; // 다음 프레임까지 대기
        }

        Debug.Log("TargetDiscardTile has completed. Proceeding with emptyGrid...");
        emptyGrid(EnemyKawaToi);
        emptyGrid(PlayerKawa);
        emptyGrid(PlayerHaipai);
        emptyGrid(EnemyHaipaiKami);
        emptyGrid(EnemyHaipaiShimo);
        emptyGrid(EnemyHaipaiToi);
        emptyGrid(EnemyKawaKami);
        emptyGrid(EnemyKawaShimo);
    }


    public void InitializeRoundState()
    {
        var gameStatusUI = GameStatusUI.GetComponent<GameStatusUI>();
        gameStatusUI.IsUpdated = false;
        Debug.Log($"InitializeRoundState, Player Index: {PlayerIndex}, SeatWind: {playerStatus.SeatWind}, RoundWind: {playerStatus.RoundWind}");

        // TargetDiscardTile 실행 중인지 확인
        StartCoroutine(WaitForTargetDiscardTile());


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
        if (GameStatusUI != null)
        {
            if (gameStatusUI != null)
            {
                gameStatusUI.Initialize();
            }
        }
        CmdSetInitializeFlagEnd();
    }

    [Command]
    public void CmdSetInitializeFlagEnd()
    {
        EndFlag_InitializePlayer = true;
    }

    private bool EndFlag_InitializePlayer;
    [Server]
    public void SetInitializeFlagFalse()
    {
        EndFlag_InitializePlayer = false;
    }
    [Server]
    public bool IsInitializationComplete()
    {
        return EndFlag_InitializePlayer;
    }
    [Server]
    public void InitializePlayerOnClient(Wind seatWind, Wind roundWind)
    {
        Debug.Log($"InitializePlayerOnClient called for PlayerManager[{PlayerIndex}]. SeatWind: {seatWind}, RoundWind: {roundWind}");

        // playerStatus 생성자 사용
        playerStatus = new PlayerStatus(playerStatus.CurrentScore, seatWind, roundWind);
        Debug.Log($"PlayerManager[{PlayerIndex}]: PlayerStatus initialized. SeatWind: {seatWind}, RoundWind: {roundWind}, IsPlayerTurn: {playerStatus.IsPlayerTurn}, CurrentScore: {playerStatus.CurrentScore}");

        var networkIdentity = GetComponent<NetworkIdentity>();
        if (networkIdentity == null)
        {
            Debug.LogError($"PlayerManager[{PlayerIndex}]: NetworkIdentity is null.");
            return;
        }

        // TargetRpc 호출
        TargetInitializePlayer(networkIdentity.connectionToClient, seatWind, roundWind);
        Debug.Log($"PlayerManager[{PlayerIndex}]: TargetInitializePlayer called.");
    }


    [TargetRpc]
    public void TargetInitializePlayer(NetworkConnection target, Wind seatWind, Wind roundWind)
    {
        Debug.Log($"TargetInitializePlayer called for PlayerManager[{PlayerIndex}]. SeatWind: {seatWind}, RoundWind: {roundWind}");
        InitializeRoundState();
        Debug.Log($"PlayerManager[{PlayerIndex}]: PlayerStatus set on client.");
    }




    public bool CheckIfPlayerTurn()
    {
        return playerStatus.IsPlayerTurn;
    }
}