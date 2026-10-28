using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
using System.Linq.Expressions;
using System.Linq;


[CreateAssetMenu(fileName = "TileData", menuName = "Datas/TileData")]


public class TileData : ScriptableObject
{
    public List<TileInfo> stageDesign = new List<TileInfo>();

    [Header("CSVインポート設定")]
    [SerializeField] private TextAsset sourceCsvFile;

    [ContextMenu("CSVファイルからデータをインポート")]
    public void ImportFromCSV()
    {
        if (sourceCsvFile == null)
        {
            Debug.LogError("[TileData]CSVファイルが未設定です！");
            return;
        }

        stageDesign.Clear();

        string[] lines = sourceCsvFile.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < lines.Length; i++)
        {
            string[] values = lines[i].Split(',');
            if (values.Length < 3) continue;

            TileInfo info = new TileInfo();

            //1列目パターン
            if (int.TryParse(values[0].Trim(), out int pattern))
            {
                info.obstaclePattern = pattern;
            }

            //２列目障害物タイプ
            string enumString = values[1].Trim();
            if (System.Enum.TryParse(enumString, true, out ObstacleType parsedType))
            {
                info.obstacleType = parsedType;
            }
            else
            {
                Debug.LogWarning($"[TileData警告]{i + 1}行目の『{enumString}』はobstacleTypeに存在しません");
            }

            if (float.TryParse(values[2].Trim(), out float speed))
            {
                info.CustomSpeed = speed;
            }

            stageDesign.Add(info);
        }
        Debug.Log($"【TileData】CSVから {stageDesign.Count} マス分のデータを正常にインポートしました！");

        // Unityエディタにデータが変更されたことを通知して保存を確定させる
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}

public enum ObstacleType
{
    CarNormal, //普通の車
    CarTruck, //トラック
    CarMove, //車線変更する車
    CarForward, //前に進む車
    CarStop, //急停止する車
    fallingObject, //落下するモノ

    mud //ぬかるみ
}

[System.Serializable]
public class TileInfo
{
    [Tooltip("0:なし, 1:左, 2:中央, 3:右, 4:左右, 5:左&中央, 6:右&中央, 7:ゴール板")]
    [Range(0, 7)] public int obstaclePattern;
    public ObstacleType obstacleType;

    [Tooltip("この床で生成する車の移動速度（０ならプレハブの初期値)")]
    [SerializeField] private float customSpeed = 0f;

    public float CustomSpeed
    {
        get => customSpeed;
        set => customSpeed = value;
    }
}