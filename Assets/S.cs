using System;
using System.Collections;
using UnityEngine;

public class S : MonoBehaviour
{
    [SerializeField] private RotateData[] _rotateDatas;
    private void Start() => StartCoroutine(RotateAsync());

    private IEnumerator RotateAsync()
    {
        float startTime = Time.time;
        int ind = 0;
        while (true)
        {
            if (startTime + _rotateDatas[ind].RotateTime < Time.time)
            {
                ind = (ind + 1) % _rotateDatas.Length;
            }

            transform.Rotate(_rotateDatas[ind].Angle);

            yield return null;
        }
    }

    [Serializable]
    private struct RotateData
    {
        public Vector3 Angle;
        public int RotateTime;
    }
}