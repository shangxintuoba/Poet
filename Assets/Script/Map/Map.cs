using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{
    [SerializeField] private Node startingNode;
    [SerializeField] private TextManager textManager;

    private Node currentNode;
    public Node CurrentNode => currentNode;
    private readonly Dictionary<Node, NodeState> nodeStates = new Dictionary<Node, NodeState>();
    private Node[] allNodes;

    private class NodeState
    {
        public string storyState;
        public string displayedText;
    }

    private void Start()
    {
        if (textManager == null)
            textManager = FindFirstObjectByType<TextManager>();

        allNodes = GetComponentsInChildren<Node>(true);
        currentNode = startingNode;
        if (currentNode != null)
        {
            currentNode.isUnlocked = true;
            currentNode.SetCurrent(true);
            RefreshVisibleNodes();
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
