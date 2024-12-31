using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TileEvent : NetworkBehaviour
{
    public GameObject Canvas;
    public GameObject PlayerHaipai;
    private Vector3 originalPosition;
    public PlayerManager playerManager;

    private bool isDragging = false;
    private bool isDraggable = true;
    private bool isHoveringEnd = false;
    private GameObject startParent;
    private Vector2 startPosition;
    private int siblingIndex = -1;

    // 특정 높이 기준 (예: 화면 비율을 고려한 상대적인 Y 좌표 기준)
    private float discardThresholdY;


    public void Awake()
    {
        Canvas = GameObject.Find("Main Canvas");
        PlayerHaipai = GameObject.Find("PlayerHaipai");

        // discardThresholdY를 화면 높이를 기준으로 설정 (예: 화면 상단 20%)
        discardThresholdY = Screen.height * 0.8f;

        // 드래그 및 호버 이벤트를 동적으로 추가
        AddEventListeners();
    }

    private void AddEventListeners()
    {
        EventTrigger trigger = gameObject.AddComponent<EventTrigger>();

        // 드래그 시작 이벤트
        EventTrigger.Entry dragStartEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.BeginDrag
        };
        dragStartEntry.callback.AddListener((data) => { StartDrag(); });
        trigger.triggers.Add(dragStartEntry);

        // 드래그 종료 이벤트
        EventTrigger.Entry dragEndEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.EndDrag
        };
        dragEndEntry.callback.AddListener((data) => { EndDrag(); });
        trigger.triggers.Add(dragEndEntry);

        // 호버 시작 이벤트
        EventTrigger.Entry hoverEnterEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        hoverEnterEntry.callback.AddListener((data) => { OnHoverEnter(); });
        trigger.triggers.Add(hoverEnterEntry);

        // 호버 종료 이벤트
        EventTrigger.Entry hoverExitEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit
        };
        hoverExitEntry.callback.AddListener((data) => { OnHoverExit(); });
        trigger.triggers.Add(hoverExitEntry);
    }

    void Start()
    {
        if (!isOwned)
        {
            isDraggable = false;
        }
    }

    public void StartDrag()
    {
        if (!isDraggable) return;
        isDragging = true;
        startParent = transform.parent.gameObject;
        siblingIndex = transform.GetSiblingIndex();
        startPosition = transform.position;
    }

    public void EndDrag()
    {
        if (!isDraggable) return;
        isDragging = false;

        if (transform.position.y >= discardThresholdY)
        {
            playerManager.CmdDiscardTile(gameObject);
        }
        else
        {
            ResetPosition();
        }
    }

    private void ResetPosition()
    {
        transform.SetParent(Canvas.transform, true);
        transform.SetParent(startParent.transform, false);
        transform.SetSiblingIndex(siblingIndex);
    }

    public void OnHoverEnter()
    {
        if (!isOwned || !isDraggable) return;

        originalPosition = transform.position;
        siblingIndex = transform.GetSiblingIndex();
        transform.position = new Vector3(originalPosition.x, originalPosition.y + 10 * Screen.height / 1920f, originalPosition.z);
    }

    public void OnHoverExit()
    {
        if (!isOwned || !isDraggable || isHoveringEnd) return;
        transform.position = originalPosition;
        transform.SetSiblingIndex(siblingIndex);
    }

    void Update()
    {
        if (isDragging)
        {
            transform.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        }
    }
}
