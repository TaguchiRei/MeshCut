using UnityEngine;
using UsefulAttribute;

public class InsectLegIK : MonoBehaviour
{
    [Header("Bones")] [SerializeField] Transform _root;
    [SerializeField] Transform _knee;
    [SerializeField] Transform _footTarget;

    [Header("Lengths")] [SerializeField] float _upperLength = 1f; // L1
    [SerializeField] float _lowerLength = 1f; // L2

    [Header("Bend Direction")] [SerializeField]
    Vector3 _bendDirection = Vector3.up;

    [SerializeField] private Transform _legPosition;

    void LateUpdate()
    {
        Vector3 rootPos = _root.position;
        Vector3 targetPos = _footTarget.position;

        // IK平面の決定 (root→target と bendDirection で平面を作る)

        Vector3 toTarget = targetPos - rootPos;
        Vector3 planeNormal = Vector3.Cross(_bendDirection, toTarget).normalized;

        if (planeNormal.sqrMagnitude < 0.0001f)
            planeNormal = _root.right;

        // ターゲットをIK平面へ投影
        Vector3 projectedTarget = ProjectPointOnPlane(targetPos, rootPos, planeNormal);

        // 到達距離制限（円が重ならない場合の補正）
        // 届かない位置なら補正する
        float distance = Vector3.Distance(rootPos, projectedTarget);
        float maxReach = _upperLength + _lowerLength;
        float minReach = Mathf.Abs(_upperLength - _lowerLength);

        Vector3 dir = (projectedTarget - rootPos).normalized;

        //到達しない距離の場合は投影した座標を利用する。
        //いずれバーストコンパイラを利用した最適化を利かせるために以下の記述
        float clampedDistance = Mathf.Clamp(distance, minReach, maxReach);
        projectedTarget = rootPos + dir * clampedDistance;
        distance = clampedDistance;

        // projectedTargetを中心とする半径lowerLengthの円と根元を中心とする半径upperLengthの円のの交差点を求める
        float rootToMidDistance = (_upperLength * _upperLength - _lowerLength * _lowerLength + distance * distance) /
                                  (2f * distance);
        float midToKneDistance =
            Mathf.Sqrt(Mathf.Max(_upperLength * _upperLength - rootToMidDistance * rootToMidDistance, 0f));

        Vector3 midPoint = rootPos + dir * rootToMidDistance;
        Vector3 perpendicular = Vector3.Cross(planeNormal, dir).normalized;

        Vector3 kneeCandidateA = midPoint + perpendicular * midToKneDistance;
        Vector3 kneeCandidateB = midPoint - perpendicular * midToKneDistance;

        // 二つの交点のうち膝の理想方向に近い方を選ぶ
        Vector3 desiredKneeDir = (_bendDirection).normalized;

        float dotA = Vector3.Dot((kneeCandidateA - rootPos).normalized, desiredKneeDir);
        float dotB = Vector3.Dot((kneeCandidateB - rootPos).normalized, desiredKneeDir);

        Vector3 kneePos = dotA > dotB ? kneeCandidateA : kneeCandidateB;

        // 今回のIKは回転のみで成立するので回転にする
        Vector3 rootToKnee = (kneePos - rootPos).normalized;
        Vector3 kneeToTarget = (projectedTarget - kneePos).normalized;

        _root.rotation = Quaternion.LookRotation(
            Vector3.ProjectOnPlane(rootToKnee, planeNormal),
            _bendDirection
        );

        _knee.rotation = Quaternion.LookRotation(
            Vector3.ProjectOnPlane(kneeToTarget, planeNormal),
            _bendDirection
        );
    }

    // 平面投影
    private Vector3 ProjectPointOnPlane(Vector3 point, Vector3 planePoint, Vector3 planeNormal)
    {
        Vector3 v = point - planePoint;
        float d = Vector3.Dot(v, planeNormal);
        return point - planeNormal * d;
    }

    [MethodExecutor(true)]
    private void SetLength()
    {
        _upperLength = (_root.position - _knee.position).magnitude;
        _lowerLength = (_knee.position - _legPosition.position).magnitude;
    }
}