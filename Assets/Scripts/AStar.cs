using System.Collections.Generic;
using UnityEngine;

public static class AStar
{
    public static List<Vector3> FindPath(Vector3 startPos, Vector3 endPos)
    {
        if (NodeGrid.Instance == null) return null;

        Node startNode = NodeGrid.Instance.GetNodeFromWorldPoint(startPos);
        Node endNode = NodeGrid.Instance.GetNodeFromWorldPoint(endPos);

        if (!endNode.walkable) return null;

        var openSet = new List<Node>();
        var closedSet = new HashSet<Node>();

        startNode.gCost = 0;
        startNode.hCost = GetDistance(startNode, endNode);
        startNode.parent = null;
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node current = GetLowestFCost(openSet);

            if (current == endNode)
                return RetracePath(startNode, endNode);

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Node neighbor in NodeGrid.Instance.GetNeighbors(current))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor)) continue;

                int newG = current.gCost + GetDistance(current, neighbor);
                if (newG < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newG;
                    neighbor.hCost = GetDistance(neighbor, endNode);
                    neighbor.parent = current;
                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        return null;
    }

    private static Node GetLowestFCost(List<Node> list)
    {
        Node best = list[0];
        for (int i = 1; i < list.Count; i++)
        {
            if (list[i].fCost < best.fCost ||
               (list[i].fCost == best.fCost && list[i].hCost < best.hCost))
                best = list[i];
        }
        return best;
    }

    private static List<Vector3> RetracePath(Node start, Node end)
    {
        var path = new List<Node>();
        Node current = end;
        while (current != start)
        {
            path.Add(current);
            current = current.parent;
        }
        path.Reverse();

        var result = new List<Vector3>(path.Count);
        foreach (var n in path) result.Add(n.worldPosition);
        return result;
    }

    private static int GetDistance(Node a, Node b)
    {
        int dx = Mathf.Abs(a.gridX - b.gridX);
        int dy = Mathf.Abs(a.gridY - b.gridY);
        return dx > dy ? 14 * dy + 10 * (dx - dy) : 14 * dx + 10 * (dy - dx);
    }
}