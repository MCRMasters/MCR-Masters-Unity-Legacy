using UnityEngine;
using UnityEngine.UI;

public class TsumoTileDisplay : MonoBehaviour
{
    private GridLayoutGroup gridLayoutGroup; // Grid Layout Group 참조
    private GameObject tilePrefab; // 추가할 타일 프리팹
    private float hdOffsetX = 20f; // HD 해상도 기준의 X축 거리
    private float hdOffsetY = 0f;  // HD 해상도 기준의 Y축 거리

    private GameObject lastTsumoTileObject; // 마지막으로 추가된 Tsumo 타일 참조

    // tilePrefab을 동적으로 설정
    public void SetTilePrefab(GameObject prefab)
    {
        tilePrefab = prefab;
        Debug.Log($"Tile prefab assigned: {prefab.name}");
    }

    // GridLayoutGroup이 포함된 GameObject를 설정
    public void SetGridLayoutGroup(GameObject gridObject)
    {
        if (gridObject == null)
        {
            Debug.LogError("GridObject is null. Cannot assign GridLayoutGroup.");
            return;
        }

        gridLayoutGroup = gridObject.GetComponent<GridLayoutGroup>();
        if (gridLayoutGroup == null)
        {
            Debug.LogError("GridLayoutGroup component not found in the provided GameObject.");
        }
        else
        {
            Debug.Log($"GridLayoutGroup assigned from GameObject: {gridObject.name}");
        }
    }

    // 마지막 Tsumo 타일 근처에 새로운 타일 추가
    public void ShowTsumoTile()
    {
        if (gridLayoutGroup == null || tilePrefab == null)
        {
            Debug.LogError("GridLayoutGroup or tilePrefab is not assigned.");
            return;
        }

        int childCount = gridLayoutGroup.transform.childCount;
        if (childCount == 0)
        {
            Debug.LogWarning("No child objects found in the GridLayoutGroup.");
            return;
        }

        RectTransform lastChild = gridLayoutGroup.transform.GetChild(childCount - 1) as RectTransform;
        if (lastChild == null)
        {
            Debug.LogError("Unable to get the RectTransform of the last child.");
            return;
        }

        float scaleX = (float)Screen.width / 1920f;
        float scaleY = (float)Screen.height / 1080f;
        float adjustedOffsetX = hdOffsetX * scaleX;
        float adjustedOffsetY = hdOffsetY * scaleY;

        lastTsumoTileObject = Instantiate(tilePrefab, gridLayoutGroup.transform.parent);
        RectTransform newObjectRect = lastTsumoTileObject.GetComponent<RectTransform>();
        Vector3 lastPosition = lastChild.position;
        Vector3 newPosition = lastPosition + new Vector3(adjustedOffsetX, adjustedOffsetY, 0f);
        newObjectRect.position = newPosition;

        lastTsumoTileObject.transform.SetParent(gridLayoutGroup.transform.parent, true);
        Debug.Log($"Tsumo tile displayed near last at position: {newPosition}");
    }

    // 마지막으로 추가된 Tsumo 타일 제거
    public void HideTsumoTile()
    {
        if (lastTsumoTileObject != null)
        {
            Destroy(lastTsumoTileObject);
            Debug.Log("Last Tsumo tile hidden.");
            lastTsumoTileObject = null;
        }
        else
        {
            Debug.LogWarning("No Tsumo tile to hide.");
        }
    }
}
