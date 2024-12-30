using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyUI : MonoBehaviour
{
    public Button readyButton; // Button을 Inspector에서 연결하지 않아도 됩니다.
    private CustomNetworkRoomPlayer roomPlayer;

    void Start()
    {
        // 서버에서 실행 시 로직 무시
        if (!NetworkClient.active)
        {
            Debug.LogWarning("LobbyUI: This script is intended to run on the client.");
            return;
        }

        // 로컬 플레이어 가져오기
        var networkIdentity = NetworkClient.connection.identity;
        roomPlayer = networkIdentity?.GetComponent<CustomNetworkRoomPlayer>();
        if (roomPlayer == null)
        {
            Debug.LogError("LobbyUI: Unable to find CustomNetworkRoomPlayer for the local client.");
            return;
        }

        // ReadyButton 동적 찾기
        readyButton = GameObject.Find("ReadyButton")?.GetComponent<Button>();
        if (readyButton == null)
        {
            Debug.LogError("LobbyUI: ReadyButton not found. Check the GameObject name.");
            return;
        }

        // 버튼 클릭 이벤트 추가
        readyButton.onClick.AddListener(OnReadyButtonClicked);

        // 초기 버튼 상태 설정
        UpdateButtonUI(false);
    }

    public void OnReadyButtonClicked()
    {
        if (roomPlayer != null)
        {
            // 현재 상태의 반대로 설정
            bool newReadyState = !roomPlayer.readyToBegin;
            roomPlayer.CmdChangeReadyState(newReadyState);

            // UI 업데이트
            UpdateButtonUI(newReadyState);
        }
    }

    void UpdateButtonUI(bool isReady)
    {
        TMP_Text buttonText = readyButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.text = isReady ? "Cancel" : "Ready";
        }
        else
        {
            Debug.LogError("LobbyUI: TMP_Text component not found on ReadyButton.");
        }
    }
}
