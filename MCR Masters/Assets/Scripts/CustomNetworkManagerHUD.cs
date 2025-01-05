using UnityEngine;
using Mirror;

public class CustomNetworkManagerHUD : NetworkManagerHUD
{
    public NetworkRoomManager my_manager; // NetworkManager 참조

    void Update()
    {
        // Host 시작 (단축키: H)
        if (Input.GetKeyDown(KeyCode.H))
        {
            my_manager.StartHost();
            Debug.Log("Host started via shortcut (H)");
        }

        // 클라이언트 연결 (단축키: C)
        if (Input.GetKeyDown(KeyCode.C))
        {
            my_manager.StartClient();
            Debug.Log("Client started via shortcut (C)");
        }

        // 서버 시작 (단축키: S)
        if (Input.GetKeyDown(KeyCode.S))
        {
            my_manager.StartServer();
            Debug.Log("Server started via shortcut (S)");
        }

        // 호스트 중지 (단축키: Q)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (NetworkServer.active || NetworkClient.isConnected)
            {
                my_manager.StopHost();
                Debug.Log("Host stopped via shortcut (Q)");
            }
        }
    }
}
