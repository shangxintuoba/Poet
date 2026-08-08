using UnityEngine;

public class Map : MonoBehaviour
{
    //The map is a group of nodes connected to each other, it shows on UI as some blocks connected to each other with lines.
    //when that block is clicked, it will expand and shows its childs ( also nodes, which are subnode/ subdestination of that)
    //if the node does not have any Children, when clicked, that node will be set as current node (meaning player will go to that position)
    //all children of one parents are nearby node of each other
    //player can only go to nearby node. If player clicked a non-nearby node, there will be no response
    //player cannot clicked and go to other node when the text is typing.
    //the state of that textblock will be saved when player goto other node

    private node CurrentNode;
    public GameObject NodePrefab;
    public GameObject LinePrefab;





    public void GoTo(node destination)
    {
        //if destination is a nearbynodes to current node

        CurrentNode = destination;
    }

}

public class node
{
    public bool isUnlocked;
    public string name;
    public node[] NearbyNodes;
    public node[] Children;
    public node[] Parents;


}