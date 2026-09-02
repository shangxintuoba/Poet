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

    private void ProgressTime(int travelDistance)
    {
        GameTime timeCard = GameManager.Instance != null
            ? GameManager.Instance.TimeCard
            : null;

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

        RefreshVisibleCards();
    }

    private void RefreshVisibleCards()
    {
        Card[] cards = FindObjectsByType<Card>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Card card in cards)
        {
            if (card == null || !card.gameObject.scene.IsValid())
                continue;

            card.gameObject.SetActive(card.ShouldBeVisibleAt(currentNode));
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