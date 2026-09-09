using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws one UI line between two map nodes. The line is only visible while
/// both endpoints are visible in the hierarchy.
/// </summary>
[RequireComponent(typeof(RectTransform), typeof(Image))]
public sealed class NodeConnection : MonoBehaviour
{
    private Node from;
    private Node to;
    private RectTransform root;
    private RectTransform lineRect;
    private float thickness;

    public void Initialize(Node fromNode, Node toNode, RectTransform lineRoot,
        float lineThickness, Color lineColor)
    {
        from = fromNode;
        to = toNode;
        root = lineRoot;
        thickness = lineThickness;
        lineRect = transform as RectTransform;

        Image image = GetComponent<Image>();
        image.color = lineColor;
        image.raycastTarget = false;

        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.pivot = new Vector2(0.5f, 0.5f);

        Refresh();
    }

    public void Refresh()
    {
        if (from == null || to == null || root == null || lineRect == null)
        {
            gameObject.SetActive(false);
            return;
        }

        bool visible = from.gameObject.activeInHierarchy && to.gameObject.activeInHierarchy;
        gameObject.SetActive(visible);
        if (!visible)
            return;

        Vector3 fromLocal = root.InverseTransformPoint(from.transform.position);
        Vector3 toLocal = root.InverseTransformPoint(to.transform.position);
        Vector2 start = new Vector2(fromLocal.x, fromLocal.y);
        Vector2 end = new Vector2(toLocal.x, toLocal.y);
        Vector2 direction = end - start;

        lineRect.anchoredPosition = (start + end) * 0.5f;
        lineRect.sizeDelta = new Vector2(direction.magnitude, thickness);
        lineRect.localRotation = Quaternion.Euler(
            0f,
            0f,
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }
}
