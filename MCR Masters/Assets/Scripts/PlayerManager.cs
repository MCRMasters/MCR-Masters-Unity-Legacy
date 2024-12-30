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

    public PlayerStatus PlayerStatus;

    public ServerManager ServerManager;

    public Hand PlayerHand = new Hand();
    public WinningCondition PlayerWinningCondition = new WinningCondition();
    public List<int> PlayerKawaTiles = new List<int>();


    public bool CheckIfPlayerTurn()
    {
        return PlayerStatus.IsPlayerTurn;
    }
}
