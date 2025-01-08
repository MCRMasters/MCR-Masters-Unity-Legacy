using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using Game.Shared;
using System.Linq;
using Mirror.Examples.MultipleMatch;
using System.Collections;
using System;
using static Unity.Burst.Intrinsics.X86.Avx;
using System.Threading.Tasks;
using DataTransfer;
using Mirror.BouncyCastle.Utilities.Collections;
using TMPro;
using Unity.VisualScripting;
using System.Xml;
using Mirror.BouncyCastle.Security.Certificates;

public class PlayerManager : NetworkBehaviour
{
    [SyncVar]
    public int PlayerIndex;

    [SyncVar]
    public string PlayerName;

    [SyncVar(hook = nameof(OnPlayerStatusChanged))]
    public PlayerStatus playerStatus;



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

    public GameObject huButtonPrefab;
    public GameObject skipButtonPrefab;
    public GameObject flowerButtonPrefab;
    public GameObject chiiButtonPrefab;
    public GameObject ponButtonPrefab;
    public GameObject kanButtonPrefab;
    public GameObject popupPrefab;

    private bool IsPopupConfirmed = false;
    private GameObject popupObject;
    private GameObject huButton;
    private GameObject skipButton;
    private GameObject flowerButton;
    private GameObject chiiButton;
    private GameObject ponButton;
    private GameObject kanButton;
    private List<GameObject> allButtons;


    private PlayerManager GetOwnedPlayerManager()
    {
        PlayerManager[] allPlayerManagers = UnityEngine.Object.FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);

        if (allPlayerManagers.Length == 0)
        {
            Debug.LogWarning("No PlayerManager instances found in the scene.");
            return null;
        }

