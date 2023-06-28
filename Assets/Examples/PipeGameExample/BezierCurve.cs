using System;
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEditor;

[ExecuteInEditMode]
public class BezierCurve : MonoBehaviour
{
    //Has to be at least 4 so-called control points
    [Header("Bezier Points")] public Transform startPoint;
    public Transform endPoint;
    public Transform controlPointStart;
    public Transform controlPointEnd;

    [NonSerialized] public Transform target;

    [Header("Control Params")]

    // 抖动、移动动画时间
    [SerializeField]
    private float _animTime = 1.3f;

    // 最大长度
    [SerializeField] private float _maxLength = 6f;

    // 当到达最大长度时，晃动的角度
    [SerializeField] private float _shakeAngleWhenMaxLength = 15f;

    // 控制点距离
    [SerializeField] private float _controlEndDistance = 2.5f;

    // 算法所需代数变量
    private Vector3 A, B, C, D;

    // 曲线插值的密度, 越小越密
    private readonly float _resolution = 0.05f;
    private bool _isDragging = false;

    #region TweenKeys

    private const string TweenKeyPlayIdleAnimation = "PlayIdleAnimation";

    #endregion

    private void Start()
    {
        PlayIdleAnimation();
    }

    public void BeginDrag()
    {
        _isDragging = true;

        StopAllTweenAnimations();
    }

    public void EndDrag()
    {
        _isDragging = false;

        StopAllTweenAnimations();

        // var endBox = endPoint.GetComponent<BoxCollider2D>();
        // var target = this.target.GetComponent<BoxCollider2D>();
        // if (endBox.bounds.Intersects(target.bounds))
        // {
        //     PlayConnectAnimation();
        // }
        // else
        // {
        //     PlayIdleAnimation();
        // }
    }

    public void MoveTo(Vector3 pos)
    {
        var startPos = startPoint.position;
        // var curLen = GetLength();
        // if (curLen > _maxLength)
        // {
        //     var dir = pos - startPos;
        //     pos = startPos + dir.normalized * _maxLength;
        // }

        pos = Vector3.ClampMagnitude(pos, _maxLength);
        pos.x = Mathf.Min(pos.x, startPos.x);

        // endPoint.position = pos;
        if (DOTween.IsTweening(endPoint))
        {
            DOTween.Kill(endPoint);
        }

        endPoint.DOMove(pos, _animTime).SetEase(Ease.OutElastic).OnUpdate(() =>
        {
            UpdateParams();
            UpdateLine();
        }).SetLink(gameObject);
        
        UpdateParams();
        D = pos;
        UpdateEndControl();
    }

    private void UpdateParams()
    {
        A = startPoint.position;
        B = controlPointStart.position;
        C = controlPointEnd.position;
        D = endPoint.position;
    }

    private void StopAllTweenAnimations()
    {
        DOTween.Kill(TweenKeyPlayIdleAnimation);
        DOTween.Kill(endPoint);
        DOTween.Kill(controlPointEnd);
    }

    private void UpdateEndControl(bool immediately = false)
    {
        if (!target)
        {
            return;
        }

        var dir = D - target.position;
        var dir2 = C - D;
        var angle = Vector3.SignedAngle(dir.normalized, dir2.normalized, Vector3.forward);
        var quaternion = Quaternion.Euler(0, 0, -angle);
        var pos = quaternion * dir2.normalized;
        // C = controlPointEnd.position = D + pos.normalized * _controlEndDistance;
        var newPos = D + pos.normalized * _controlEndDistance;

        // TODO: 连接头旋转角度计算还需要考虑实际方向
        var endAngle = Vector3.SignedAngle(dir.normalized, Vector3.left, Vector3.forward);
        endPoint.localRotation = Quaternion.Euler(0, 0, endAngle);

        if (immediately)
        {
            C = controlPointEnd.position = newPos;
        }
        else
        {
            if (DOTween.IsTweening(controlPointEnd))
            {
                DOTween.Kill(controlPointEnd);
            }

            newPos.x = Mathf.Min(newPos.x, startPoint.position.x);
            controlPointEnd.DOMove(newPos, _animTime).SetEase(Ease.OutElastic).OnComplete(() =>
            {
                if (GetLength() >= _maxLength)
                {
                    PlayMaxLengthAnimation();
                }
            }).SetLink(gameObject);
        }
    }

    private float GetLength()
    {
        return Vector3.Distance(startPoint.position, endPoint.position);
    }

    #region 动画接口

