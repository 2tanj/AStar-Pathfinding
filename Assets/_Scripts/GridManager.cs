using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// domace iz analize (poslednji i dijagram)
// domace iz ai, apstrakt

// BUG: kada je node start ili target vise se nikad ne ispisuju brojevi tu
public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [SerializeField] private Node _nodePrefab;

    // references to the prefabs
    [SerializeField] private Unit _startUnitPrefab;
    [SerializeField] private Unit _targetUnitPrefab;

    // references to the objects (we instantiate prefabs into these objects)
    public Unit StartUnit  { get; set; }
    public Unit TargetUnit { get; set; }

    [SerializeField]
    private Button _stepByStepBtn;
    public bool IsStepByStep { get; private set; } = false;

    // start = false, target = true
    public bool StartOrTargetSelected { get; private set; } = false;
    private static bool _isStartSet; // so we dont set the start multiple times on beginning
    public Node StartNode { get; private set; }
    public Node TargetNode { get; private set; }


    [SerializeField]
    private int _width = 5, _height = 5;

    [field: SerializeField]
    public List<NodeType> NodeTypes { get; private set; }

    public NodeType SelectedType { get; private set; }

    private List<Node> _nodes;

    public void SetStartAndTargetNode(Node n)
    {
        if (!StartOrTargetSelected)
            SetStartNode(n);
        else
            SetTargetNode(n);
    }
    private void SetStartNode(Node n)
    {
        StartNode = n;
        n.ChangeUnit(n, StartUnit);
    }
    private void SetTargetNode(Node n)
    {
        TargetNode = n;
        n.ChangeUnit(n, TargetUnit);
    }

    private void Awake() => Instance = this;
    void Start()
    {
        NodeTypes.Add(Node.NORMAL_TYPE);
        NodeTypes.Add(Node.WALL_TYPE);

        SelectedType = Node.NORMAL_TYPE;

        _nodes = new List<Node>();
        DrawGrid();

        InitDropdown();
    }

    public void DrawGrid()
    {
        if (!(_nodes.Count <= 1))
        {
            _nodes.ForEach(s => Destroy(s.gameObject));
            _nodes.Clear();
        }
        
        _isStartSet = false;

        InitNodes();
        Debug.Log(_nodes.Count);

        // moving the camera to the middle of the grid
        var pos = new Vector3(((float)_width / 2) - .5f, ((float)_height / 2) - .5f, 
                               _width > _height ? -_width : -_height);
        Camera.main.transform.position = pos;
    }

    private void InitNodes()
    {
        for (int x = 0; x < _width; x++)
            for (int y = 0; y < _height; y++)
            {
                var node = Instantiate(_nodePrefab, new Vector3(x, y, 0),
                    Quaternion.identity, transform);

                node.name = $"Node {x} {y}";
                node.SetColor(((x + y) % 2 == 0) ? node.MainColor : node.OffsetColor);
                node.SetType(Node.NORMAL_TYPE);

                _nodes.Add(node);
            }

        // we do this after the nested loop because all neighbours of a node are not already initialised
        _nodes.ForEach(n => n.CacheNeighbors());

        if (!_isStartSet)
        {
            InitUnits();
            _isStartSet = true;
        }
    }

    public void RandomizeNodes()
    {
        // randomizing all nodes
        foreach (var node in _nodes)
        {
            var type = NodeTypes[Random.Range(0, NodeTypes.Count)];
            node.SetType(type, false);
            node.ResetNode(node.Nodetype.Color);
        }

        // setting the start and target nodes by getting two random non-wall nodes
        var noWalls = _nodes.Where(node => node.Nodetype != Node.WALL_TYPE).ToList();

        var startNode = noWalls[Random.Range(0, noWalls.Count)];
        Node targetNode;
        // making sure the nodes arent the same
        do
            targetNode = noWalls[Random.Range(0, noWalls.Count)];
        while (targetNode == startNode);

        SetStartNode(startNode);
        SetTargetNode(targetNode);

        if (!IsStepByStep)
            Pathfinding.FindPathAStar(StartNode, TargetNode);
        else
            AltPathfinding.AStarStepByStep(startNode, TargetNode);
    }

    // resetAll=true resets all types: walls, sand...
    public void ResetNodes(bool resetAll = false)
    {
        int last = -1;
        for (int i = 0; i < _nodes.Count; i++)
        {
            // normalColor creates a random pattern(depending on the size of the board)
            // which is used only for nodes of types normal
            // if a node is not of type normal we just get the color of its type
            var normalColor = (i+last) % 2 == 0 ? _nodes[i].MainColor : _nodes[i].OffsetColor;
            var finalColor = _nodes[i].Nodetype == Node.NORMAL_TYPE ? normalColor : _nodes[i].Nodetype.Color;

            if (resetAll)
            {
                 _nodes[i].SetType(Node.NORMAL_TYPE);
                 _nodes[i].ResetNode(normalColor);
            }
            else _nodes[i].ResetNode(finalColor);

            if (i % 2 == 0) last++;
        }
    }

    public Node GetNodeAtExactPosition(Vector2 pos)
    {
        foreach (var r in _nodes)
            if ((Vector2)r.transform.position == pos)
                return r;

        return null;
    }
    public Node GetNodeAtLoosePosition(Vector2 pos)
    {
        foreach (var r in _nodes)
            if (pos.x <= r.transform.position.x + .5f && pos.x >= r.transform.position.x - .5f && 
                pos.y <= r.transform.position.y + .5f && pos.y >= r.transform.position.y - .5f)
                    return r;

        return null;
    }

    // function for changing the x and y values with sliders
    // if value is less then 0, we change the y value by inverting
    // if the value is more then 0 we change the x value
    // done like this for the sole purpose of not making more functions and duplicating code
    public void OnSliderValueChanged(Slider s)
    {
        int newSize = s.value > 0 ? (int)s.value : (int)s.value * -1;
        
        if (s.value > 0) _width  = newSize;
        else             _height = newSize;

        DrawGrid();
    }
    public void OnDropdownValueChanged(TMP_Dropdown d)
    {
        SelectedType = 
            NodeTypes.Single(type => type.Name == d.options[d.value].text);
    }
    public void OnResetGridButtonClicked()
    {
        ResetNodes(true);
    }
    public void OnToggleValueChanged(Toggle t)
    {
        IsStepByStep = t.isOn;
        _stepByStepBtn.gameObject.SetActive(IsStepByStep);

        if (IsStepByStep)
        {
            if (TargetNode == null || StartNode == null)
                return;

            AltPathfinding.AStarStepByStep(StartNode, TargetNode);
        }
        else
            Pathfinding.FindPathAStar(StartNode, TargetNode);
    }
    public void StepByStepShowcase()
    {
        if (TargetNode == null || StartNode == null)
            return;

        AltPathfinding.AStarStepByStep(StartNode, TargetNode, false);
    }
    public void StartOrTargetToggle()
    {
        StartOrTargetSelected = !StartOrTargetSelected;
    }

    private void InitDropdown()
    {
        var dropdown = FindObjectOfType<TMP_Dropdown>();
        // initializing the options from the NodeTypes list
        NodeTypes.ForEach(type => dropdown.options.Add(new TMP_Dropdown.OptionData(type.Name)));

        // initializing the default selected value
        for (int i = 0; i < dropdown.options.Count; i++)
            if (dropdown.options[i].text == "Normal")
                dropdown.value = i;
    }

    private void InitUnits()
    {
        if (StartUnit == null || TargetUnit == null)
        {
            StartUnit = Instantiate(_startUnitPrefab, Vector3.zero, Quaternion.identity, transform);
            TargetUnit = Instantiate(_targetUnitPrefab, Vector3.zero, Quaternion.identity, transform);
        }
        StartUnit.gameObject.SetActive(false);
        TargetUnit.gameObject.SetActive(false);

        SetStartAndTargetNode(_nodes.First()); // setting the first node as starting
        if (!StartOrTargetSelected) StartOrTargetSelected = true;
    }
}
