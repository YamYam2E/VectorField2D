using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public partial class VectorField2D : MonoBehaviour
{
    [SerializeField] private bool isShowTileArrow;
    [SerializeField] private bool isShowTileDistance;
    
    [SerializeField] private TextMeshPro tileText;
    [SerializeField] private Transform tileArrow;
    [SerializeField] private Chaser chaser;
    
    [Space] 
    [SerializeField] private Vector2Int mapSize;

    [Space] 
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap obstacleTilemap;

    private readonly Vector2 tileWorldOffset = new(0.5f, 0.5f);
    private readonly List<Vector2Int> directions = new();
    
    private Vector3Int fromGround, toGround;
    private Vector2Int goalIndex;

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
        Initialize();
        CreateFields();
        CreateChaser(800);
        CreateDrawObjects();
        
        UpdateGoalPosition();
    }

    private void Initialize()
    {
        fromGround = groundTilemap.origin;
        toGround = groundTilemap.size + groundTilemap.origin;

        var size = groundTilemap.size;
        fields = new Tile[size.x, size.y];
        fieldWidth = size.x;
        fieldHeight = size.y;

        Debug.Log($"Initialize :::\n ->Field size [{fieldWidth} X {fieldHeight}]");

        SetDirections();
    }
    
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
        var position = groundTilemap.WorldToCell(groundTilemap.transform.position);
        goalIndex = (Vector2Int)position + new Vector2Int(fieldWidth / 2, fieldHeight / 2);
    }

    #region 1. Heatmap

    private void CreateHeatMap()
    {
        tileQueue.Clear();
        foreach (var tile in drawTiles)
            tile.Value.gameObject.SetActive(false);

        foreach (var arrow in drawArrows)
            arrow.Value.gameObject.SetActive(false);

        foreach (var field in fields)
            field.Distance = -1;

        CreateHeatMap_internal(isShowTileDistance);
    }

    private void CreateHeatMap_internal(bool drawDistance)
    {
        if (goalIndex.x < 0 || goalIndex.x >= fieldWidth || goalIndex.y < 0 || goalIndex.y >= fieldHeight)
            return;

        fields[goalIndex.x, goalIndex.y].Distance = 0;

        if (drawDistance)
        {
            drawTiles[fields[goalIndex.x, goalIndex.y].Index].color = Color.red;
            drawTiles[fields[goalIndex.x, goalIndex.y].Index].text = $"{fields[goalIndex.x, goalIndex.y].Distance}";
        }

        drawTiles[fields[goalIndex.x, goalIndex.y].Index].gameObject.SetActive(drawDistance);

        tileQueue.Enqueue(fields[goalIndex.x, goalIndex.y]);

        while (tileQueue.Count > 0)
        {
            var tile = tileQueue.Dequeue();

            if (tile.IsBlock)
                continue;

            SetTileDistance(ref tile, drawDistance);
        }
    }

    private void SetTileDistance(ref Tile tile, bool drawDistance = false)
    {
        var distance = tile.Distance + 1;

        for (var index = 0; index < directions.Count; index++)
        {
            var nearTileIndex = tile.Index + directions[index];

            if (nearTileIndex == goalIndex)
                continue;

            if (nearTileIndex.x < 0 || nearTileIndex.x >= groundTilemap.size.x || nearTileIndex.y < 0 || nearTileIndex.y >= groundTilemap.size.y)
                continue;

            var nearTile = fields[nearTileIndex.x, nearTileIndex.y];
            
            if (nearTile.IsBlock)
                continue;

            if (nearTile.Distance > -1)
            {
                if (nearTile.Distance > distance)
                    nearTile.Distance = distance;
            }
            else
            {
                nearTile.Distance = distance;
                tileQueue.Enqueue(nearTile);
            }

            if (drawDistance)
            {
                drawTiles[nearTile.Index].color = Color.white;
                drawTiles[nearTile.Index].text = $"{nearTile.Distance}";
            }

            drawTiles[nearTile.Index].gameObject.SetActive(drawDistance);
        }
    }

    #endregion

    #region 2. Vector Field

    private void CreateVectorField()
    {
        // foreach (var tile in drawTiles)
        //     tile.Value.gameObject.SetActive(false);
        
        foreach (var arrow in drawArrows)
            arrow.Value.gameObject.SetActive(false);
        
        foreach (var field in fields)
        {
            if (field.IsBlock)
                continue;
            
            var tile = field;
            SetTileVector(ref tile);
        }
    }

    private void SetTileVector(ref Tile tile)
    {
        var direction = Vector2.zero;
        var isNearObstacle = false;

        for (var index = 0; index < directions.Count; index++)
        {
            var x = tile.Index.x + directions[index].x;
            var y = tile.Index.y + directions[index].y;

            if (!fields[x, y].IsBlock) 
                continue;
            
            isNearObstacle = true;
            break;
        }

        if (isNearObstacle)
        {
            var minimumDistance = int.MaxValue;
            
            for (var index = 0; index < directions.Count; index++)
            {
                var x = tile.Index.x + directions[index].x;
                var y = tile.Index.y + directions[index].y;
        
                if (x < 0 || x >= groundTilemap.size.x || y < 0 || y >= groundTilemap.size.y)
                    continue;
        
                if (fields[x, y].IsBlock)
                    continue;
        
                if (minimumDistance > fields[x, y].Distance)
                {
                    minimumDistance = fields[x, y].Distance;
                    direction = -directions[index];
                }      
            }
        }
        else
        {
            for (var index = 0; index < directions.Count; index++)
            {
                var x = tile.Index.x + directions[index].x;
                var y = tile.Index.y + directions[index].y;

                if (x < 0 || x >= groundTilemap.size.x || y < 0 || y >= groundTilemap.size.y)
                    continue;

                direction += fields[x, y].Distance * directions[index];
            }
        }

        if (tile.Index != goalIndex && direction == Vector2.zero)
        {
            // 8방향 모든 벡터의 가중치를 합하다보면
            // 방향이 0이 되는 경우가 있음
            // 그때는 강제로 골 방향으로 방향 선정
            // 수식은 마지막에 방향을 - 시키기 때문에 반대로 값을 넣어야 함
            direction = tile.Position - goalIndex;
        }
        
        direction = direction.normalized;
        tile.Direction = -direction;
        SetArrowAngle(ref tile);
    }

    private void SetArrowAngle(ref Tile tile)
    {
        if (tile.Index == goalIndex)
            return;
        
        var arrow = drawArrows[tile.Index];
        
        var angle = Math.Atan2(tile.Direction.y, tile.Direction.x) * (180f / Math.PI);
        arrow.eulerAngles = new Vector3(0, 0, (float)angle);
        arrow.gameObject.SetActive(isShowTileArrow);
    }

    #endregion

    #region 3. Create Chaser

    private void CreateChaser(int count)
    {
        var currentIndex = 0;

        while (currentIndex < count)
        {
            var randomCoordinate = GetRandomCoordinate();
            var position = groundTilemap.WorldToCell(
                groundTilemap.transform.position + 
                Camera.main.ScreenToWorldPoint(randomCoordinate));    
        
            var index = position + new Vector3Int(fieldWidth / 2, fieldHeight / 2);

            if (index.x < 0 || index.y < 0 || 
                index.x >= fieldWidth || index.y >= fieldHeight || 
                fields[index.x, index.y].IsBlock)
                continue;

            var newChaser = Instantiate(chaser);
            newChaser.transform.position = position;
            newChaser.gameObject.SetActive(true);
            
            currentIndex += 1;
        }
    }
    
    private Vector2 GetRandomCoordinate()
    {
        // 화면 크기 가져오기
        var screenSize = new Vector2(Screen.width, Screen.height);

        // 0과 1 사이의 임의 값 생성
        var randomX = Random.Range(0.0f, 1.0f);
        var randomY = Random.Range(0.0f, 1.0f);

        // 화면 크기에 임의 값을 곱하여 임의 좌표 계산
        return new Vector2(randomX * screenSize.x, randomY * screenSize.y); 
    }

    #endregion
    
    private void SetDirections()
    {
        if (directions.Count != 0)
            return;

        directions.Add(new Vector2Int(1, 0));
        directions.Add(new Vector2Int(-1, 0));
        directions.Add(new Vector2Int(0, 1));
        directions.Add(new Vector2Int(0, -1));
        directions.Add(new Vector2Int(1, 1));
        directions.Add(new Vector2Int(1, -1));
        directions.Add(new Vector2Int(-1, -1));
        directions.Add(new Vector2Int(-1, 1));
    }

    public Vector2 GetDirection(Vector3 position)
    {
        var tilePosition = groundTilemap.LocalToCell(groundTilemap.transform.position + position);
        var tileIndex = tilePosition + new Vector3Int(fieldWidth / 2, fieldHeight / 2);
        return fields[tileIndex.x, tileIndex.y].Direction;
    }
}

public partial class VectorField2D
{
    private bool isTouch;
    
    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isTouch)
        {
            isTouch = true;
            
            var position = groundTilemap.WorldToCell(groundTilemap.transform.position + Camera.main.ScreenToWorldPoint(Input.mousePosition));
            goalIndex = (Vector2Int)position + new Vector2Int(fieldWidth / 2, fieldHeight / 2);
            
            CreateHeatMap();
            CreateVectorField();
        }
        
        if (Input.GetMouseButtonUp(0) && isTouch)
        {
            isTouch = false;
        }
    }
}