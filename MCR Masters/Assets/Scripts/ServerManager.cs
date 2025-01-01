using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using UnityEngine;
using Mirror;
using Game.Shared;
using System.Collections;


public class ServerManager : NetworkBehaviour
{
    private List<int> tileDeck = new();
    private const int MaxPlayers = 4;

    private Hand[] handList = new Hand[MaxPlayers];
    private List<int>[] kawaTilesList = new List<int>[MaxPlayers];

    private const int TotalTiles = 144;
    private int tileDrawIndexLeft = 0;
    private int tileDrawIndexRight = TotalTiles - 1;

    private int activePlayerCount = 0;
    private PlayerManager[] PlayerManagers;
    private int[] playerIndices;


    private int CurrentRound = 0;
    private Wind RoundWind = Wind.EAST;
    private bool gameStarted = false;


    public int GetActivePlayerCount()
    {
        return activePlayerCount;
    }

    void Start()
    {
        // 안전장치: 서버에서만 실행되도록 확인
        if (!NetworkServer.active)
        {
            Debug.LogWarning("ServerManager is not running on the server. Destroying this object.");
            Destroy(gameObject); // 클라이언트에서 생성된 경우 제거
            return;
        }

        Debug.Log("ServerManager starting...");


    }

    private void BroadcastPlayerIndices()
    {
        playerIndices = new int[PlayerManagers.Length];
        for (int i = 0; i < PlayerManagers.Length; i++)
        {
            if (PlayerManagers[i] != null)
            {
                playerIndices[i] = PlayerManagers[i].PlayerIndex;
            }
        }
        // 각 PlayerManager에 RPC 호출

        for (int i = 0; i < PlayerManagers.Length; i++)
        {
            var playerManager = PlayerManagers[i];
            if (playerManager == null)
            {
                Debug.LogWarning("PlayerManager is null. Skipping...");
                continue;
            }

            if (playerManager.connectionToClient == null)
            {
                Debug.LogWarning($"PlayerManager for PlayerIndex {playerManager.PlayerIndex} has a null connectionToClient. Skipping...");
                continue;
            }

            Debug.Log($"PlayerManager for PlayerIndex {playerManager.PlayerIndex} has a valid connectionToClient: ConnectionId {playerManager.connectionToClient.connectionId}");

            // RPC 호출
            playerManager.TargetSetEnemyIndexMap(playerManager.connectionToClient, playerIndices, i);
        }
    }



    bool CanDrawTile()
    {
        return tileDrawIndexLeft <= tileDrawIndexRight;
    }

    private IEnumerator WaitForInitialization()
    {
        yield return new WaitForSeconds(0.5f); // 0.5초 대기 (필요 시 조정)
        InitializePlayers();
    }
    public void IncrementPlayerCount()
    {
        activePlayerCount++;
        Debug.Log($"Player connected. ActivePlayerCount: {activePlayerCount}");

        if (activePlayerCount == MaxPlayers)
        {
            Debug.Log($"All {MaxPlayers} players are active. Initializing players...");
            StartCoroutine(WaitForInitialization());
        }
    }

    public void DecrementPlayerCount()
    {
        activePlayerCount--;
        Debug.Log($"Player disconnected. ActivePlayerCount: {activePlayerCount}");
    }

    public void InitializePlayers()
    {
        Debug.Log("ServerManager initialize players...");
        // 씬에서 PlayerManager를 찾음
        PlayerManagers = UnityEngine.Object.FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);

        // Debug: PlayerManagers 배열 상태 출력
        Debug.Log($"Found {PlayerManagers.Length} PlayerManagers in the scene.");

        // PlayerManagers 배열 검증
        for (int i = 0; i < PlayerManagers.Length; i++)
        {
            if (PlayerManagers[i] == null)
            {
                Debug.LogWarning($"PlayerManagers[{i}] is null.");
            }
            else
            {
                Debug.Log($"PlayerManagers[{i}] - PlayerIndex: {PlayerManagers[i].PlayerIndex}, PlayerName: {PlayerManagers[i].PlayerName}");
            }
        }

        if (PlayerManagers.Length != MaxPlayers)
        {
            Debug.LogError($"Expected {MaxPlayers} PlayerManagers, but found {PlayerManagers.Length}. Game cannot proceed.");
            return;
        }

