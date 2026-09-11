using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Map : MonoBehaviour
{
    [SerializeField] private Node startingNode;
    [SerializeField] private TextManager textManager;
    [SerializeField] private CardManager cardManager;
    [SerializeField] private CardLibrary cardLibrary;
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
    private int lastChildNodeVisibilityPeriod = -1;

    private void Start()
    {
        if (textManager == null)
            textManager = FindFirstObjectByType<TextManager>();
        if (cardManager == null)
            cardManager = FindFirstObjectByType<CardManager>();
        if (cardLibrary == null)
            cardLibrary = FindFirstObjectByType<CardLibrary>();
        if (mapScrollView == null)
            mapScrollView = GetComponentInParent<ScrollRect>();

        allNodes = GetComponentsInChildren<Node>(true);
        EnsureNodeArrows();
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

    private void EnsureNodeArrows()
    {
        GameObject arrowTemplate = null;
        foreach (Node node in allNodes)
        {
            if (node != null && node.Arrow != null)
            {
                arrowTemplate = node.Arrow;
                break;
            }
        }

        if (arrowTemplate == null)
        {
            Debug.LogWarning("No node arrow template is assigned on the map.", this);
            return;
        }

        foreach (Node node in allNodes)
            node?.EnsureArrow(arrowTemplate);
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

        if (IsNearbyNode(destination))
            return 1;

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
            bool isNearby = IsNearbyNode(node);
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
            if (from == null)
                continue;

            foreach (Node to in GetConnectionTargets(from))
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

    private bool IsNearbyNode(Node node)
    {
        if (currentNode == null || node == null)
            return false;

        if (currentNode.NearbyNodes != null && System.Array.IndexOf(currentNode.NearbyNodes, node) >= 0)
            return true;

        CardLibrary.NodeData currentNodeData = cardLibrary != null
            ? cardLibrary.FindNodeData(currentNode.Index)
            : null;
        if (currentNodeData == null || currentNodeData.childNodes == null ||
            System.Array.IndexOf(currentNodeData.childNodes, node.Index) < 0)
            return false;

        return !IsChildNodeHidden(currentNodeData, GetCurrentTime());
    }

    private List<Node> GetConnectionTargets(Node from)
    {
        List<Node> targets = new List<Node>();
        if (from.NearbyNodes != null)
        {
            foreach (Node nearby in from.NearbyNodes)
            {
                if (nearby != null && !targets.Contains(nearby))
                    targets.Add(nearby);
            }
        }

        CardLibrary.NodeData nodeData = cardLibrary != null ? cardLibrary.FindNodeData(from.Index) : null;
        foreach (string childNodeId in nodeData?.childNodes ?? System.Array.Empty<string>())
        {
            foreach (Node node in allNodes)
            {
                if (node != null && node.Index == childNodeId && !targets.Contains(node))
                    targets.Add(node);
            }
        }

        return targets;
    }

    private static bool IsChildNodeHidden(CardLibrary.NodeData nodeData, int currentTime)
    {
        if (nodeData.childNodeHidingTimes == null || nodeData.childNodeHidingTimes.Length == 0)
            return false;

        string period = GetChildNodeVisibilityPeriodName(currentTime);
        foreach (string hiddenPeriod in nodeData.childNodeHidingTimes)
        {
            if (string.Equals(hiddenPeriod?.Trim(), period, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private int GetCurrentTime()
    {
        GameTime timeCard = GameManager.Instance != null ? GameManager.Instance.TimeCard : null;
        return timeCard != null ? timeCard.CurrentTime : 0;
    }

    private static int GetChildNodeVisibilityPeriod(int currentTime)
    {
        if (currentTime >= 6 * 60 && currentTime < 13 * 60) return 0;
        if (currentTime >= 13 * 60 && currentTime < 16 * 60) return 1;
        if (currentTime >= 16 * 60 && currentTime < 19 * 60) return 2;
        if (currentTime >= 19 * 60) return 3;
        return 4;
    }

    private static string GetChildNodeVisibilityPeriodName(int currentTime)
    {
        return GetChildNodeVisibilityPeriod(currentTime) switch
        {
            0 => "morning",
            1 => "afternoon",
            2 => "sunset",
            3 => "night",
            _ => "midnight"
        };
    }

    private void Update()
    {
        int visibilityPeriod = GetChildNodeVisibilityPeriod(GetCurrentTime());
        if (visibilityPeriod == lastChildNodeVisibilityPeriod)
            return;

        lastChildNodeVisibilityPeriod = visibilityPeriod;
        if (currentNode != null)
            RefreshVisibleNodes();
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
