using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// scripts thats used for step by step demonstration
public class AltPathfinding : MonoBehaviour
{
    static List<Node> toSearch;
    static List<Node> searched;
    static Node current;

    public static void AStarStepByStep(Node start, Node target, bool beginning = true)
    {
        if (start == null || target == null)
            return;

        if (!beginning)
            AStarHelper(start, target);
        else
        {
            GridManager.Instance.ResetNodes();

            toSearch = new List<Node>();
            searched = new List<Node>();
            current = null;

            toSearch.Add(start);

            AStarHelper(start, target);
        }
    }

    public static void AStarHelper(Node start, Node target)
    {
        if (current == target)
            return;

        current = toSearch[0];
        foreach (var node in toSearch)
            if (node.F < current.F || node.F == current.F && node.H < current.H)
                current = node;

        searched.Add(current);
        toSearch.Remove(current);
        current.SetColor(current.ProcessedColor);

        if (current == target)
        {
            var currentPathTile = target;
            var path = new List<Node>();

            while (currentPathTile != start)
            {
                path.Add(currentPathTile);
                currentPathTile = currentPathTile.Connection;
            }

            path.ForEach(node => node.SetColor(node.PathColor));
            start.SetColor(start.PathColor);
        }

        foreach (var neighbor in current.Neighbors.Where(node => !(node.Nodetype.Name == "Wall") && !searched.Contains(node)))
        {
            bool inSearch = toSearch.Contains(neighbor);
            var costToNeighbor = current.G + current.GetDistance(neighbor);

            if (!inSearch || costToNeighbor < neighbor.G)
            {
                neighbor.SetG(costToNeighbor);
                neighbor.SetConnection(current);

                if (!inSearch)
                {
                    neighbor.SetH(neighbor.GetDistance(target));
                    toSearch.Add(neighbor);
                    neighbor.SetColor(neighbor.NeighbourColor);
                }
            }
        }
    }
}
