using System.Collections.Generic;
using UnityEngine;

public class NodeGrid : MonoBehaviour
{
    public static NodeGrid Instance { get; private set; }

    [Header("Grid")]
    public Vector2 gridWorldSize = new Vector2(30f, 30f);
    public float nodeRadius = 0.5f;
    public LayerMask obstacleLayer;

    private Node[,] grid;
    private float nodeDiameter;
    private int gridSizeX;
    private int gridSizeY;

    private void Awake()
    {
        Instance = this;
        CreateGrid();
    }

    private void CreateGrid()
    {
        nodeDiameter = nodeRadius * 2f;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        grid = new Node[gridSizeX, gridSizeY];

        Vector3 bottomLeft = transform.position
            - Vector3.right * gridWorldSize.x * 0.5f
            - Vector3.forward * gridWorldSize.y * 0.5f;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = bottomLeft
                    + Vector3.right * (x * nodeDiameter + nodeRadius)
                    + Vector3.forward * (y * nodeDiameter + nodeRadius);

                bool walkable = !Physics.CheckSphere(worldPoint, nodeRadius * 0.9f, obstacleLayer);
                grid[x, y] = new Node(walkable, worldPoint, x, y);
            }
        }
    }

    public Node GetNodeFromWorldPoint(Vector3 worldPos)
    {
        float px = Mathf.Clamp01(
            (worldPos.x - transform.position.x + gridWorldSize.x * 0.5f) / gridWorldSize.x);
        float py = Mathf.Clamp01(
            (worldPos.z - transform.position.z + gridWorldSize.y * 0.5f) / gridWorldSize.y);

        int x = Mathf.RoundToInt((gridSizeX - 1) * px);
        int y = Mathf.RoundToInt((gridSizeY - 1) * py);
        return grid[x, y];
    }

    public List<Node> GetNeighbors(Node node)
    {
        var list = new List<Node>();
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = node.gridX + dx;
                int ny = node.gridY + dy;
                if (nx >= 0 && nx < gridSizeX && ny >= 0 && ny < gridSizeY)
                    list.Add(grid[nx, ny]);
            }
        }
        return list;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position,
            new Vector3(gridWorldSize.x, 1f, gridWorldSize.y));

        if (grid == null) return;
        foreach (var node in grid)
        {
            Gizmos.color = node.walkable
                ? new Color(1f, 1f, 1f, 0.04f)
                : new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawCube(node.worldPosition, Vector3.one * (nodeDiameter - 0.05f));
        }
    }
}