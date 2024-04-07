using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

public partial class VectorField2D : MonoBehaviour
{
    [Header("For Debugging")] 
    [SerializeField] private bool isShowGroundArea;
    [SerializeField] private bool isShowObstacleArea;
    [SerializeField] private TextMeshPro tileText;
    
    [Space] 
    [SerializeField] private Vector2Int mapSize;
    [SerializeField] private Transform goalTransform;

    [Space] 
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap obstacleTilemap;

    private Vector2 tileWorldOffset;
    private Vector3Int fromGround, fromObstacle, toGround, toObstacle;
    private List<Vector2> obstaclePositions = new();
    private List<Vector2> groundPositions = new();
    private Vector3Int goalIndex;
    
    private Tile[,] fields;
    private int fieldWidth;
    private int fieldHeight;
    
    private readonly List<Vector2Int> directions = new();
    
    private void Start()
    {
        Initialize();
        UpdateGoalPosition();
        CreateFields();
        
        CreateHeatMap();
        
        foreach (var field in fields)
        {
            var tileInfo = Instantiate(tileText);
            tileInfo.transform.position = field.Position;
            tileInfo.text = field.Distance.ToString();
            tileInfo.gameObject.SetActive(true);
        }
    }

    private void Initialize()
    {
        tileWorldOffset = new Vector2(0.5f, 0.5f);
        fromGround = groundTilemap.origin;
        toGround = groundTilemap.size + groundTilemap.origin;

        fromObstacle = obstacleTilemap.origin;
        toObstacle = obstacleTilemap.size + obstacleTilemap.origin;

        var size = groundTilemap.size;
        fields = new Tile[size.x, size.y];
        fieldWidth = size.x;
        fieldHeight = size.y;
        
        SetDirections();
    }

    private void OnDrawGizmos()
    {
        if (obstaclePositions.Count == 0)
        {
            Initialize();
            SetObstaclePositions();
            SetGroundPositions();
        }

        if (isShowObstacleArea)
        {
            Gizmos.color = new Color(1f, .2f, .2f, 0.3f);

            obstaclePositions.ForEach(
                position =>
                {
                    //타일 사이즈가 1x1 일 경우에, 중앙으로 위치하기 위해서 0.5 씩 위치를 더해준다.
                    Gizmos.DrawCube(
                        position,
                        Vector3.one);
                });
        }

        if (isShowGroundArea)
        {
            Gizmos.color = new Color(0.5f, 0.8f, 0.6f, 0.2f);

            groundPositions.ForEach(
                position =>
                {
                    //타일 사이즈가 1x1 일 경우에, 중앙으로 위치하기 위해서 0.5 씩 위치를 더해준다.
                    Gizmos.DrawWireCube(
                        position,
                        Vector3.one);
                });
        }
    }

    /// <summary>
    /// 장애물로 등록한 타일맵에 존재하는 타일들의 위치(월드 좌표)를 저장한다.
    /// </summary>
    private void SetObstaclePositions()
    {
        for (var x = fromObstacle.x; x < toObstacle.x; ++x)
        {
            for (var y = fromObstacle.y; y < toObstacle.y; ++y)
            {
                var tileWorldPosition = obstacleTilemap.transform.position + new Vector3(x, y);
                var tileIndex = obstacleTilemap.LocalToCell(tileWorldPosition);
                var tile = obstacleTilemap.GetTile(tileIndex);

                if (tile == null)
                    continue;

                obstaclePositions.Add((Vector2)tileWorldPosition + tileWorldOffset);
            }
        }
    }

    /// <summary>
    /// 그라운드로 등록한 타일맵에 존재하는 타일들의 위치(월드 좌표)를 저장한다.
    /// </summary>
    private void SetGroundPositions()
    {
        for (var x = fromGround.x; x < toGround.x; ++x)
        {
            for (var y = fromGround.y; y < toGround.y; ++y)
            {
                var tileWorldPosition = groundTilemap.transform.position + new Vector3(x, y);
                var tileIndex = groundTilemap.LocalToCell(tileWorldPosition);
                var tile = groundTilemap.GetTile(tileIndex);

                if (tile == null)
                    continue;
                
                groundPositions.Add((Vector2)tileWorldPosition + tileWorldOffset);
            }
        }
    }
}


public partial class VectorField2D
{
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

    private void UpdateGoalPosition()
    {
        var position = groundTilemap.LocalToCell(groundTilemap.transform.position + goalTransform.position);

        goalIndex = new Vector3Int(fieldWidth / 2, fieldHeight / 2) - position;
    }

    private Queue<Tile> tileQueue = new();
    private void CreateHeatMap()
    {
        tileQueue.Clear();
        fields[goalIndex.x, goalIndex.y].Distance = 0;
        tileQueue.Enqueue(fields[goalIndex.x, goalIndex.y]);
        
        // while (tileQueue.Count > 0)
        // {
        //     var tile = tileQueue.Dequeue();
        //
        //     if (tile.IsBlock)
        //         continue;
        //
        //     SetTileDistance(ref tile);
        // }
    }

    private void SetTileDistance(ref Tile tile)
    {
        var distance = tile.Distance + 1;

        for (var index = 0; index < directions.Count; index++)
        {
            var x = tile.Index.x + directions[index].x;
            var y = tile.Index.y + directions[index].y;

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
        }
    }

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
}