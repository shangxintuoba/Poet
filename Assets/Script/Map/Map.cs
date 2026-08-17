using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{
    [SerializeField] private Node startingNode;
    [SerializeField] private TextManager textManager;

    private Node currentNode;
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
        if (!IsNearby(destination))
            return;

        SaveCurrentNodeState();
        currentNode.SetCurrent(false);
        currentNode = destination;
        currentNode.isUnlocked = true;
        currentNode.SetCurrent(true);
        RefreshVisibleNodes();

        if (currentNode.InkFile != null && textManager != null)
        {
            nodeStates.TryGetValue(currentNode, out NodeState state);
            textManager.LoadStory(
                currentNode.InkFile,
                state != null ? state.storyState : null,
                state != null ? state.displayedText : string.Empty);
        }
    }

    private bool IsNearby(Node destination)
    {
        if (currentNode == null)
            return destination == startingNode;

        foreach (Node nearby in currentNode.NearbyNodes)
        {
            if (nearby == destination)
                return true;
        }
        return false;
    }

    private void RefreshVisibleNodes()
    {
        foreach (Node node in allNodes)
        {
            if (node == null) continue;
            bool visible = node == currentNode || IsNearby(node);
            node.gameObject.SetActive(visible);
        }
    }

    private void SaveCurrentNodeState()
    {
        if (currentNode == null || textManager == null)
            return;

        nodeStates[currentNode] = new NodeState
        {
            storyState = textManager.SaveStoryState(),
            displayedText = textManager.SaveDisplayedText()
        };
    }
}