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

    public ServerManager ServerManager;

    private Hand PlayerHand = new Hand();  
    private WinningCondition PlayerWinningCondition = new WinningCondition();
    public List<int> PlayerKawaTiles = new List<int>();


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