        Debug.Log($"[RpcDisplayEnemyTsumoTile] Found {allPlayerManagers.Length} PlayerManager instances:");
        foreach (var playerManager in allPlayerManagers)
        {
            if (playerManager.isOwned)
            {
                return playerManager; ;
            }
        }
        return null;
    }

    [TargetRpc]
    public void TargetClearButtons(NetworkConnection conn)
    {
        PlayerManager playerManager = GetOwnedPlayerManager();
        if (playerManager == null)
        {
            return;
        }
        DeleteButtons();
        DestroyAdditionalChoices();
    }

    [TargetRpc]
    public void TargetClearButtonsAndDoCallAction(NetworkConnection conn, ActionPriorityInfo action, int sourceTileId, int playerIndex)
    {
        PlayerManager playerManager = GetOwnedPlayerManager();
        if (playerManager == null)
        {
            return;
        }
        if (playerManager.PlayerIndex != playerIndex)
        {
            DeleteButtons();
            DestroyAdditionalChoices();
            return;
        }
        ExecuteAction(action, sourceTileId, playerIndex);
    }

    private void PerformKan(ActionPriorityInfo action, int sourceTileId, int playerIndex)
    {

    }

    private void PerformPon(ActionPriorityInfo action, int sourceTileId, int playerIndex)
    {

    }

    private void PerformChii(ActionPriorityInfo action, int sourceTileId, int playerIndex)
    {

    }

    private void PerformFlower(ActionPriorityInfo action, int sourceTileId, int playerIndex)
    {

    }

    private void ExecuteAction(ActionPriorityInfo action, int sourceTileId, int playerIndex)
    {
        switch (action.Type)
        {
            case ActionType.KAN:
                PerformKan(action, sourceTileId, playerIndex);
                break;
            case ActionType.PON:
                PerformPon(action, sourceTileId, playerIndex);
                break;
            case ActionType.CHII:
                PerformChii(action, sourceTileId, playerIndex);
                break;
            case ActionType.FLOWER:
                PerformFlower(action, sourceTileId, playerIndex);
                break;
        }
    }



    [TargetRpc]
    public void TargetShowActionButtons(NetworkConnection conn, int playerWindIndex, List<ActionPriorityInfo> actions, int actionTurnId, int tileId)
    {
        StartCoroutine(MakeButtonsAndHandlePlayerDecision(playerWindIndex, actions, actionTurnId, tileId));

    }

    private void InitializeButtonList()
    {
        allButtons = new List<GameObject> { huButton, skipButton, flowerButton, chiiButton, ponButton, kanButton };
    }

    public void DisableButtons()
    {
        foreach (var button in allButtons)
        {
            if (button != null)
            {
                button.SetActive(false);
            }
        }

        Debug.Log("[DisableButtons] All active buttons have been disabled.");
    }

    public void EnableButtons()
    {
        foreach (var button in allButtons)
        {
            if (button != null)
            {
                button.SetActive(true);
            }
        }

        Debug.Log("[EnableButtons] All valid buttons have been enabled.");
    }

    public void DeleteButtons()
    {
        huButton = DeleteButton(huButton);
        skipButton = DeleteButton(skipButton);
        flowerButton = DeleteButton(flowerButton);
        chiiButton = DeleteButton(chiiButton);
        ponButton = DeleteButton(ponButton);
        kanButton = DeleteButton(kanButton);
    }

    private GameObject DeleteButton(GameObject button)
    {
        if (button != null)
        {
            Destroy(button);
            button = null;
        }
        return button;
    }



    [TargetRpc]
    public void TargetShowRoundScore(NetworkConnection target, int playerIndex, List<YakuScoreData> huYakuScoreArray, int totalScore)
    {
        // Main Canvas 찾기
        GameObject mainCanvas = GameObject.Find("Main Canvas");
        if (mainCanvas == null)
        {
            Debug.LogError("[TargetShowRoundScore] Main Canvas not found.");
            return;
        }

        // 팝업 UI 생성
        popupObject = Instantiate(popupPrefab, mainCanvas.transform); // Main Canvas에 추가
        RectTransform popupRect = popupObject.GetComponent<RectTransform>();

        TMP_Text scoreText = popupObject.transform.Find("Canvas/ScoreListText").GetComponent<TMP_Text>();
        TMP_Text totalScoreText = popupObject.transform.Find("Canvas/TotalScoreText").GetComponent<TMP_Text>();

        if (scoreText == null || totalScoreText == null)
        {
            Debug.LogError("[TargetShowRoundScore] ScoreListText or TotalScoreText not found in popup prefab.");
            return;
        }

        // 점수 데이터 표시
        int maxRows = huYakuScoreArray.Count >= 8 ? 8 : 5;
        int totalColumns = Mathf.CeilToInt((float)huYakuScoreArray.Count / maxRows);
        string[,] table = new string[maxRows, totalColumns];

        for (int i = 0; i < huYakuScoreArray.Count; i++)
        {
            int row = i % maxRows;
            int column = i / maxRows;
            table[row, column] = $"{YakuDictionary.dict[huYakuScoreArray[i].YakuId]} : {huYakuScoreArray[i].Score}";
        }

        string scoreDisplay = "";
        for (int row = 0; row < maxRows; row++)
        {
            for (int column = 0; column < totalColumns; column++)
            {
                if (!string.IsNullOrEmpty(table[row, column]))
                {
                    scoreDisplay += table[row, column].PadRight(40);
                }
            }
            scoreDisplay += "\n";
        }

        scoreText.text = scoreDisplay;
        scoreText.fontSize = huYakuScoreArray.Count >= 8 ? 24 : (huYakuScoreArray.Count >= 5 ? 30 : 40);
        totalScoreText.text = $"{totalScore} Points";

        // 총 점수 표시 및 색상 설정
        totalScoreText.text = $"{totalScore} Points";
        if (totalScore < 16)
        {
            totalScoreText.color = Color.black;
        }
        else if (totalScore < 32)
        {
            totalScoreText.color = Color.blue;
        }
        else if (totalScore < 48)
        {
            totalScoreText.color = Color.red;
        }
        else
        {
            totalScoreText.color = new Color(1.0f, 0.84f, 0.0f); // Gold 색상
        }

        // Material 색상 덮어쓰기
        Material material = totalScoreText.fontMaterial;
        material.SetColor(ShaderUtilities.ID_FaceColor, totalScoreText.color);
        totalScoreText.fontMaterial = material;
        // 확인 버튼 처리
        Button confirmButton = popupObject.transform.Find("Canvas/ConfirmButton").GetComponent<Button>();
        if (confirmButton == null)
        {
            Debug.LogError("[TargetShowRoundScore] ConfirmButton not found in popup prefab.");
            return;
        }

        IsPopupConfirmed = false;
        confirmButton.onClick.AddListener(() =>
        {
            IsPopupConfirmed = true;
            //Destroy(popup); // 팝업 제거
        });

        // 10초 대기 또는 확인 버튼 클릭 대기
        StartCoroutine(WaitForPopupConfirmation(10f));
    }

    [TargetRpc]
    public void TargetDeletePopup(NetworkConnection conn)
    {
        if (!!popupObject)
        {
            Destroy(popupObject);
            popupObject = null;
        }
    }

    private IEnumerator WaitForPopupConfirmation(float popupWaitTime)
    {
        float elapsedTime = 0f;
        while (elapsedTime < popupWaitTime && !IsPopupConfirmed)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        //if (!IsPopupConfirmed)
        //{
            //Destroy(popup); // 팝업 제거
        //}

        CmdNotifyServerPopupComplete(); // 서버에 팝업 완료 알림
    }

    [Command]
    private void CmdNotifyServerPopupComplete()
    {
        if (serverManager == null)
        {
            Debug.LogError("[CmdNotifyServerPopupComplete] ServerManager component not found.");
            return;
        }

        serverManager.OnClientPopupComplete();
    }


    private void OnPlayerStatusChanged(PlayerStatus oldStatus, PlayerStatus newStatus)
    {
        Debug.Log($"on Player {PlayerIndex}, PlayerStatus changed from {oldStatus} to {newStatus}");
    }

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
        DeleteButtons();
        InitializeButtonList();
    }


    [Server]
    public void UpdateCurrentScore(int newScore)
    {
        var newStatus = playerStatus;
        newStatus.CurrentScore = newScore;
        playerStatus = newStatus;

        Debug.Log($"Player {PlayerIndex}: score updated to {playerStatus.CurrentScore}");
    }

    [Server]
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
        if (spawnedTile == null) return;
        spawnedTile.name = "TileBack_14";
        TileEvent tileEvent = spawnedTile.GetComponent<TileEvent>();
        if (tileEvent == null) return;
        tileEvent.SetUndraggable();
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

    private GameObject CreateButton(GameObject prefab, Transform parent, string buttonType)
    {
        if (prefab == null)
        {
            Debug.LogError($"[CreateButton] Prefab for {buttonType} is null.");
            return null;
        }

        GameObject button = Instantiate(prefab, parent);

        // 전역 변수에 버튼 할당
        switch (buttonType.ToUpper())
        {
            case "HU":
                huButton = button;
                break;
            case "SKIP":
                skipButton = button;
                break;
            case "FLOWER":
                flowerButton = button;
                break;
            case "CHII":
                chiiButton = button;
                break;
            case "PON":
                ponButton = button;
                break;
            case "KAN":
                kanButton = button;
                break;
            default:
                Debug.LogWarning($"[CreateButton] Unknown button type: {buttonType}");
                break;
        }

        return button;
    }


    [Command]
    public void CmdReturnActionDecision(int playerWindIndex, ActionPriorityInfo actionPriorityInfo, int actionTurnId, int tileId)
    {
        serverManager.ReceiveActionDecision(playerWindIndex, actionPriorityInfo, actionTurnId, tileId);
    }

    private bool decisionMade = false; // 전역 변수로 이동하여 버튼 클릭 상태를 추적

    private IEnumerator MakeButtonsAndHandlePlayerDecision(int playerIndex, List<ActionPriorityInfo> actions, int actionTurnId, int tileId)
    {
        Debug.Log("[MakeButtonsAndHandlePlayerDecision] Starting decision coroutine.");

        decisionMade = false;
        if (actions.Count == 0)
        {
            yield break;
        }
        int priority = actions[0].Priority;
        // `Main Canvas` 찾기
        Canvas mainCanvas = GameObject.Find("Main Canvas").GetComponent<Canvas>();
        if (mainCanvas == null)
        {
            Debug.LogError("[MakeButtonsAndHandlePlayerDecision] Main Canvas not found.");
            CmdReturnActionDecision(playerIndex, new ActionPriorityInfo(ActionType.SKIP, priority, -1), actionTurnId, tileId);
            yield break;
        }

        // `PlayerHaipai`의 RectTransform 가져오기
        RectTransform playerHaipaiRect = PlayerHaipai.GetComponent<RectTransform>();
        if (playerHaipaiRect == null)
        {
            Debug.LogError("[MakeButtonsAndHandlePlayerDecision] PlayerHaipai RectTransform not found.");
            CmdReturnActionDecision(playerIndex, new ActionPriorityInfo(ActionType.SKIP, priority, -1), actionTurnId, tileId);
            yield break;
        }

        Vector3 playerHaipaiPosition = playerHaipaiRect.localPosition;
        Vector3 playerHaipaiScale = playerHaipaiRect.localScale;

        // Skip 버튼 생성 및 위치 설정
        skipButton = CreateButton(skipButtonPrefab, mainCanvas.transform, "SKIP");
        RectTransform skipButtonRect = skipButton.GetComponent<RectTransform>();
        if (skipButtonRect == null)
        {
            Debug.LogError("[MakeButtonsAndHandlePlayerDecision] SkipButton RectTransform not found.");
            CmdReturnActionDecision(playerIndex, new ActionPriorityInfo(ActionType.SKIP, priority, -1), actionTurnId, tileId);
            yield break;
        }

        skipButtonRect.localPosition = new Vector3(
            playerHaipaiPosition.x + playerHaipaiRect.sizeDelta.x * playerHaipaiScale.x,
            playerHaipaiPosition.y + (playerHaipaiRect.sizeDelta.y / 2) * playerHaipaiScale.y,
            0
        );

        Debug.Log($"[MakeButtonsAndHandlePlayerDecision] SkipButton position set to {skipButtonRect.localPosition}.");

        // ActionType별로 선택지를 분류
        Dictionary<ActionType, List<ActionPriorityInfo>> ActionCountDictionary = new Dictionary<ActionType, List<ActionPriorityInfo>>();
        foreach (ActionType action in System.Enum.GetValues(typeof(ActionType)))
        {
            ActionCountDictionary[action] = new List<ActionPriorityInfo>();
        }
        foreach (var action in actions)
        {
            ActionCountDictionary[action.Type].Add(action);
        }

        // 버튼 생성 및 선택 처리
        float buttonSpacing = skipButtonRect.sizeDelta.x * skipButtonRect.localScale.x; // 버튼 간격
        int buttonIndex = 0;

        foreach (var actionPair in ActionCountDictionary)
        {
            if (actionPair.Value.Count == 0) continue; // 선택지가 없는 경우 버튼 생성 안 함
            string str = $"action type: {actionPair.Key.ToString()}, anctions:";
            for (int i = 0; i < actionPair.Value.Count; i++)
            {
                str += $" {actionPair.Value[i]}";
            }
            Debug.Log(str);
            GameObject buttonPrefab = null;
            string buttonType = actionPair.Key.ToString(); // 전역 변수 이름과 매핑
            switch (actionPair.Key)
            {
                case ActionType.HU:
                    buttonPrefab = huButtonPrefab;
                    break;
                case ActionType.KAN:
                    buttonPrefab = kanButtonPrefab;
                    break;
                case ActionType.PON:
                    buttonPrefab = ponButtonPrefab;
                    break;
                case ActionType.CHII:
                    buttonPrefab = chiiButtonPrefab;
                    break;
                case ActionType.FLOWER:
                    buttonPrefab = flowerButtonPrefab;
                    break;
            }

            if (buttonPrefab != null)
            {
                // 버튼 생성 및 전역 변수에 할당
                GameObject actionButton = CreateButton(buttonPrefab, mainCanvas.transform, buttonType);
                RectTransform actionButtonRect = actionButton.GetComponent<RectTransform>();
                if (actionButtonRect != null)
                {
                    // 버튼 위치 계산
                    actionButtonRect.localPosition = new Vector3(
                        skipButtonRect.localPosition.x - (buttonSpacing * (buttonIndex + 1)),
                        skipButtonRect.localPosition.y,
                        0
                    );
                    Debug.Log($"[MakeButtonsAndHandlePlayerDecision] Button for {actionPair.Key} placed at {actionButtonRect.localPosition}.");
                }

                // 클릭 이벤트 처리
                if (actionPair.Value.Count == 1)
                {
                    // 선택지가 하나인 경우 바로 실행
                    var selectedAction = actionPair.Value[0];
                    actionButton.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        Debug.Log($"[MakeButtonsAndHandlePlayerDecision] {selectedAction.Type} button clicked with single choice.");
                        decisionMade = true;
                        CmdReturnActionDecision(playerIndex, selectedAction, actionTurnId, tileId);
                    });
                }
                else
                {
                    // 선택지가 여러 개인 경우 추가 선택지 UI 표시
                    actionButton.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        Debug.Log($"[MakeButtonsAndHandlePlayerDecision] {actionPair.Key} button clicked with multiple choices.");
                        ShowAdditionalChoices(playerIndex, actionPair.Value, actionTurnId, tileId);
                    });
                }

                buttonIndex++;
            }
        }

        // Skip 버튼 클릭 이벤트 추가
        skipButton.GetComponent<Button>().onClick.AddListener(() =>
        {
            Debug.Log("[MakeButtonsAndHandlePlayerDecision] Skip button clicked.");
            decisionMade = true;
            CmdReturnActionDecision(playerIndex, new ActionPriorityInfo(ActionType.SKIP, priority, -1), actionTurnId, tileId);
        });

        // 20초 타이머 시작
        float remainingTime = 30f;

        while (remainingTime > 0 && !decisionMade)
        {
            remainingTime -= Time.deltaTime;
            yield return null;
        }

        if (!decisionMade)
        {
            Debug.Log("[MakeButtonsAndHandlePlayerDecision] Time is up. Defaulting to Skip.");
            CmdReturnActionDecision(playerIndex, new ActionPriorityInfo(ActionType.SKIP, priority, -1), actionTurnId, tileId);
        }

        // 버튼 제거
        DeleteButtons();

        Debug.Log("[MakeButtonsAndHandlePlayerDecision] Buttons destroyed. Decision process completed.");
    }

    private GameObject additionalChoicesContainer;
    public GameObject backButtonPrefab;
    private GameObject backButton;
    // 추가 선택지를 보여주는 함수
    private void ShowAdditionalChoices(int playerIndex, List<ActionPriorityInfo> choices, int actionTurnId, int tileId)
    {
        Debug.Log("[ShowAdditionalChoices] Displaying additional choices.");

        // 기존 버튼 비활성화
        DisableButtons();

        // `Main Canvas` 찾기
        Canvas mainCanvas = GameObject.Find("Main Canvas").GetComponent<Canvas>();
        if (mainCanvas == null)
        {
            Debug.LogError("[ShowAdditionalChoices] Main Canvas not found.");
            return;
        }

        // 추가 선택지를 감싸는 컨테이너 생성
        additionalChoicesContainer = new GameObject("AdditionalChoicesContainer", typeof(RectTransform));
        additionalChoicesContainer.transform.SetParent(mainCanvas.transform, false);

        RectTransform containerRect = additionalChoicesContainer.GetComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(600, 300); // 기본 크기
        containerRect.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 정렬
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.localPosition = new Vector3(0, 200, 0); // PlayerHaipai 위에 위치

        // 배경 생성
        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        background.transform.SetParent(additionalChoicesContainer.transform, false);

        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.sizeDelta = new Vector2(600, 300); // 기본 크기
        backgroundRect.anchorMin = new Vector2(0, 0);
        backgroundRect.anchorMax = new Vector2(1, 1);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        background.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f); // 반투명 검정

        // 뒤로가기 버튼 생성
        if (backButtonPrefab != null)
        {
            backButton = Instantiate(backButtonPrefab, additionalChoicesContainer.transform);
            RectTransform backButtonRect = backButton.GetComponent<RectTransform>();
            backButtonRect.anchorMin = new Vector2(1, 1); // 우상단 정렬
            backButtonRect.anchorMax = new Vector2(1, 1);
            backButtonRect.pivot = new Vector2(1, 1);
            backButtonRect.sizeDelta = new Vector2(50, 50); // 크기
            backButtonRect.anchoredPosition = new Vector2(-10, -10); // 우상단 여백

            backButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                Debug.Log("[ShowAdditionalChoices] Back button clicked.");
                DestroyAdditionalChoices(); // 추가 선택지 삭제
                EnableButtons(); // 기존 버튼 활성화
            });
        }

        // 선택지 버튼 생성
        float buttonWidth = 120;
        float buttonHeight = 50;
        float spacing = 10; // 버튼 간격

        int index = 0;
        foreach (var choice in choices)
        {
            // 버튼 생성
            GameObject choiceButton = new GameObject($"ChoiceButton_{choice.Type}_{index++}", typeof(RectTransform), typeof(Button), typeof(Image));
            choiceButton.transform.SetParent(additionalChoicesContainer.transform, false);
            RectTransform choiceButtonRect = choiceButton.GetComponent<RectTransform>();
            choiceButtonRect.sizeDelta = new Vector2(buttonWidth, buttonHeight);

            // 버튼 위치 계산
            float xOffset = (index % 3) * (buttonWidth + spacing) - ((choices.Count - 1) % 3) * (buttonWidth + spacing) / 2;
            float yOffset = -((index / 3) * (buttonHeight + spacing));
            choiceButtonRect.anchoredPosition = new Vector2(xOffset, yOffset);

            // CHII와 KAN의 경우 타일 묶음을 생성
            GameObject tileGroup = new GameObject("TileGroup", typeof(RectTransform));
            tileGroup.transform.SetParent(choiceButton.transform, false);
            RectTransform tileGroupRect = tileGroup.GetComponent<RectTransform>();
            tileGroupRect.sizeDelta = new Vector2(buttonWidth, buttonHeight);
            tileGroupRect.anchorMin = new Vector2(0.5f, 0.5f);
            tileGroupRect.anchorMax = new Vector2(0.5f, 0.5f);
            tileGroupRect.pivot = new Vector2(0.5f, 0.5f);
            tileGroupRect.localPosition = Vector3.zero;

            if (choice.Type == ActionType.CHII)
            {
                // CHII의 경우 TileId, TileId+1, TileId+2
                for (int i = 0; i < 3; i++)
                {
                    GameObject tile = Instantiate(TilePrefabArray[choice.TileId + i], tileGroup.transform);
                    RectTransform tileRect = tile.GetComponent<RectTransform>();
                    tileRect.sizeDelta = new Vector2(30, 30);
                    tileRect.anchoredPosition = new Vector2((i - 1) * 35, 0); // 타일 간격

                    // TileEvent의 isDraggable 설정
                    TileEvent tileEvent = tile.GetComponent<TileEvent>();
                    if (tileEvent != null)
                    {
                        tileEvent.SetUndraggable();
                    }
                }
            }
            else if (choice.Type == ActionType.KAN)
            {
                // KAN의 경우 TileId 4개
                for (int i = 0; i < 4; i++)
                {
                    GameObject tile = Instantiate(TilePrefabArray[choice.TileId], tileGroup.transform);
                    RectTransform tileRect = tile.GetComponent<RectTransform>();
                    tileRect.sizeDelta = new Vector2(30, 30);
                    tileRect.anchoredPosition = new Vector2((i - 1.5f) * 35, 0); // 타일 간격

                    // TileEvent의 isDraggable 설정
                    TileEvent tileEvent = tile.GetComponent<TileEvent>();
                    if (tileEvent != null)
                    {
                        tileEvent.SetUndraggable();
                    }
                }
            }

            // 버튼 클릭 이벤트
            choiceButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                Debug.Log($"[ShowAdditionalChoices] {choice.Type} choice clicked.");
                DestroyAdditionalChoices(); // 추가 선택지 삭제
                CmdReturnActionDecision(playerIndex, choice, actionTurnId, tileId); // 선택 결과 전송
            });
        }

        Debug.Log("[ShowAdditionalChoices] Additional choices displayed.");
    }

    private void DestroyAdditionalChoices()
    {
        if (additionalChoicesContainer != null)
        {
            Destroy(additionalChoicesContainer);
            additionalChoicesContainer = null;
        }

        if (backButton != null)
        {
            Destroy(backButton);
            backButton = null;
        }

        Debug.Log("[DestroyAdditionalChoices] Additional choices and background destroyed.");
    }


    [TargetRpc]
    public void TargetWaitForPlayerDecision(NetworkConnection target, int playerIndex, int totalScore)
    {
        StartCoroutine(ClientWaitForPlayerDecision(playerIndex, totalScore));
    }

    private IEnumerator ClientWaitForPlayerDecision(int playerIndex, int totalScore)
    {
        Debug.Log("[ClientWaitForPlayerDecision] Starting decision coroutine.");

        // `Main Canvas` 찾기
        Canvas mainCanvas = GameObject.Find("Main Canvas").GetComponent<Canvas>();
        if (mainCanvas == null)
        {
            Debug.LogError("[ClientWaitForPlayerDecision] Main Canvas not found.");
            CmdReturnDecision(playerIndex, false);
            yield break;
        }

        // `PlayerHaipai`의 RectTransform 가져오기
        RectTransform playerHaipaiRect = PlayerHaipai.GetComponent<RectTransform>();
        if (playerHaipaiRect == null)
        {
            Debug.LogError("[ClientWaitForPlayerDecision] PlayerHaipai RectTransform not found.");
            CmdReturnDecision(playerIndex, false);
            yield break;
        }

        Vector3 playerHaipaiPosition = playerHaipaiRect.localPosition;
        Vector3 playerHaipaiScale = playerHaipaiRect.localScale;

        // Skip 버튼 생성 및 위치 설정
        skipButton = Instantiate(skipButtonPrefab, mainCanvas.transform);
        RectTransform skipButtonRect = skipButton.GetComponent<RectTransform>();
        if (skipButtonRect == null)
        {
            Debug.LogError("[ClientWaitForPlayerDecision] SkipButton RectTransform not found.");
            CmdReturnDecision(playerIndex, false);
            yield break;
        }

        skipButtonRect.localPosition = new Vector3(
            playerHaipaiPosition.x + playerHaipaiRect.sizeDelta.x * playerHaipaiScale.x,
            playerHaipaiPosition.y + (playerHaipaiRect.sizeDelta.y / 2) * playerHaipaiScale.y,
            0
        );

        Debug.Log($"[ClientWaitForPlayerDecision] SkipButton position set to {skipButtonRect.localPosition}.");

        // Hu 버튼 생성 및 위치 설정
        huButton = Instantiate(huButtonPrefab, mainCanvas.transform);
        RectTransform huButtonRect = huButton.GetComponent<RectTransform>();
        if (huButtonRect == null)
        {
            Debug.LogError("[ClientWaitForPlayerDecision] HuButton RectTransform not found.");
            CmdReturnDecision(playerIndex, false);
            yield break;
        }

        huButtonRect.localPosition = new Vector3(
            skipButtonRect.localPosition.x - (skipButtonRect.sizeDelta.x * skipButtonRect.localScale.x),
            skipButtonRect.localPosition.y,
            0
        );

        Debug.Log($"[ClientWaitForPlayerDecision] HuButton position set to {huButtonRect.localPosition}.");

        bool decisionMade = false;
        bool isHu = false;

        // Hu 버튼 클릭 이벤트 추가
        huButton.GetComponent<Button>().onClick.AddListener(() =>
        {
            Debug.Log("[ClientWaitForPlayerDecision] Hu button clicked.");
            decisionMade = true;
            isHu = true;
        });

        // Skip 버튼 클릭 이벤트 추가
        skipButton.GetComponent<Button>().onClick.AddListener(() =>
        {
            Debug.Log("[ClientWaitForPlayerDecision] Skip button clicked.");
            decisionMade = true;
            isHu = false;
        });

        Debug.Log("[ClientWaitForPlayerDecision] Button click events added.");

        // 플레이어의 결정을 대기
        yield return new WaitUntil(() => decisionMade);

        Debug.Log("[ClientWaitForPlayerDecision] Player decision made.");

        // 버튼 제거
        //Destroy(huButton);
        //Destroy(skipButton);
        DeleteButtons();
        Debug.Log("[ClientWaitForPlayerDecision] Buttons destroyed.");

        CmdReturnDecision(playerIndex, isHu);
    }


    [Command]
    private void CmdReturnDecision(int playerIndex, bool isHu)
    {
        Debug.Log($"[CmdReturnDecision] Player {playerIndex} decision: {(isHu ? "Hu" : "Skip")}");

        if (isHu)
        {
            serverManager.FinalizeRoundScore(playerIndex);
        }
        else
        {
            Debug.Log($"[CmdReturnDecision] Player {playerIndex} skipped Hu.");
        }
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