        AssignPlayerIndicesAndNames();
        GameStarted();
    }

    private void AssignPlayerIndicesAndNames()
    {
        // 플레이어 인덱스를 섞기
        var indices = Enumerable.Range(0, PlayerManagers.Length).ToList();
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

        for (int i = 0; i < PlayerManagers.Length; i++)
        {
            if (PlayerManagers[i] != null)
            {
                PlayerManagers[i].PlayerIndex = indices[i];
                PlayerManagers[i].PlayerName = $"Player {indices[i] + 1}";

                Debug.Log($"PlayerIndex: {PlayerManagers[i].PlayerIndex}, PlayerName: {PlayerManagers[i].PlayerName} 할당 완료");
            }
            else
            {
                Debug.LogWarning($"PlayerManagers[{i}]가 null입니다. 건너뜁니다.");
            }
        }
        // 디버깅용 출력
        for (int i = 0; i < PlayerManagers.Length; i++)
        {
            if (PlayerManagers[i] != null)
            {
                Debug.Log($"PlayerManagers[{i}] - PlayerName: {PlayerManagers[i].PlayerName}, PlayerIndex: {PlayerManagers[i].PlayerIndex}");
            }
        }
    }

    public void GameStarted()
    {
        if (gameStarted)
        {
            Debug.LogWarning("Game already started. Ignoring duplicate call.");
            return;
        }

        gameStarted = true;

        Debug.Log("Game started. Ready for the first round initialization.");
        Debug.Log($"PlayerManagers count: {PlayerManagers?.Length}");

        for (int i = 0; i < PlayerManagers.Length; i++)
        {
            if (PlayerManagers[i] == null)
            {
                Debug.LogError($"PlayerManager at index {i} is null.");
                continue;
            }

            // PlayerManager에게 정보를 전달
            Debug.Log($"Passing initialization data to PlayerManager[{i}].");
            PlayerManagers[i].InitializePlayerOnClient(Wind.EAST + i, RoundWind);
        }
        StartNewRounds();
    }




    public void StartNewRounds()
    {
        Debug.Log("Players first seat:");
        for (int i = 0; i < PlayerManagers.Length; i++)
        {
            Debug.Log($"Index {i}: Player {PlayerManagers[i].PlayerName} - Wind: {PlayerManagers[i].PlayerStatus.SeatWind}");
        }
        for (int i = 0; i < 17; ++i)
        {
            StartNewRound();
            // test debug code for fisrt round
            return;
        }
    }



    public void StartNewRound()
    {
        if (CurrentRound >= 16)
        {
            Debug.Log("Game over. All rounds completed.");
            return;
        }
        CurrentRound++;
        if (CurrentRound > 1 && CurrentRound % 4 == 1)
        {
            RoundWind++;
        }
        if (CurrentRound > 1)
        {
            AdjustPositionsBeforeRound();
        }
        BroadcastPlayerIndices();

        Debug.Log($"New round started: Round {CurrentRound}, Wind: {RoundWind}");
        InitializeTiles();
        ShuffleTiles();
        DealTilesToPlayers();
        int currentPlayerIndex = 0;

        // test debug code for fisrt round
        return;

        while (CanDrawTile())
        {
            // 타일 뽑기
            var drawnTiles = DrawTiles(1);
            if (drawnTiles == null || drawnTiles.Count == 0)
            {
                Debug.LogWarning($"Player {currentPlayerIndex}: No more tiles to draw.");
                break;
            }

            // TsumoOneTile 호출
            int tile = drawnTiles[0];
            int tsumoResult = handList[currentPlayerIndex].TsumoOneTile(tile);
            if (tsumoResult != 0)
            {
                Debug.LogError($"Player {currentPlayerIndex}: Failed to tsumo tile {tile}.");
                return;
            }

            // Tsumo 호출 (TargetPerformTsumo 사용)
            PlayerManagers[currentPlayerIndex].TargetPerformTsumo(PlayerManagers[currentPlayerIndex].connectionToClient, tile);


            Debug.Log($"Player {currentPlayerIndex} tsumoed tile: {TileDictionary.NumToString[tile]}.");

            // WinningTile을 참조하여 타일 버리기
            int winningTile = handList[currentPlayerIndex].WinningTile;


            if (winningTile == -1)
            {
                Debug.LogError($"Player {currentPlayerIndex}: No WinningTile to discard.");
                return;
            }


            int discardResult = handList[currentPlayerIndex].DiscardOneTile(winningTile);
            if (discardResult != 0)
            {
                Debug.LogError($"Player {currentPlayerIndex}: Failed to discard tile {winningTile}.");
                currentPlayerIndex = (currentPlayerIndex + 1) % 4; // 다음 플레이어로 이동
                return;
            }

            // 버려진 타일을 kawaTilesList에 추가
            kawaTilesList[currentPlayerIndex].Add(winningTile);
            Debug.Log($"Player {currentPlayerIndex} discarded tile: {TileDictionary.NumToString[winningTile]}.");

            PlayerManagers[currentPlayerIndex].SetPlayerTurn(false);
            // 다음 플레이어로 이동
            currentPlayerIndex = (currentPlayerIndex + 1) % 4;
            PlayerManagers[currentPlayerIndex].SetPlayerTurn(true);
        }
    }


    private void AdjustPositionsBeforeRound()
    {
        int[] changeIndex = new int[PlayerManagers.Length];
        PlayerManager[] tempManagers = new PlayerManager[PlayerManagers.Length];
        if (CurrentRound % 4 == 1)
        {
            if (RoundWind == Wind.SOUTH || RoundWind == Wind.NORTH)
            {
                changeIndex[0] = 2;
                changeIndex[1] = 1;
                changeIndex[2] = 0;
                changeIndex[3] = 3;
            }
            else if (RoundWind == Wind.WEST)
            {
                changeIndex[0] = 0;
                changeIndex[1] = 3;
                changeIndex[2] = 1;
                changeIndex[3] = 2;
            }
        }
        else
        {
            changeIndex[0] = 1;
            changeIndex[1] = 2;
            changeIndex[2] = 3;
            changeIndex[3] = 0;
        }


        for (int i = 0; i < PlayerManagers.Length; i++)
        {
            tempManagers[i] = PlayerManagers[changeIndex[i]];
        }

        for (int i = 0; i < PlayerManagers.Length; i++)
        {
            PlayerManagers[i] = tempManagers[i];
        }
        for (int i = 0; i < PlayerManagers.Length; i++)
        {
            PlayerManagers[i].PlayerStatus.SeatWind = i + Wind.EAST;
        }

        Debug.Log("Player positions adjusted after round and reassigned.");
        for (int i = 0; i < PlayerManagers.Length; i++)
        {
            Debug.Log($"Index {i}: Player {PlayerManagers[i].PlayerName} - Wind: {PlayerManagers[i].PlayerStatus.SeatWind}");
        }
    }

    private void InitializeTiles()
    {
        tileDeck.Clear();
        handList = new Hand[MaxPlayers];
        kawaTilesList = new List<int>[MaxPlayers];
        tileDrawIndexLeft = 0;
        tileDrawIndexRight = TotalTiles - 1;
        for (int i = 0; i < MaxPlayers; i++)
        {
            handList[i] = new Hand();
            kawaTilesList[i] = new();
        }
        for (int tileNum = 0; tileNum < 34; tileNum++)
        {
            for (int i = 0; i < 4; i++)
            {
                tileDeck.Add(tileNum);
            }
        }

        for (int i = 0; i < 8; i++)
        {
            tileDeck.Add(34); // 0f tiles
        }

        Debug.Log("Tile deck initialized with " + tileDeck.Count + " tiles.");
    }

    private void ShuffleTiles()
    {
        using (var rng = RandomNumberGenerator.Create())
        {
            int n = tileDeck.Count;
            while (n > 1)
            {
                byte[] box = new byte[4];
                rng.GetBytes(box);
                int k = (int)(BitConverter.ToUInt32(box, 0) % n);
                n--;
                int temp = tileDeck[n];
                tileDeck[n] = tileDeck[k];
                tileDeck[k] = temp;
            }
        }

        Debug.Log("Tiles shuffled.");
    }


    public List<int> DrawTiles(int count)
    {
        if (tileDrawIndexRight - tileDrawIndexLeft + 1 < count)
        {
            Debug.LogWarning("Not enough tiles left in the deck.");
            return null;
        }

        var drawnTiles = tileDeck.GetRange(tileDrawIndexLeft, count);
        tileDrawIndexLeft += count;
        return drawnTiles;
    }


    public void PlayerDiscardTile(PlayerManager playerManager, int tile)
    {

    }


    private void DealTilesToPlayers()
    {
        for (int i = 0; i < MaxPlayers; ++i)
        {
            var handTiles = DrawTiles(13);
            if (handTiles == null)
            {
                Debug.LogError("Not enough tiles to deal to player " + PlayerManagers[i].PlayerName);
                return;
            }
            handTiles.Sort();
            handList[i].DrawFirstHand(handTiles);
            Debug.Log("Dealt starting hand to player " + PlayerManagers[i].PlayerName);
        }

        SpawnFirstHands();
    }

    private void SpawnFirstHands()
    {
        Debug.Log($"{PlayerManagers.Length} Players in SpawnFirstHands function.");
        for (int i = 0; i < MaxPlayers; ++i)
        {
            if (PlayerManagers[i] == null)
            {
                Debug.LogWarning($"PlayerManager at index {i} is null. Skipping.");
                continue;
            }

            Debug.Log($"PlayerManager at index {i}, Player Name: {PlayerManagers[i].PlayerName}, Player Index: {PlayerManagers[i].PlayerIndex}, try to Spawn First hand...");
            // 클라이언트에게 타겟팅된 데이터 전송
            PlayerManagers[i].TargetSpawnFirstHand(PlayerManagers[i].connectionToClient, handList[i].ClosedTiles);
        }

        Debug.Log("SpawnFirstHands called with TargetRpc on all clients.");
    }
}