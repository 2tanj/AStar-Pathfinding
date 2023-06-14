using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

#pragma warning disable CS0660  // Type defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.Equals(object o)

[Serializable]
public struct NodeType
{
    public NodeType(string n, int w, Color c)
    {
        Name = n;
        Weight = w;
        Color = c;
    }

    [field: SerializeField] public string Name   { get; set; }
    [field: SerializeField] public int    Weight { get; set; }
    [field: SerializeField] public Color  Color  { get; set; }

    public static bool operator ==(NodeType lhs, NodeType rhs)
    {
        return lhs.Name == rhs.Name && lhs.Weight == rhs.Weight && lhs.Color == rhs.Color;
    }
    public static bool operator !=(NodeType lhs, NodeType rhs) => !(lhs == rhs);
}

[RequireComponent(typeof(SpriteRenderer))]
public class Node : MonoBehaviour
{
    private SpriteRenderer _sprite;

    public static readonly NodeType NORMAL_TYPE = 
                        new NodeType("Normal", 0, new Color(0.64f, 0.64f, 0.64f));
    public static readonly NodeType WALL_TYPE = 
                        new NodeType("Wall", 0, Color.black);

    public NodeType Nodetype { get; private set; }

    private Unit _unit;

    [field: Header("Node Colors")]
    [field: SerializeField] public Color MainColor      { get; private set; }
    [field: SerializeField] public Color OffsetColor    { get; private set; }
    [field: SerializeField] public Color OnHoverColor   { get; private set; }

    [field: Header("Pathfinding Colors")]
    [field: SerializeField] public Color NeighbourColor { get; private set; }
    [field: SerializeField] public Color PathColor      { get; private set; }
    [field: SerializeField] public Color ProcessedColor { get; private set; }

    private Color _assignedColor;

    // PATHFINDING
    [Header("Pathfinding variables"), SerializeField]
    private TextMeshProUGUI _gCostText;
    [SerializeField]
    private TextMeshProUGUI _hCostText, _fCostText;
    public float G { get; private set; }
    public float H { get; private set; }
    public float F => G + H;
    public List<Node> Neighbors { get; private set; }
    public Node       Connection { get; private set; }
    // directions of all neighbors
    private static readonly List<Vector2> NEIGHBOR_DIRS = new List<Vector2>() {
            new Vector2(0, 1), new Vector2(-1, 0), new Vector2(0, -1), new Vector2(1, 0),
            new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, -1), new Vector2(-1, 1)
    };


    public SpriteRenderer GetSprite() { return _sprite; }
    public void SetColor(Color color)    => _sprite.color = color;
    public void SetConnection(Node unit) => Connection = unit;
    public void SetType(NodeType type, bool ignoreNormal = true) { 
        Nodetype = type;

        if (type == NORMAL_TYPE && ignoreNormal)
            return; 

        SetColor(type.Color); 
    }
    public void ChangeUnit(Node newNode, Unit unit)
    {
        if (!unit.gameObject.active)
        {
            unit.transform.gameObject.SetActive(true);
            unit.transform.SetParent(newNode.transform);
        }
        unit.ChangePosition(newNode.transform);
        
        _unit = null;
        newNode._unit = unit;
    }

    public void SetG(float g)
    {
        G = g;
        SetText();
    }
    public void SetH(float h)
    {
        H = h;
        SetText();
    }
    private void SetText()
    {
        if (_unit != null)
            return;

        _gCostText.text = G.ToString();
        _hCostText.text = H.ToString();
        _fCostText.text = F.ToString();
    }
    public void ResetNode(Color c)
    {
        SetColor(c);
        _gCostText.text = "";
        _hCostText.text = "";
        _fCostText.text = "";
    }

    private void Awake() => _sprite = GetComponent<SpriteRenderer>();
    private void Update()
    {
        // TODO: make faster, currently very slow
        if (Input.GetMouseButton(1))
        {
            var clickedNode = GridManager.Instance.GetNodeAtLoosePosition
                                (CameraController.GetWorldPosition(0));
            if (clickedNode == null)
                return;

            clickedNode.SetType(GridManager.Instance.SelectedType, false);

            if (!GridManager.Instance.IsStepByStep)
                Pathfinding.FindPathAStar(GridManager.Instance.StartNode, GridManager.Instance.TargetNode);
            else
                AltPathfinding.AStarStepByStep(GridManager.Instance.StartNode, GridManager.Instance.TargetNode);
        }
    }

    public void CacheNeighbors()
    {
        Neighbors = new List<Node>();

        // we respectivly add every dir to our current node and make it a Neighbor if its not null
        foreach (var node in NEIGHBOR_DIRS
                .Select(dir => GridManager.Instance.GetNodeAtExactPosition((Vector2)transform.position + dir))
                .Where(node => node != null))
            Neighbors.Add(node);
    }

    public float GetDistance(Node other)
    {
        var dist = new Vector2Int(Mathf.Abs((int)transform.position.x - (int)other.transform.position.x),
                                  Mathf.Abs((int)transform.position.y - (int)other.transform.position.y));

        var lowest = Mathf.Min(dist.x, dist.y);
        var highest = Mathf.Max(dist.x, dist.y);
        var horizontal = highest - lowest;

        return (lowest * 14 + horizontal * 10) + Nodetype.Weight;
    }

    private void OnMouseEnter()
    {
        _assignedColor = _sprite.color;
        _sprite.color  = OnHoverColor;
    }
    private void OnMouseExit()
    {
        if (_sprite.color != OnHoverColor)
            return;

        _sprite.color = _assignedColor;
    }
    private void OnMouseUp()
    {
        GridManager.Instance.SetStartAndTargetNode(this);

        if (!GridManager.Instance.IsStepByStep)
            Pathfinding.FindPathAStar(GridManager.Instance.StartNode, GridManager.Instance.TargetNode);
        else
            AltPathfinding.AStarStepByStep(GridManager.Instance.StartNode, GridManager.Instance.TargetNode);
    }
}
