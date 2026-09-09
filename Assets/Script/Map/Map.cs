using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{
    [SerializeField] private Node startingNode;
    [SerializeField] private TextManager textManager;
    [SerializeField] private CardManager cardManager;
    [SerializeField, Min(1f)] private float connectionThickness = 4f;
    [SerializeField] private Color connectionColor = Color.black;

    private Node currentNode;
    public Node CurrentNode => currentNode;
    private readonly Dictionary<Node, NodeState> nodeStates = new Dictionary<Node, NodeState>();
    private readonly List<NodeConnection> connections = new List<NodeConnection>();
    private Node[] allNodes;
    private RectTransform connectionRoot;

    private class NodeState
    {
        public string storyState;
        public string displayedText;
    }

    private void Start()
    {
        if (textManager == null)
            textManager = FindFirstObjectByType<TextManager>();
        if (cardManager == null)
            cardManager = FindFirstObjectByType<CardManager>();

        allNodes = GetComponentsInChildren<Node>(true);
        CreateConnections();
        currentNode = startingNode;
        if (currentNode != null)
        {
            currentNode.isUnlocked = true;
            currentNode.SetCurrent(true);
            RefreshVisibleNodes();
            cardManager.RefreshCharactersAtNode(currentNode);
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

        SaveCurrentNodeState();
        currentNode.SetCurrent(false);
        currentNode = destination;
        currentNode.isUnlocked = true;
        currentNode.SetCurrent(true);
        RefreshVisibleNodes();
        cardManager.RefreshCharactersAtNode(currentNode);
        ProgressTime(travelDistance);

        if (currentNode.InkFile != null && textManager != null)
        {
            nodeStates.TryGetValue(currentNode, out NodeState state);
            textManager.LoadStory(
                currentNode.InkFile,
                state != null ? state.storyState : null,
                state != null ? state.displayedText : string.Empty);
        }
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
            bool visible = node == currentNode || (node.isUnlocked && GetTravelDistance(node) > 0);
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

    private void SaveCurrentNodeState()
    {
        if (currentNode == null || textManager == null)
            return;

        if (textManager.IsStoryEnded)
        {
            nodeStates.Remove(currentNode);
            return;
        }

        nodeStates[currentNode] = new NodeState
        {
            storyState = textManager.SaveStoryState(),
            displayedText = textManager.SaveDisplayedText()
        };
    }
}
