using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public partial class VectorField2D : MonoBehaviour
{
    [Header("For Debugging")]
    [SerializeField] private bool isShowGroundArea;
    [SerializeField] private bool isShowObstacleArea;
    
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
    
    private void Start()
    {
        Initialize();
        SetObstaclePositions();
        SetGroundPositions();
        
        UpdateGoalPosition();
    }

    private void Initialize()
    {
        tileWorldOffset = new Vector2(0.5f, 0.5f);
        fromGround = groundTilemap.origin;
        toGround = groundTilemap.size + groundTilemap.origin;
        
        fromObstacle = obstacleTilemap.origin;
        toObstacle = obstacleTilemap.size + obstacleTilemap.origin;
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
                        new Vector3(position.x + 0.5f, position.y + 0.5f),
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
                        new Vector3(position.x + 0.5f, position.y + 0.5f),
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
                var tileIndex = obstacleTilemap.WorldToCell( tileWorldPosition );
                var tile = obstacleTilemap.GetTile(tileIndex);

                if (tile == null)
                    continue;
                
                obstaclePositions.Add(tileWorldPosition);
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
                var tileIndex = groundTilemap.WorldToCell( tileWorldPosition );
                var tile = groundTilemap.GetTile(tileIndex);

                if (tile == null)
                    continue;
                
                groundPositions.Add(tileWorldPosition);
            }
        }
    }
}

public partial class VectorField2D
{
    private void UpdateGoalPosition()
    {
        var goalWorldPosition = goalTransform.position;
        var position = groundTilemap.WorldToCell(groundTilemap.transform.position + goalWorldPosition);
        
        Debug.Log(position);
    }
}