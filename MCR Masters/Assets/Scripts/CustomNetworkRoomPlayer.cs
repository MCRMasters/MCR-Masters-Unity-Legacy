using Mirror;
using UnityEngine;

public class CustomNetworkRoomPlayer : NetworkRoomPlayer
{
    // 새로운 메서드: 기존 CmdChangeReadyState를 활용하고 커스텀 로직 추가
    [Command]
    public void ChangeReadyState(bool ready)
    {
        // 기본 CmdChangeReadyState 호출
        CmdChangeReadyState(ready);

        // 추가 동작
        Debug.Log($"Custom logic: Player {index} is now {(ready ? "ready" : "not ready")}.");
    }

    public override void OnClientEnterRoom()
    {
        base.OnClientEnterRoom();
        Debug.Log($"Player {index} entered the room.");
    }

    public override void OnClientExitRoom()
    {
        base.OnClientExitRoom();
        Debug.Log($"Player {index} left the room.");
    }

}
