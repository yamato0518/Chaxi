using Unity.VisualScripting;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;

public class StageManager : MonoBehaviour
{
    [Header("参照設定")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameObject roadPrefab; //道路のプレハブ
    [SerializeField] private GameObject goalPrefab; //ゴールのプレハブ

    [Header("ステージ設定")]
    [SerializeField] private float tileLength = 10f; //道路タイルの長さ
    [SerializeField] private int initialTilesCount = 10; //最初に生成されるタイルの数
    [SerializeField] private float laneDistance = 2.3f;
    [SerializeField] private float recycleBehindZ = -15f;

    [Header("ステージ設計図")]
    [SerializeField] private TileData currentStageData;

    [Header("障害物のプレハブ")]
    [SerializeField] private GameObject[] obstaclePrefabs;
    [SerializeField] private float fallingHeight = 20f; //落下物の高さ

    private Queue<GameObject> activeTiles = new Queue<GameObject>();
    private int nextTileIndex = 0;
    private float spawnZ = 0f;

    public float TotalMileage{get; private set;} = 0f;
    void Start()
    {
        //最初のタイル
        for (int i = 0; i < initialTilesCount; i++)
        {
            SpawnInitialTile();
        }
    }

    void FixedUpdate()
    {
        if(playerController == null) return;

        if(currentStageData != null && nextTileIndex >= currentStageData.stageDesign.Count)return;

        float currentSpeed = playerController.CurrentMapSpeed;

        if (!playerController.getIsStuckInMud())
        {
            TotalMileage += currentSpeed * Time.fixedDeltaTime;
        }

        transform.Translate(0f, 0f, -currentSpeed * Time.fixedDeltaTime);
   
        if(activeTiles.Count > 0)
        {
            GameObject oldestTile = activeTiles.Peek();
            if(oldestTile.transform.position.z < recycleBehindZ)
            {
                MoveAndResetTile();
            }
        }
    }

    //最初のタイルの生成用関数
    private void SpawnInitialTile()
    {
        GameObject tile = Instantiate(roadPrefab, new Vector3(0, 0, spawnZ), Quaternion.identity);
        tile.transform.SetParent(this.transform);

        ApplyObstacleData(tile);

        activeTiles.Enqueue(tile);
        spawnZ += tileLength;
        nextTileIndex++;
    }

    //一番前のタイルを一番後ろに
    private void MoveAndResetTile()
    {
        GameObject oldestTile = activeTiles.Dequeue();

        oldestTile.transform.position = new Vector3(0, 0, spawnZ);

        foreach (Transform child in oldestTile.transform)
        {
            if (child.CompareTag("Obstacle"))
            {
                Destroy(child.gameObject);
            }
        }

        ApplyObstacleData(oldestTile);

        activeTiles.Enqueue(oldestTile);
        spawnZ += tileLength;
        nextTileIndex++;
    }

    //データから障害物を生成する関数
    private void ApplyObstacleData(GameObject tile)
    {
        if (currentStageData == null || nextTileIndex >= currentStageData.stageDesign.Count) return;

        TileInfo currentData = currentStageData.stageDesign[nextTileIndex];
        Vector3 tilePos = tile.transform.position;

        switch (currentData.obstaclePattern)
        {
            case 0:
                break;
            case 1:
                SpawnObstacle(tile, -laneDistance, tilePos.z, currentData.obstacleType);
                break;
            case 2:
                SpawnObstacle(tile, 0f, tilePos.z, currentData.obstacleType);
                break;
            case 3:
                SpawnObstacle(tile, laneDistance, tilePos.z, currentData.obstacleType);
                break;
            case 4:
                SpawnObstacle(tile, -laneDistance, tilePos.z, currentData.obstacleType);
                SpawnObstacle(tile, laneDistance, tilePos.z, currentData.obstacleType);
                break;
            case 5:
                SpawnObstacle(tile, -laneDistance, tilePos.z, currentData.obstacleType);
                SpawnObstacle(tile, 0f, tilePos.z, currentData.obstacleType);
                break;
            case 6:
                SpawnObstacle(tile, 0f, tilePos.z, currentData.obstacleType);
                SpawnObstacle(tile, laneDistance, tilePos.z, currentData.obstacleType);
                break;
            case 7:
                GameObject goal = Instantiate(goalPrefab, new Vector3(0f, 0f, tilePos.z), Quaternion.identity);
                goal.transform.SetParent(tile.transform);
                break;
        }
    }

    private void SpawnObstacle(GameObject parentTile, float localX, float worldZ, ObstacleType type)
    {
        int prefabIndex = (int)type;

        if (prefabIndex < 0 || prefabIndex >= obstaclePrefabs.Length || obstaclePrefabs[prefabIndex] == null) return;

        GameObject selectedPrefab = obstaclePrefabs[prefabIndex];

        float spawnY = 0.5f;

        if(type == ObstacleType.fallingObject)
        {
            spawnY = fallingHeight;
        }

        Vector3 spawnWorldPosition = parentTile.transform.TransformPoint(new Vector3(localX, spawnY, 0f));
        GameObject obstacle = Instantiate(selectedPrefab, spawnWorldPosition, Quaternion.identity);
        obstacle.transform.SetParent(parentTile.transform);

        if(type == ObstacleType.fallingObject || type == ObstacleType.mud)
        {
            obstacle.transform.SetParent(parentTile.transform);
        }
        else
        {
            
        }

        obstacle.tag = "Obstacle";
    }
}
