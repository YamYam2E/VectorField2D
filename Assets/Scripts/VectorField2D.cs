using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using Button = UnityEngine.UI.Button;

public partial class VectorField2D : MonoBehaviour
{
    [SerializeField] private TextMeshPro tileText;
    [SerializeField] private Transform tileArrow;
    [SerializeField] private Chaser chaser;
    
    [Space] 
    [SerializeField] private Vector2Int mapSize;
    [SerializeField] private Transform goalTransform;

    [Space] 
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap obstacleTilemap;

    [Space] 
    [Range(0f, 5f)] public float searchingTime;
    [SerializeField] private Button createHeatmapButton;
    [SerializeField] private Button drawVectorFieldButton;
    [SerializeField] private Button createChaserButton;
    
    private readonly Vector2 tileWorldOffset = new(0.5f, 0.5f);
    private readonly List<Vector2Int> directions = new();
    
    private Vector3Int fromGround, fromObstacle, toGround, toObstacle;
    private Vector3Int goalIndex;

    private Tile[,] fields;
    private int fieldWidth;
    private int fieldHeight;
    private Queue<Tile> tileQueue = new();
    
    private Dictionary<Vector2Int, TextMeshPro> drawTiles = new();
    private Dictionary<Vector2Int, Transform> drawArrows = new();
}

public partial class VectorField2D
{
    private void Start()
    {
        createHeatmapButton.onClick.AddListener(OnClickCreateHeatmap);
        drawVectorFieldButton.onClick.AddListener(OnClickCreateVectorField);
        createChaserButton.onClick.AddListener(OnClickCreateChaser);
        Initialize();
        CreateFields();
        CreateDrawObjects();
    }

    private void Initialize()
    {
        fromGround = groundTilemap.origin;
        toGround = groundTilemap.size + groundTilemap.origin;

        fromObstacle = obstacleTilemap.origin;
        toObstacle = obstacleTilemap.size + obstacleTilemap.origin;

        var size = groundTilemap.size;
        fields = new Tile[size.x, size.y];
        fieldWidth = size.x;
        fieldHeight = size.y;

        Debug.Log($"Initialize :::\n ->Field size [{fieldWidth} X {fieldHeight}]");

        SetDirections();
    }
    
    /// <summary>
    /// 필드의 가중치를 적용하기 위해 TileMap 기준 인덱스 및 포지션 값을 저장한 배열을 만든다.
    /// </summary>
    private void CreateFields()
    {
        var fieldX = 0;
        var fieldY = 0;

        for (var x = fromGround.x; x < toGround.x; ++x)
        {
            for (var y = fromGround.y; y < toGround.y; ++y)
            {
                // * Ground
                var tileWorldPosition = groundTilemap.transform.position + new Vector3(x, y);
                var tileIndex = groundTilemap.LocalToCell(tileWorldPosition);
                var groundTile = groundTilemap.GetTile(tileIndex);
                var obstacleTile = obstacleTilemap.GetTile(tileIndex);

                if (groundTile == null)
                    continue;

                fields[fieldX, fieldY] = new Tile
                {
                    Index = new Vector2Int(fieldX, fieldY),
                    Position = (Vector2)tileWorldPosition + tileWorldOffset,
                    Direction = Vector2.zero,
                    Distance = -1,
                    IsBlock = obstacleTile != null
                };
                fieldY += 1;
            }

            fieldX += 1;
            fieldY = 0;
        }
    }

    private void CreateDrawObjects()
    {
        foreach (var field in fields)
        {
            // Create Draw Tile for distance
            var tileInfo = Instantiate(tileText);
            tileInfo.transform.position = field.Position;
            tileInfo.text = $"{field.Distance}";
            drawTiles.Add(field.Index, tileInfo);

            // Create Draw arrow for direction
            var arrow = Instantiate(tileArrow);
            arrow.position = field.Position;
            drawArrows.Add(field.Index, arrow);
        }
    }

    private void UpdateGoalPosition()
    {
        var position = groundTilemap.LocalToCell(groundTilemap.transform.position + goalTransform.position);
        goalIndex = position + new Vector3Int(fieldWidth / 2, fieldHeight / 2);
    }

    #region 1. Heatmap

    private void OnClickCreateHeatmap()
    {
        tileQueue.Clear();
        foreach (var tile in drawTiles)
            tile.Value.gameObject.SetActive(false);

        foreach (var arrow in drawArrows)
            arrow.Value.gameObject.SetActive(false);
        
        foreach (var field in fields)
            field.Distance = -1;

        createHeatmapButton.interactable = false;
        
        UpdateGoalPosition();
        StartCoroutine(CreateTileHeatmap_internal());
    }

