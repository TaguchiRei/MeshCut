using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CutObjectPool : MonoBehaviour
{
    [SerializeField, Tooltip("あらかじめ生成しておくインスタンス数")]
    private int _preCutObjectInstanceNum;

    private List<GameObject> _preCutPool = new();
    private Dictionary<int, GameObject> _postCutPool = new();

    private void Start()
    {
    }

    /// <summary>
    /// オブジェクトプールから切断後のオブジェクトを生成する
    /// </summary>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="mesh"></param>
    /// <param name="mats"></param>
    public void GenerateCutObject(Vector3 position, Quaternion rotation, Mesh mesh, Material[] mats)
    {
        if (_preCutPool.Count != 0)
        {
            var obj = _preCutPool[0];
            var cuttable = obj.GetComponent<CuttableObject>();
        }
    }
}