using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class Chunk : MonoBehaviour
{
    [SerializeField] GameObject[] obstaclePrefab; //生成する障害物のプレハブ
    [SerializeField] private float fallingObjectSpawnHeight = 10f; //落下物の生成高さ
    [SerializeField] GameObject mudPrefab;

    [SerializeField] float[] lanes = { -2.8f, 0f, 2.8f }; //レーンのX座標
    [SerializeField] private Transform carParent; //障害物の親オブジェクト

    [Header("背景")]
    [SerializeField] private GameObject[] leftTownPrefabs;
    [SerializeField] private GameObject[] rightTownPrefabs;
    [SerializeField] private GameObject goalPrefab;
    [SerializeField] private float goalZPos = 20f;
    [SerializeField] private float goalYPos = 6f;

    [SerializeField] private Transform leftBackground;
    [SerializeField] private Transform rightBackground;

    [SerializeField] private float sideOffset = 10f;
    [SerializeField] private float heightOffset = .5f;
    [SerializeField] float[] zPositions = { -2.5f, 2.5f };

    private PlayerController player;

    void Start()
    {
    }

    public void ApplyStageDesign(int pattern, ObstacleType type, float customSpeed)
    {
        // 7ならゴール
        if (pattern == 7)
        {
            SpawnGoal();
            return;
        }


        GenareteTown();


        // 0なら何も生成しない
        if (pattern == 0) return;



        // パターンに応じて、どのレーン(0, 1, 2)に生成するかを決める
        switch (pattern)
        {
            case 1: // 左
                SpawnObstacle(0, type, customSpeed);
                break;
            case 2: // 中央
                SpawnObstacle(1, type, customSpeed);
                break;
            case 3: // 右
                SpawnObstacle(2, type, customSpeed);
                break;
            case 4: // 左右
                SpawnObstacle(0, type, customSpeed);
                SpawnObstacle(2, type, customSpeed);
                break;
            case 5: // 左＆中央
                SpawnObstacle(0, type, customSpeed);
                SpawnObstacle(1, type, customSpeed);
                break;
            case 6: // 右＆中央
                SpawnObstacle(1, type, customSpeed);
                SpawnObstacle(2, type, customSpeed);
                break;
        }

    }

    private void SpawnObstacle(int laneIndex, ObstacleType type, float customSpeed)
    {
        Vector3 localPos = new Vector3(lanes[laneIndex], 0f, 0f);
        Vector3 worldSpawnPosition = transform.TransformPoint(localPos);
        player = FindFirstObjectByType<PlayerController>();


        if (type == ObstacleType.mud)
        {
            if (mudPrefab != null)
            {
                Instantiate(mudPrefab, worldSpawnPosition, Quaternion.identity, this.transform);
            }
        }
        else
        {
            int prefabIndex = (int)type;
            if (prefabIndex >= 0 && prefabIndex < obstaclePrefab.Length && obstaclePrefab[prefabIndex] != null)
            {
                float spawnY = 0f;
                if (type == ObstacleType.fallingObject)
                {
                    spawnY = fallingObjectSpawnHeight;
                }
                worldSpawnPosition.y = spawnY;

                if (type == ObstacleType.CarForward)
                {
                    if (player != null)
                    {
                        worldSpawnPosition.z = player.transform.position.z - 35f;
                    }
                }

                GameObject obstacle = Instantiate(obstaclePrefab[prefabIndex], worldSpawnPosition, Quaternion.identity);
                obstacle.tag = "Obstacle";

                if (obstacle.TryGetComponent<NPCVehicle>(out var npcVehicle))
                {
                    npcVehicle.InitSpeed(customSpeed);
                }
                else if (obstacle.TryGetComponent<NPCVehicleForward>(out var npcVehicleForward))
                {
                    npcVehicleForward.InitSpeed(customSpeed);
                }
            }
        }
    }

    private void GenareteTown()
    {
        SpawnTown(leftBackground, leftTownPrefabs, -sideOffset);
        SpawnTown(rightBackground, rightTownPrefabs, sideOffset);
    }

    private void SpawnTown(Transform parent, GameObject[] prefabs, float x)
    {
        if (parent == null) return;

        foreach (float z in zPositions)
        {
            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];

            GameObject town = Instantiate(prefab, parent);

            town.transform.localPosition = new Vector3(x, heightOffset, z);
            town.transform.localRotation = Quaternion.identity;
        }

    }

    private void SpawnGoal()
    {
        if (goalPrefab == null) return;

        GameObject goal = Instantiate(goalPrefab, transform);

        goal.transform.localPosition = Vector3.zero;
        goal.transform.localRotation = Quaternion.identity;
    }

}
