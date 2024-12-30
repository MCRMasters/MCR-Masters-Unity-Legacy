using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using UnityEngine;
using Mirror;
using Game.Shared;


public class ServerManager : NetworkBehaviour
{
    private static List<int> tileDeck = new List<int>();
    private static int currentIndex = 0;
    private const int TotalTiles = 144;
    public PlayerManager[] PlayerManagers;

    [SyncVar]
    public int CurrentRound = -1;

    [SyncVar]
    public Wind RoundWind = Wind.EAST;

    [SyncVar]
    public int RoundCount = -1;

    public void Start()
    {
        Debug.Log("Server started. Ready for the first round initialization.");
        for(int i = 0; i < 16; ++i)
        {
            StartNewRound();
        }
    }

    public void StartNewRound()
    {
        if (RoundWind > Wind.NORTH)
        {
            Debug.Log("Game over. All rounds completed.");
            return;
        }

        RoundCount++;

        if (RoundCount == 4)
        {
            RoundCount = 0;
            AdjustPositionsAfterRound();
            RoundWind++;
        }
        else
        {
            RotatePlayers();
        }

        CurrentRound++;

        InitializeTiles();
        ShuffleTiles();
        DealTilesToPlayers();
        UpdatePlayerStates();

        Debug.Log($"New round started: Round {CurrentRound}, Wind: {RoundWind}");
    }

    private void RotatePlayers()
    {
        var seatWinds = PlayerManagers.Select(pm => pm.PlayerStatus.SeatWind).ToList();
        if (seatWinds.Count != PlayerManagers.Length)
        {
            Debug.LogError("Mismatch between seat winds and player managers count.");
            return;
        }

        var rotated = new List<Wind>
        {
            seatWinds[3], // North becomes East
            seatWinds[0], // East becomes South
            seatWinds[1], // South becomes West
            seatWinds[2]  // West becomes North
        };

        PlayerManager[] tempManagers = new PlayerManager[PlayerManagers.Length];

        for (int i = 0; i < PlayerManagers.Length; i++)
        {
            PlayerManagers[i].PlayerStatus.SeatWind = rotated[i];
        }

        for (int i = 0; i < PlayerManagers.Length; i++)
        {
            tempManagers[(int)PlayerManagers[i].PlayerStatus.SeatWind - (int)Wind.EAST] = PlayerManagers[i];
        }

        for (int i = 0; i < PlayerManagers.Length; i++)
        {
            PlayerManagers[i] = tempManagers[i];
        }

        Debug.Log("Players rotated and reassigned.");
        for (int i = 0; i < PlayerManagers.Length; i++)
        {
            Debug.Log($"Index {i}: Player {PlayerManagers[i].PlayerName} - Wind: {PlayerManagers[i].PlayerStatus.SeatWind}");
        }
    }

    private void AdjustPositionsAfterRound()
    {
        var seatWinds = PlayerManagers.Select(pm => pm.PlayerStatus.SeatWind).ToList();
        if (seatWinds.Count != PlayerManagers.Length)
        {
            Debug.LogError("Mismatch between seat winds and player managers count.");
            return;
        }

        PlayerManager[] tempManagers = new PlayerManager[PlayerManagers.Length];

        if (RoundWind == Wind.EAST || RoundWind == Wind.WEST)
        {
            // Swap East and South, West and North
            var swapped = new List<Wind>
            {
                seatWinds[1], // South
                seatWinds[0], // East
                seatWinds[3], // North
                seatWinds[2]  // West
            };

            for (int i = 0; i < PlayerManagers.Length; i++)
            {
                PlayerManagers[i].PlayerStatus.SeatWind = swapped[i];
            }
        }
        else if (RoundWind == Wind.SOUTH)
        {
            // Full rotate
            var rotated = new List<Wind>
            {
                seatWinds[2], // West
                seatWinds[0], // East
                seatWinds[3], // North
                seatWinds[1]  // South
            };

            for (int i = 0; i < PlayerManagers.Length; i++)
            {
                PlayerManagers[i].PlayerStatus.SeatWind = rotated[i];
            }
        }

        for (int i = 0; i < PlayerManagers.Length; i++)
        {
            tempManagers[(int)PlayerManagers[i].PlayerStatus.SeatWind - (int)Wind.EAST] = PlayerManagers[i];
        }

        for (int i = 0; i < PlayerManagers.Length; i++)
        {
            PlayerManagers[i] = tempManagers[i];
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
    private void UpdatePlayerStates()
    {
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn.identity.TryGetComponent<PlayerManager>(out var playerManager))
            {
                playerManager.PlayerStatus.IsPlayerTurn = false;
            }
        }

        var firstPlayer = NetworkServer.connections.Values
            .Select(conn => conn.identity.GetComponent<PlayerManager>())
            .FirstOrDefault(player => player != null && player.PlayerStatus.SeatWind == Wind.EAST);

        if (firstPlayer != null)
        {
            firstPlayer.PlayerStatus.IsPlayerTurn = true;
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

        StartNewRound();
    }
}
