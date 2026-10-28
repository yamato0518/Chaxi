using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] PlayerController player;

    [Header("ステージ設計図")]
    [SerializeField] private TileData currentStageData;

    [Header("地面設定")]
    [SerializeField] GameObject ChunkPrefab;
    [SerializeField] private GameObject goalPrefab;
    [SerializeField] int startingChunksAmount = 20;
    [SerializeField] Transform chunkParent;
    [SerializeField] float chunkLength = 10f;
    [SerializeField] private int goalApproachChunkCount = 3;

    [Header("背景の設定")]
    [SerializeField] private GameObject boardPrefab;
    [SerializeField] private GameObject leftBoardPrefab;
    [SerializeField] private GameObject centerBoardPrefab;
    [SerializeField] private GameObject rightBoardPrefab;

    [SerializeField] private float hieghtOffset = 1.5f;
    [SerializeField] private float[] lanes = { -2.8f, 0f, 2.8f }; //レーンのX座標

    private int chunkCounter = 0;
    private int targetReauiredCount;
    private readonly List<int> boardSpawnOrder = new List<int>();
    private int boardSpawnOrderIndex = 0;

    List<GameObject> chunks = new List<GameObject>();
    private int nextTileIndex = 0;

    void Start()
    {
        ResetBoardCounter();
        ResetBoardSpawnOrder();

        //最初の生成
        SpawnStartingChunks();
    }

    void Update()
    {
        if (player.getIsRunning())
        {
            MoveChunks();
        }
    }

    //chunkの生成
    private void SpawnStartingChunks()
    {
        for (int i = 0; i < startingChunksAmount; i++)
        {
            SpawnChunk(true);
        }
    }

    private GameObject SpawnChunk(bool applyStageData)
    {
        if (applyStageData && currentStageData != null && nextTileIndex >= currentStageData.stageDesign.Count)
        {
            return null;
        }

        float spawnPositionZ = CallculateSpawnPositionZ();

        Vector3 chunkSpawnPos = new Vector3(transform.position.x, transform.position.y, spawnPositionZ);
        GameObject newChunk = Instantiate(ChunkPrefab, chunkSpawnPos, Quaternion.identity, chunkParent);

        if (applyStageData && newChunk.TryGetComponent<Chunk>(out var chunkScript) && currentStageData != null)
        {
            TileInfo info = currentStageData.stageDesign[nextTileIndex];
            chunkScript.ApplyStageDesign(info.obstaclePattern, info.obstacleType, info.CustomSpeed);
        }

        chunks.Add(newChunk);

        if (applyStageData)
        {
            nextTileIndex++;

            chunkCounter++;
            if (chunkCounter >= targetReauiredCount)
            {
                SpawnBoard(chunkSpawnPos);
                ResetBoardCounter();
            }
        }

        return newChunk;
    }

    public Transform SpawnGoalApproachSequence(Vector3 startPosition, int approachChunkCount = -1)
    {
        if (ChunkPrefab == null)
        {
            return null;
        }

        int chunkCount = (approachChunkCount > 0) ? approachChunkCount : goalApproachChunkCount;
        GameObject lastChunk = null;
        float spawnPositionZ = startPosition.z + chunkLength;

        for (int i = 0; i < chunkCount; i++)
        {
            Vector3 chunkSpawnPos = new Vector3(transform.position.x, transform.position.y, spawnPositionZ);
            lastChunk = Instantiate(ChunkPrefab, chunkSpawnPos, Quaternion.identity, chunkParent);
            chunks.Add(lastChunk);
            spawnPositionZ += chunkLength;
        }

        if (lastChunk == null || goalPrefab == null)
        {
            return lastChunk != null ? lastChunk.transform : null;
        }

        Vector3 goalSpawnPos = new Vector3(transform.position.x, transform.position.y, spawnPositionZ);
        GameObject goal = Instantiate(goalPrefab, goalSpawnPos, Quaternion.identity, chunkParent);
        return goal.transform;
    }

    //スポーン位置の計算
    private float CallculateSpawnPositionZ()
    {
        float spawnPositionZ;

        if (chunks.Count == 0)
        {
            spawnPositionZ = transform.position.z - chunkLength;
        }
        else
        {
            spawnPositionZ = chunks[chunks.Count - 1].transform.position.z + chunkLength;
        }

        return spawnPositionZ;
    }

    //chunkの移動
    void MoveChunks()
    {
        if (player == null) return;

        float currentSpeed = player.CurrentMapSpeed;

        for (int i = 0; i < chunks.Count; i++)
        {
            GameObject chunk = chunks[i];
            chunk.transform.Translate(-transform.forward * (currentSpeed * Time.deltaTime));

            if (chunk.transform.position.z <= player.transform.position.z - chunkLength - 20f)
            {
                chunks.Remove(chunk);
                Destroy(chunk);
                SpawnChunk(true);
            }
        }
    }

    private void ResetBoardCounter()
    {
        chunkCounter = 0;
        targetReauiredCount = Random.Range(10, 16);
    }

    private void ResetBoardSpawnOrder()
    {
        boardSpawnOrder.Clear();

        for (int laneIndex = 0; laneIndex < lanes.Length; laneIndex++)
        {
            boardSpawnOrder.Add(laneIndex);
        }

        for (int i = boardSpawnOrder.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (boardSpawnOrder[i], boardSpawnOrder[swapIndex]) = (boardSpawnOrder[swapIndex], boardSpawnOrder[i]);
        }

        boardSpawnOrderIndex = 0;
    }

    private void SpawnBoard(Vector3 chunkPosition)
    {
        if (lanes.Length == 0) return;

        if (boardSpawnOrder.Count == 0 || boardSpawnOrderIndex >= boardSpawnOrder.Count)
        {
            ResetBoardSpawnOrder();
        }

        int laneIndex = boardSpawnOrder[boardSpawnOrderIndex++];

        if (laneIndex == 1)
        {
            laneIndex = (Random.value > 0.5f) ? 2 : 0;
        }

        laneIndex = Mathf.Clamp(laneIndex, 0, lanes.Length - 1);

        GameObject selectedBoardPrefab = GetBoardPrefab(laneIndex);
        if (selectedBoardPrefab == null) return;

        Vector3 spawnPos = new Vector3(chunkPosition.x + lanes[laneIndex], chunkPosition.y + hieghtOffset, chunkPosition.z);

        GameObject board = Instantiate(selectedBoardPrefab, spawnPos, Quaternion.identity);

        board.transform.SetParent(chunks[chunks.Count - 1].transform);


    }

    private GameObject GetBoardPrefab(int laneIndex)
    {
        if (lanes.Length < 3)
        {
            return boardPrefab;
        }

        return laneIndex switch
        {
            0 => leftBoardPrefab != null ? leftBoardPrefab : boardPrefab,
            1 => centerBoardPrefab != null ? centerBoardPrefab : boardPrefab,
            2 => rightBoardPrefab != null ? rightBoardPrefab : boardPrefab,
            _ => boardPrefab,
        };
    }
}
