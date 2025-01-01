using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TileEvent : MonoBehaviour
{
    public GameObject Canvas;
    public GameObject PlayerHaipai;
    private Vector3 originalPosition;

    private bool isDragging = false;
    private bool isDraggable = true;
    private bool isHoveringEnd = false;
    private GameObject startParent;
    private Vector2 startPosition;
    private int siblingIndex = -1;

    private float discardThresholdY;

    public void Awake()
    {
        Debug.Log("Awake: Initializing TileEvent.");
        Canvas = GameObject.Find("Main Canvas");
        PlayerHaipai = GameObject.Find("PlayerHaipai");

        if (Canvas == null) Debug.LogError("Canvas not found in scene.");
        if (PlayerHaipai == null) Debug.LogError("PlayerHaipai not found in scene.");

        discardThresholdY = Screen.height * 0.25f;
        Debug.Log($"Discard threshold set to {discardThresholdY} (25% of screen height).");

        AddEventListeners();
    }

    private void AddEventListeners()
    {
        Debug.Log("Adding event listeners.");
        EventTrigger trigger = gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry dragStartEntry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
        dragStartEntry.callback.AddListener((data) => { StartDrag(); });
        trigger.triggers.Add(dragStartEntry);

        EventTrigger.Entry dragEndEntry = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
        dragEndEntry.callback.AddListener((data) => { EndDrag(); });
        trigger.triggers.Add(dragEndEntry);

        EventTrigger.Entry hoverEnterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        hoverEnterEntry.callback.AddListener((data) => { OnHoverEnter(); });
        trigger.triggers.Add(hoverEnterEntry);

        EventTrigger.Entry hoverExitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        hoverExitEntry.callback.AddListener((data) => { OnHoverExit(); });
        trigger.triggers.Add(hoverExitEntry);
    }

    public void StartDrag()
    {
        if (!isDraggable) return;

        Debug.Log($"StartDrag: Dragging started for {gameObject.name}");
        isDragging = true;
        startParent = transform.parent.gameObject;
        siblingIndex = transform.GetSiblingIndex();
        startPosition = transform.position;
    }

    public void EndDrag()
    {
        if (!isDraggable) return;

        Debug.Log($"EndDrag: Dragging ended for {gameObject.name}");
        isDragging = false;

        if (transform.position.y >= discardThresholdY)
        {
            isDraggable = false;
            Debug.Log($"Tile {gameObject.name} dropped above discard threshold. Discarding...");
            HandleTileDiscard();
        }
        else
        {
            Debug.Log($"Tile {gameObject.name} dropped below discard threshold. Resetting position.");
            ResetPosition();
        }
    }

    private void ResetPosition()
    {
        Debug.Log($"ResetPosition: Resetting {gameObject.name} to original position.");
        transform.SetParent(Canvas.transform, true);
        transform.SetParent(startParent.transform, false);
        transform.SetSiblingIndex(siblingIndex);
    }

    public void OnHoverEnter()
    {
        if (!isDraggable) return;

        Debug.Log($"OnHoverEnter: Mouse entered over {gameObject.name}");
        originalPosition = transform.position;
        siblingIndex = transform.GetSiblingIndex();
        transform.position = new Vector3(originalPosition.x, originalPosition.y + 10 * Screen.height / 1920f, originalPosition.z);
    }

    public void OnHoverExit()
    {
        if (!isDraggable || isHoveringEnd) return;

        Debug.Log($"OnHoverExit: Mouse exited from {gameObject.name}");
        transform.position = originalPosition;
        transform.SetSiblingIndex(siblingIndex);
    }

    void Update()
    {
        if (isDragging)
        {
            transform.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            Debug.Log($"Update: Dragging {gameObject.name} to position {transform.position}");
        }
    }

    private void HandleTileDiscard()
    {
        Debug.Log($"HandleTileDiscard: Handling discard logic for {gameObject.name}");
        // TODO: Implement local discard logic here (e.g., removing from player's hand, updating UI).
    }
}
