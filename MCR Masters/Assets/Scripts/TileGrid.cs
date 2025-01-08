using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI.Table;
using System.Collections;

public class TileGrid : MonoBehaviour
{
    public int columns; // Default number of columns in the grid
    public float cellWidth; // Default width of each cell
    public float cellHeight; // Default height of each cell
    public float spacingX; // Default horizontal spacing between cells
    public float spacingY; // Default vertical spacing between cells
    public float additionalSpacingX; // Additional spacing for ShowTsumoTile

    public GameObject playerManager;
    private GameObject LastTsumoTileObject; // Reference to the last added Tsumo tile
    private List<GameObject> indexToChild;


    public void EmptyAll()
    {
        if (LastTsumoTileObject != null)
        {
            Destroy(LastTsumoTileObject);
            LastTsumoTileObject = null;
        }
        foreach (var item in indexToChild)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        indexToChild.Clear();
    }
    private void Awake()
    {
        Debug.Log("TileGrid component added to GameObject and Awake called.");
        // 초기화 작업
        LastTsumoTileObject = null;
        indexToChild = new List<GameObject>();


    }

    private void Start()
    {
        Debug.Log("TileGrid component's Start called.");
        // Start 단계에서 필요한 추가 작업 수행
        // Find all GameObjects with PlayerManager script
        PlayerManager[] allPlayerManagers = UnityEngine.Object.FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);