    private IEnumerator CreateTileHeatmap_internal()
    {
        fields[goalIndex.x, goalIndex.y].Distance = 0;
        drawTiles[fields[goalIndex.x, goalIndex.y].Index].text = $"{fields[goalIndex.x, goalIndex.y].Distance}";
        drawTiles[fields[goalIndex.x, goalIndex.y].Index].gameObject.SetActive(true);

        tileQueue.Enqueue(fields[goalIndex.x, goalIndex.y]);

        while (tileQueue.Count > 0)
        {
            var tile = tileQueue.Dequeue();

            if (tile.IsBlock)
                continue;

            SetTileDistance(ref tile);

            if (searchingTime > 0f)
                yield return new WaitForSeconds(searchingTime);
        }

        createHeatmapButton.interactable = true;
    }

    private void SetTileDistance(ref Tile tile)
    {
        var distance = tile.Distance + 1;

        for (var index = 0; index < directions.Count; index++)
        {
            if (directions[index].x + directions[index].y == 2 ||
                directions[index].x + directions[index].y == 0 ||
                directions[index].x + directions[index].y == -2)
                continue;

            var x = tile.Index.x + directions[index].x;
            var y = tile.Index.y + directions[index].y;

            if (x == goalIndex.x && y == goalIndex.y)
                continue;

            if (x < 0 || x >= groundTilemap.size.x || y < 0 || y >= groundTilemap.size.y)
                continue;

            if (fields[x, y].IsBlock)
                continue;

            if (fields[x, y].Distance > -1)
            {
                if (fields[x, y].Distance > distance)
                    fields[x, y].Distance = distance;
            }
            else
            {
                fields[x, y].Distance = distance;
                tileQueue.Enqueue(fields[x, y]);
            }

            drawTiles[fields[x, y].Index].text = $"{fields[x, y].Distance}";
            drawTiles[fields[x, y].Index].gameObject.SetActive(true);
        }
    }

    #endregion

    #region 2. VectorField

    private void OnClickCreateVectorField()
    {
        foreach (var tile in drawTiles)
            tile.Value.gameObject.SetActive(false);
        
        foreach (var arrow in drawArrows)
            arrow.Value.gameObject.SetActive(false);
        
        foreach (var field in fields)
        {
            var tile = field;
            SetTileVector(ref tile);
        }
    }

    private void SetTileVector(ref Tile tile)
    {
        var nearBlockTile = false;
        var minimumDistance = 9999;
        var minimumDirection = Vector2.zero;
        var direction = Vector2.zero;

        for (var index = 0; index < directions.Count; index++)
        {
            var x = tile.Index.x + directions[index].x;
            var y = tile.Index.y + directions[index].y;

            if (x < 0 || x >= groundTilemap.size.x || y < 0 || y >= groundTilemap.size.y)
                continue;

            if (fields[x, y].IsBlock)
                nearBlockTile = true;

            direction += fields[x, y].Distance * directions[index];

            if (fields[x, y].Distance == -1 || fields[x, y].Distance >= minimumDistance)
                continue;

            minimumDistance = fields[x, y].Distance;
            minimumDirection = directions[index];
        }

        if (nearBlockTile)
            tile.Direction = minimumDirection;
        else
            tile.Direction = -direction;
        
        SetArrowAngle(ref tile);
    }

    private void SetArrowAngle(ref Tile tile)
    {
        var arrow = drawArrows[tile.Index];
        
        var angle = Math.Atan2(tile.Direction.y, tile.Direction.x) * (180f / Math.PI);
        arrow.eulerAngles = new Vector3(0, 0, (float)angle);
        arrow.gameObject.SetActive(true);
    }

    #endregion

    #region 3. Create Chaser

    private void OnClickCreateChaser()
    {
        chaser.gameObject.SetActive(true);
    }

    #endregion
    private void SetDirections()
    {
        if (directions.Count != 0)
            return;

        directions.Add(new Vector2Int { x = 1, y = 0 });
        directions.Add(new Vector2Int { x = -1, y = 0 });
        directions.Add(new Vector2Int { x = 0, y = 1 });
        directions.Add(new Vector2Int { x = 0, y = -1 });
        directions.Add(new Vector2Int { x = 1, y = 1 });
        directions.Add(new Vector2Int { x = 1, y = -1 });
        directions.Add(new Vector2Int { x = -1, y = -1 });
        directions.Add(new Vector2Int { x = -1, y = 1 });
    }

    public Vector2 GetDirection(Vector3 position)
    {
        var tilePosition = groundTilemap.LocalToCell(groundTilemap.transform.position + position);
        var tileIndex = tilePosition + new Vector3Int(fieldWidth / 2, fieldHeight / 2);
        return fields[tileIndex.x, tileIndex.y].Direction;
    }
}