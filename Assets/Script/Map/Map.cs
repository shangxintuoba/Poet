using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Map : MonoBehaviour
{
    [SerializeField] private Node startingNode;
    [SerializeField] private TextManager textManager;
    [SerializeField] private CardManager cardManager;
    [SerializeField] private ScrollRect mapScrollView;
    [SerializeField, Min(0f)] private float farNodeCenterDuration = 0.4f;
    [SerializeField, Min(1f)] private float connectionThickness = 4f;
    [SerializeField] private Color connectionColor = Color.black;

    private Node currentNode;
    public Node CurrentNode => currentNode;
    private readonly List<NodeConnection> connections = new List<NodeConnection>();
    private Node[] allNodes;
    private RectTransform connectionRoot;
    private Tween mapScrollTween;

    private void Start()
    {
        if (textManager == null)
            textManager = FindFirstObjectByType<TextManager>();
        if (cardManager == null)
            cardManager = FindFirstObjectByType<CardManager>();
        if (mapScrollView == null)
            mapScrollView = GetComponentInParent<ScrollRect>();

        allNodes = GetComponentsInChildren<Node>(true);
        CreateConnections();
        currentNode = startingNode;
        if (currentNode != null)
        {
            currentNode.isUnlocked = true;
            currentNode.SetCurrent(true);
            RefreshVisibleNodes();
            cardManager.RefreshCharactersAtNode(currentNode);
            ShowCurrentNodeText();
            AudioManager.Instance?.PlayNode(currentNode.Index);
        }
    }

    public void TryGoTo(Node destination)
    {
        if (destination == null || destination == currentNode)
            return;
        if (textManager != null && textManager.IsTyping)
            return;
        int travelDistance = GetTravelDistance(destination);
        if (travelDistance <= 0)
            return;

        currentNode.SetCurrent(false);
        currentNode = destination;
        currentNode.isUnlocked = true;
        currentNode.SetCurrent(true);
        RefreshVisibleNodes();
        cardManager.RefreshCharactersAtNode(currentNode);
        ProgressTime(travelDistance);

        ShowCurrentNodeText();
        AudioManager.Instance?.PlayNode(currentNode.Index);
    }

    public bool TryGoToFarNode(string destinationIndex)
    {
        if (currentNode == null || string.IsNullOrWhiteSpace(destinationIndex) ||
            (textManager != null && textManager.IsTyping))
            return false;

        if (currentNode.FarNodes == null)
            return false;

        foreach (Node.FarConnectedNodes farNode in currentNode.FarNodes)
        {
            if (farNode == null || farNode.node == null || farNode.node.Index != destinationIndex)
                continue;

            TryGoTo(farNode.node);
            if (currentNode == farNode.node)
                CenterMapOnCurrentNode();
            return true;
        }

        return false;
    }

    public void UnlockNodes(IEnumerable<string> nodeIndices)
    {
        if (nodeIndices == null)
            return;

        if (allNodes == null)
            allNodes = GetComponentsInChildren<Node>(true);

        bool changed = false;
        foreach (string nodeIndex in nodeIndices)
        {
            if (string.IsNullOrWhiteSpace(nodeIndex))
                continue;

            foreach (Node node in allNodes)
            {
                if (node != null && node.Index == nodeIndex)
                {
                    node.isUnlocked = true;
                    changed = true;
                    break;
                }
            }
        }

        if (changed && currentNode != null)
            RefreshVisibleNodes();
    }
    private void ProgressTime(int travelDistance)
    {
        GameTime timeCard = GameManager.Instance.TimeCard;

        if (timeCard == null)
            timeCard = FindFirstObjectByType<GameTime>();

        timeCard?.TimeProgress(travelDistance);
    }

    private int GetTravelDistance(Node destination)
    {
        if (currentNode == null)
            return destination == startingNode ? 1 : 0;

        foreach (Node nearby in currentNode.NearbyNodes)
        {
            if (nearby == destination)
                return 1;
        }

        foreach (Node.FarConnectedNodes farNode in currentNode.FarNodes)
        {
            if (farNode != null && farNode.node == destination)
                return Mathf.Max(1, farNode.distance);
        }

        return 0;
    }

    private void RefreshVisibleNodes()
    {
        foreach (Node node in allNodes)
        {
            if (node == null) continue;
            bool isNearby = currentNode != null && currentNode.NearbyNodes != null &&
                            System.Array.IndexOf(currentNode.NearbyNodes, node) >= 0;
            bool visible = node == currentNode || (node.isUnlocked && isNearby);
            node.gameObject.SetActive(visible);
        }

        foreach (NodeConnection connection in connections)
            connection?.Refresh();
    }

    private void CreateConnections()
    {
        if (allNodes == null || allNodes.Length == 0)
            return;

        GameObject rootObject = new GameObject("Node Connections", typeof(RectTransform));
        connectionRoot = rootObject.GetComponent<RectTransform>();
        connectionRoot.SetParent(transform, false);
        connectionRoot.anchorMin = Vector2.zero;
        connectionRoot.anchorMax = Vector2.one;
        connectionRoot.offsetMin = Vector2.zero;
        connectionRoot.offsetMax = Vector2.zero;
        connectionRoot.SetAsFirstSibling();

        HashSet<string> createdPairs = new HashSet<string>();
        foreach (Node from in allNodes)
        {
            if (from == null || from.NearbyNodes == null)
                continue;

            foreach (Node to in from.NearbyNodes)
            {
                if (to == null || to == from)
                    continue;

                int fromId = from.GetInstanceID();
                int toId = to.GetInstanceID();
                string pairKey = fromId < toId
                    ? fromId + ":" + toId
                    : toId + ":" + fromId;
                if (!createdPairs.Add(pairKey))
                    continue;

                GameObject lineObject = new GameObject(
                    "Connection " + from.Index + " - " + to.Index,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(UnityEngine.UI.Image),
                    typeof(NodeConnection));
                lineObject.transform.SetParent(connectionRoot, false);

                NodeConnection connection = lineObject.GetComponent<NodeConnection>();
                connection.Initialize(from, to, connectionRoot, connectionThickness, connectionColor);
                connections.Add(connection);
            }
        }
    }

    private void ShowCurrentNodeText()
    {
        if (currentNode == null || textManager == null)
            return;

        GameTime timeCard = GameManager.Instance != null ? GameManager.Instance.TimeCard : null;
        textManager.ShowNode(currentNode.Index, timeCard != null ? timeCard.CurrentTime : 0);
    }

    private void CenterMapOnCurrentNode()
    {
        if (mapScrollView == null || mapScrollView.content == null || mapScrollView.viewport == null || currentNode == null)
            return;

        RectTransform content = mapScrollView.content;
        RectTransform viewport = mapScrollView.viewport;
        RectTransform nodeRect = currentNode.transform as RectTransform;
        if (nodeRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        Vector2 nodePosition = (Vector2)content.InverseTransformPoint(nodeRect.TransformPoint(nodeRect.rect.center));
        Vector2 viewportCenter = (Vector2)content.InverseTransformPoint(viewport.TransformPoint(viewport.rect.center));
        Vector2 targetPosition = content.anchoredPosition + viewportCenter - nodePosition;

        mapScrollTween?.Kill();
        mapScrollView.StopMovement();
        mapScrollTween = content.DOAnchorPos(targetPosition, farNodeCenterDuration).SetEase(Ease.OutQuad);
    }

    private void OnDestroy()
    {
        mapScrollTween?.Kill();
    }
}