    /// <summary>
    /// 待机动画
    /// </summary>
    public void PlayIdleAnimation()
    {
        controlPointEnd.position = endPoint.position;

        var seq = DOTween.Sequence();
        var dist = 2;
        var startPos = startPoint.position;
        var targetPos = startPos + Vector3.left * dist;
        var nextPos = startPos + Quaternion.Euler(0, 0, 15) * Vector3.left * dist;

        seq.Append(endPoint.DOMove(targetPos, 0.25f)
            .SetEase(Ease.Linear));

        seq.Append(endPoint.DOMove(nextPos, 0.3f).SetEase(Ease.Linear).SetLoops(9, LoopType.Yoyo));
        seq.SetLink(gameObject);
        seq.OnUpdate(() =>
        {
            controlPointEnd.position = endPoint.position;
            UpdateParams();
            UpdateLine();
        }).SetId(TweenKeyPlayIdleAnimation);
    }

    /// <summary>
    /// 连接动画
    /// </summary>
    public Tween PlayConnectAnimation()
    {
        var seq = DOTween.Sequence();
        var downPos = target.position + Vector3.down;

        UpdateEndControl(true);

        seq.Append(endPoint.DOMove(downPos, 1).SetEase(Ease.OutBack).OnUpdate(() => { UpdateEndControl(true); }));
        seq.Append(endPoint.DOMove(target.position, 0.5f).SetEase(Ease.OutBack));
        seq.OnUpdate(() =>
        {
            UpdateParams();
            UpdateLine();
        });
        seq.SetLink(gameObject);

        return seq;
    }

    /// <summary>
    /// 播放达到最大距离的动画
    /// </summary>
    private void PlayMaxLengthAnimation()
    {
        if (DOTween.IsTweening(controlPointEnd))
        {
            DOTween.Kill(controlPointEnd);
        }

        var dir = C - D;
        var newPos = Quaternion.Euler(0, 0, _shakeAngleWhenMaxLength) * dir;
        controlPointEnd.DOMove(D + newPos, 0.15f)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Yoyo)
            .OnUpdate(() =>
            {
                UpdateParams();
                UpdateLine();
            })
            .SetLink(gameObject);
    }

    #endregion

    private List<Vector3> _posList = new List<Vector3>();

    private List<Vector3> CalcPositions()
    {
        _posList.Clear();

        //The resolution of the line
        //Make sure the resolution is adding up to 1, so 0.3 will give a gap at the end, but 0.2 will work
        float resolution = _resolution;

        //How many loops?
        int loops = Mathf.FloorToInt(1f / resolution);

        for (int i = 1; i <= loops; i++)
        {
            //Which t position are we at?
            float t = i * resolution;

            //Find the coordinates between the control points with a Catmull-Rom spline
            Vector3 newPos = DeCasteljausAlgorithm(t);

            _posList.Add(newPos);
        }

        return _posList;
    }

    private void UpdateLine()
    {
        var lineRenderer = GetComponent<LineRenderer>();
        List<Vector3> posList = CalcPositions();
        lineRenderer.positionCount = posList.Count;
        lineRenderer.SetPositions(posList.ToArray());
    }

    //The De Casteljau's Algorithm
    Vector3 DeCasteljausAlgorithm(float t)
    {
        //Linear interpolation = lerp = (1 - t) * A + t * B
        //Could use Vector3.Lerp(A, B, t)

        //To make it faster
        float oneMinusT = 1f - t;

        //Layer 1
        Vector3 Q = oneMinusT * A + t * B;
        Vector3 R = oneMinusT * B + t * C;
        Vector3 S = oneMinusT * C + t * D;

        //Layer 2
        Vector3 P = oneMinusT * Q + t * R;
        Vector3 T = oneMinusT * R + t * S;

        //Final interpolated position
        Vector3 U = oneMinusT * P + t * T;

        return U;
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (EditorApplication.isPlaying)
        {
            return;
        }

        UpdateParams();
        UpdateEndControl();
        UpdateLine();
    }

    void OnDrawGizmos()
    {
        UpdateParams();

        Gizmos.color = Color.white;

        var posLists = CalcPositions();
        Vector3 lastPos = A;

        for (int i = 1; i < posLists.Count; i++)
        {
            Vector3 newPos = posLists[i];
            Gizmos.DrawLine(lastPos, newPos);
            lastPos = newPos;
        }

        Gizmos.color = Color.green;
        Gizmos.DrawLine(A, B);
        Gizmos.DrawLine(C, D);

        if (target)
        {
            var dir = D - target.position;
            var dir2 = C - D;
            Gizmos.color = Color.red;
            Gizmos.DrawRay(target.position, dir.normalized);
            var angle = Vector3.SignedAngle(dir.normalized, dir2.normalized, Vector3.forward);
            var pos = Quaternion.Euler(0, 0, -angle) * dir2.normalized;
            Gizmos.DrawRay(D, pos);

            Gizmos.color = Color.blue;
            var newPos = Quaternion.Euler(0, 0, _shakeAngleWhenMaxLength) * dir.normalized;
            Gizmos.DrawRay(D, newPos);
            // C = controlPointEnd.position = pos;
        }
    }
#endif
}