        // Filter by the `isOwned` property
        foreach (var manager in allPlayerManagers)
        {
            if (manager.isOwned) // Assuming isOwned is a public property or field
            {
                playerManager = manager.gameObject;
                Debug.Log($"[TileGrid] PlayerManager found: {playerManager.name}");
                break;
            }
        }
    }

    private void ArrangeChildrenByIndexAndName()
    {
        List<GameObject> childList = new List<GameObject>();
        foreach (var child in indexToChild)
        {
            if (!!child)
            {
                childList.Add(child);
            }
        }
        if (!!LastTsumoTileObject)
        {
            Debug.Log($"LastTsumoTIleObject: {LastTsumoTileObject.name}");
            childList.Add(LastTsumoTileObject);
            LastTsumoTileObject = null;
        }
        childList = childList.Select(child => child.gameObject)
            .OrderBy(child =>
            {
                string namePart = child.name.Substring(0, 2);

                // "0f"는 항상 최후의 우선순위로 설정
                if (namePart == "0f") return (2, string.Empty);

                // Reverse 기준으로 정렬
                char[] reversed = namePart.ToCharArray();
                Array.Reverse(reversed);
                string reversedString = new string(reversed);

                return (1, reversedString); // 일반 정렬은 reverse된 문자열 사용
            })
            .ToList();
        for (int i = 0; i < childList.Count; i++)
        {
            GameObject child = childList[i];
            int row = i / columns;
            int column = i % columns;

            // Adjust child's size (if it has a RectTransform)
            RectTransform rectTransform = child.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(cellWidth, cellHeight);

            }
            else
            {
                // If not RectTransform, adjust localScale
                child.transform.localScale = new Vector3(cellWidth / 100f, cellHeight / 100f, 1);

            }

            // Set child's position
            Vector3 position = new Vector3(
                column * (cellWidth + spacingX),
                -row * (cellHeight + spacingY),
                0
            );
            child.transform.localPosition = position;

            // Log each child's new position
            //Debug.Log($"Child: {child.name}, Index: {i}, Position: {position}");
        }
        indexToChild = childList;
        // indexToChild 내용을 한 줄로 출력
        string childDebugInfo = string.Join(", ", indexToChild.Select(child => child?.name ?? "null"));
        Debug.Log($"indexToChild contents: [{childDebugInfo}]");

        Debug.Log("Completed ArrangeChildrenByIndexAndName.");
    }

    private void ArrangeChildrenByIndex()
    {
        Debug.Log("Starting ArrangeChildrenByIndex.");

        List<GameObject> childList = new List<GameObject>();
        foreach (var child in indexToChild)
        {
            if (child != null && child != LastTsumoTileObject)
            {
                childList.Add(child);
            }
        }

        for (int i = 0; i < childList.Count; i++)
        {
            GameObject child = childList[i];
            int row = i / columns;
            int column = i % columns;

            // Adjust child's size (if it has a RectTransform)
            RectTransform rectTransform = child.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(cellWidth, cellHeight);
            }
            else
            {
                // If not RectTransform, adjust localScale
                child.transform.localScale = new Vector3(cellWidth / 100f, cellHeight / 100f, 1);

            }

            // Set child's position
            Vector3 position = new Vector3(
                column * (cellWidth + spacingX),
                -row * (cellHeight + spacingY),
                0
            );
            child.transform.localPosition = position;

            // Log each child's new position
            //Debug.Log($"Child: {child.name}, Index: {i}, Position: {position}");
        }
        indexToChild = childList;
        string childDebugInfo = string.Join(", ", indexToChild.Select(child => child?.name ?? "null"));
        Debug.Log($"indexToChild contents: [{childDebugInfo}]");

        if (LastTsumoTileObject != null)
        {
            ShowTsumoTile(LastTsumoTileObject);
        }

        Debug.Log("Completed ArrangeChildrenByIndex.");
    }

    private void ArrangeChildrenByName()
    {
        Debug.Log("Starting ArrangeChildrenByName.");

        var children = transform.Cast<Transform>()
            .Select(child => child.gameObject)
            .OrderBy(child =>
            {
                string namePart = child.name.Substring(0, 2);

                // "0f"는 항상 최후의 우선순위로 설정
                if (namePart == "0f") return (2, string.Empty);

                // Reverse 기준으로 정렬
                char[] reversed = namePart.ToCharArray();
                Array.Reverse(reversed);
                string reversedString = new string(reversed);

                return (1, reversedString); // 일반 정렬은 reverse된 문자열 사용
            })
            .ToList();


        indexToChild = new List<GameObject>(children);
        string childDebugInfo = string.Join(", ", indexToChild.Select(child => child?.name ?? "null"));
        Debug.Log($"indexToChild contents: [{childDebugInfo}]");

        ArrangeChildrenByIndex();
        Debug.Log("Completed ArrangeChildrenByName.");
    }

    public void ShowTedashi(bool IsTedashi)
    {
        if (!IsTedashi)
        {
            if (LastTsumoTileObject)
                Destroy(LastTsumoTileObject);
            LastTsumoTileObject = null;
        }
        else
        {
            StartCoroutine(ShowTedashiCoroutine());
        }
    }

    private IEnumerator ShowTedashiCoroutine()
    {
        // 1. Remove null values from indexToChild and update the list
        indexToChild = indexToChild.Where(obj => obj != null).ToList();

        if (indexToChild.Count == 0)
        {
            Debug.LogError("All objects in indexToChild are null.");
            yield break;
        }

        // 2. Pick random object from updated indexToChild
        int randomIndex = UnityEngine.Random.Range(0, indexToChild.Count);
        GameObject randomObject = indexToChild[randomIndex];

        // 3. Hide it
        randomObject.SetActive(false);
        
        // 4. Wait 1 second
        yield return new WaitForSeconds(1f); //Destroy(randomObject ); 1초 기다리는동안 Destroy 당함
        if (!randomObject)
        {
            yield break;
        }
        // 5. Destroy LastTsumoTileObject if it exists
        if (LastTsumoTileObject)
        {
            Destroy(LastTsumoTileObject);
            LastTsumoTileObject = null;
        }

        // 6. Show it
        randomObject.SetActive(true);
    }



    public void ShowTsumoTile(GameObject lastTsumoTileObject)
    {
        if (lastTsumoTileObject == null)
        {
            Debug.LogError("remove last tsumo tile.");
            if (LastTsumoTileObject)
                Destroy(LastTsumoTileObject);
            LastTsumoTileObject = null;
            return;
        }

        LastTsumoTileObject = lastTsumoTileObject;
        // Adjust child's size (if it has a RectTransform)
        RectTransform rectTransform = LastTsumoTileObject.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(cellWidth, cellHeight);
        }
        else
        {
            // If not RectTransform, adjust localScale
            LastTsumoTileObject.transform.localScale = new Vector3(cellWidth / 100f, cellHeight / 100f, 1);
        }
        for (int i= indexToChild.Count - 1; i >= 0; --i)
        {
            if (indexToChild[i] != null)
            {

                Debug.Log($"[ShowTsumoTile] Last Tile index : {i+1}");

                Vector3 lastTilePosition = indexToChild[i].transform.localPosition;
                Vector3 position = new Vector3(
                    lastTilePosition.x + cellWidth + spacingX + additionalSpacingX,
                    lastTilePosition.y + spacingY,
                    lastTilePosition.z
                );
                lastTsumoTileObject.transform.localPosition = position;

                Debug.Log("A new Tsumo tile has been added with adjusted position.");
                return;
            }
        }

        Debug.LogError("No tile in hand, failed to add tsumo tile.");
    }


    public void DiscardSelectedTile(GameObject tile)
    {
        if (tile == null)
        {
            Debug.LogError("Tile is not part of this grid or is invalid.");
            return;
        }

        var tileName = tile.name;
        bool isTsumoTile = false;

        if (tile == LastTsumoTileObject)
        {
            isTsumoTile = true;
            LastTsumoTileObject = null;
        }
        Debug.Log($"Attempting to remove tile: {tile.name}");
        indexToChild.Remove(tile);
        Debug.Log($"Tile {tile.name} removed from indexToChild list.");

        Debug.Log($"Attempting to destroy tile: {tile.name}");
        Destroy(tile);
        Debug.Log($"Tile {tile.name} destroyed.");

        Debug.Log("Rearranging children after discard.");
        ArrangeChildrenByIndexAndName();
        Debug.Log("Children rearranged after discard.");


        if (playerManager == null)
        {
            Debug.LogError("playerManager is null.");
            return;
        }

        PlayerManager pm = playerManager.GetComponent<PlayerManager>();
        if (pm == null)
        {
            Debug.LogError("PlayerManager component not found on playerManager.");
            return;
        }


        Debug.Log($"PlayerManager component found. PlayerIndex: {pm.PlayerIndex}, PlayerName: {pm.PlayerName}");

        Debug.Log($"playerManager GameObject name: {playerManager.name}");
        Debug.Log($"playerManager components: {string.Join(", ", playerManager.GetComponents<Component>().Select(c => c.GetType().Name))}");
        pm.CmdDiscardTile(tileName, isTsumoTile);
        pm.DeleteButtons();

        Debug.Log($"The selected tile {tileName} has been discarded and the grid rearranged.");

    }


    public void AddTileToLastIndex(GameObject tile)
    {
        if (tile == null)
        {
            Debug.LogError("Cannot add a null tile to the grid.");
            return;
        }

        indexToChild.Add(tile);
        ArrangeChildrenByIndex();

        Debug.Log($"Tile {tile.name} added to the grid and rearranged.");
    }


    public void SetChildIndex(GameObject child, int index)
    {
        if (child == null || !indexToChild.Contains(child))
        {
            Debug.LogError("Child is not part of this grid.");
            return;
        }

        ArrangeChildrenByIndex();
        Debug.Log($"Index for child {child.name} set to {index}.");
    }

    public void UpdateLayoutByIndex()
    {
        ArrangeChildrenByIndex();
    }

    public void UpdateLayoutByName()
    {
        ArrangeChildrenByName();
    }
}
