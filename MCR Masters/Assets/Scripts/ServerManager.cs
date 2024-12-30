using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using Mirror;
using System.Linq;
using Game.Shared;

public class ServerManager : NetworkBehaviour
{
    private static List<int> tileDeck = new List<int>();
    private static int currentIndex = 0;
    private const int TotalTiles = 144;
    public PlayerManager[] PlayerManagers;

    [SyncVar]
    public int CurrentRound = -1;

    public override void OnStartServer()
    {
        base.OnStartServer();
        // test code for dict
        Debug.Log("In OnStartServer function.");
        // seems this function didn't excute

    }

    public void Start()
    {
        Debug.Log("Server started. Ready for the first round initialization.");
        // test code for dict
        //Debug.Log(TileDictionary.TileToString.ContainsKey(0));
    }
    public void StartNewRound()
    {
        CurrentRound++;
        InitializeTiles();
        ShuffleTiles();
        DealTilesToPlayers();
        UpdatePlayerStates();
        Debug.Log("New round started: Round " + CurrentRound);
    }

    private void InitializeTiles()
    {
        tileDeck.Clear();
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

    [Server]
    public List<int> DrawTiles(int count)
    {
        if (tileDeck.Count - currentIndex < count)
        {
            Debug.LogWarning("Not enough tiles left in the deck.");
            return null;
        }

        var drawnTiles = tileDeck.GetRange(currentIndex, count);
        currentIndex += count;
        return drawnTiles;
    }

    [Server]
    public void DealTilesToPlayers()
    {
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn.identity.TryGetComponent<PlayerManager>(out var playerManager))
            {
                var handTiles = DrawTiles(13);
                if (handTiles == null)
                {
                    Debug.LogError("Not enough tiles to deal to player " + playerManager.PlayerName);
                    return;
                }
                playerManager.PlayerHand.ClosedTiles.AddRange(handTiles);
                playerManager.PlayerHand.ClosedTiles.Sort();
                Debug.Log("Dealt starting hand to player " + playerManager.PlayerName);
            }
        }
    }

    [Server]
    public void PlayerDiscardTile(PlayerManager playerManager, int tile)
    {
        if (!playerManager.PlayerHand.ClosedTiles.Remove(tile))
        {
            Debug.LogError("Player " + playerManager.PlayerName + " tried to discard a tile not in their hand: " + tile);
            return;
        }

        playerManager.PlayerKawaTiles.Add(tile);
        Debug.Log("Player " + playerManager.PlayerName + " discarded tile: " + tile);
    }

    [Server]
    private void UpdatePlayerStates()
    {
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn.identity.TryGetComponent<PlayerManager>(out var playerManager))
            {
                playerManager.CurrentScore += 10; // Example score update
                playerManager.SeatWind = (playerManager.SeatWind + 1) % 4; // Rotate winds
                playerManager.RoundWind = CurrentRound % 4; // Example round wind logic
                playerManager.IsPlayerTurn = false;

                Debug.Log($"Updated player {playerManager.PlayerName} - Score: {playerManager.CurrentScore}, SeatWind: {playerManager.SeatWind}, RoundWind: {playerManager.RoundWind}");
            }
        }

        // Assign first turn to the player with SeatWind = EAST
        var firstPlayer = NetworkServer.connections.Values
            .Select(conn => conn.identity.GetComponent<PlayerManager>())
            .FirstOrDefault(player => player != null && player.SeatWind == (int)Wind.EAST);

        if (firstPlayer != null)
        {
            firstPlayer.IsPlayerTurn = true;
            Debug.Log("Player " + firstPlayer.PlayerName + " starts the round.");
        }
    }

    [Server]
    public void CheckWinningCondition(PlayerManager playerManager)
    {
        // Example logic for checking a winning condition
        if (playerManager.PlayerHand.ClosedTiles.Count == 0)
        {
            Debug.Log("Player " + playerManager.PlayerName + " has no tiles left and wins the round!");
        }
    }

    [Server]
    public void HandleDraw()
    {
        Debug.Log("No tiles left to draw. The round ends in a draw.");
    }

    [Command]
    public void CmdRequestStartingHand()
    {
        if (!isServer) return;

        var playerManager = connectionToClient.identity.GetComponent<PlayerManager>();
        if (playerManager == null)
        {
            Debug.LogError("PlayerManager not found for connection: " + connectionToClient.connectionId);
            return;
        }

        DealTilesToPlayers();
    }

    [Command]
    public void CmdDiscardTile(int tile)
    {
        if (!isServer) return;

        var playerManager = connectionToClient.identity.GetComponent<PlayerManager>();
        if (playerManager == null)
        {
            Debug.LogError("PlayerManager not found for connection: " + connectionToClient.connectionId);
            return;
        }

        PlayerDiscardTile(playerManager, tile);
    }

    [Command]
    public void CmdEndTurn()
    {
        if (!isServer) return;

        var playerManager = connectionToClient.identity.GetComponent<PlayerManager>();
        if (playerManager == null)
        {
            Debug.LogError("PlayerManager not found for connection: " + connectionToClient.connectionId);
            return;
        }

        AdvanceTurn(playerManager);
    }

    [Server]
    private void AdvanceTurn(PlayerManager currentTurnPlayer)
    {
        currentTurnPlayer.IsPlayerTurn = false;
        var nextPlayer = NetworkServer.connections.Values
            .Select(conn => conn.identity.GetComponent<PlayerManager>())
            .FirstOrDefault(player => player != null && player.SeatWind == (currentTurnPlayer.SeatWind + 1) % 4);

        if (nextPlayer != null)
        {
            nextPlayer.IsPlayerTurn = true;
            Debug.Log("Player " + nextPlayer.PlayerName + " is now taking their turn.");
        }
    }
}
