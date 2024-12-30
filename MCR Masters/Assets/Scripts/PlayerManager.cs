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

    [SyncVar]
    public int CurrentScore;

    [SyncVar]
    public int SeatWind;

    [SyncVar]
    public int RoundWind;

    [SyncVar]
    public bool IsPlayerTurn;

    public ServerManager ServerManager;

    public Hand PlayerHand = new Hand();
    public WinningCondition PlayerWinningCondition = new WinningCondition();
    public List<int> PlayerKawaTiles = new List<int>();


    public bool CheckIfPlayerTurn()
    {
        return IsPlayerTurn;
    }
}
