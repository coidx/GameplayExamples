using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class PipeGame : MonoBehaviour
{
    [SerializeField] public Transform targetConnector;
    [SerializeField] public BezierCurve bezierCurve;

    private bool _isHit = false;
    private List<Pipe> _pipes = new List<Pipe>();
    private List<PipeConnector> _connectors = new List<PipeConnector>();

    private void Start()
    {
        _pipes.AddRange(FindObjectsOfType<Pipe>());
        _connectors.AddRange(FindObjectsOfType<PipeConnector>());
    }

    private PipeConnector FindNearestConnector(Transform target)
    {
        float minDist = float.MaxValue;
        PipeConnector ret = null;
        for (int i = 0; i < _connectors.Count; i++)
        {
            var connector = _connectors[i];

            var dist = Vector3.Distance(target.position, connector.transform.position);
            if (minDist > dist)
            {
                minDist = dist;
                ret = connector;
            }
        }

        return ret;
    }

    void Update()
    {
        var ray = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (Input.GetMouseButtonDown(0) && !_isHit)
        {
            var hits = Physics2D.RaycastAll(ray, Vector2.zero, 100);
            for (int i = 0; i < hits.Length; i++)
            {
                ray.z = 0;

                if (bezierCurve.target)
                {
                    var connector = bezierCurve.target.GetComponent<PipeConnector>();
                    connector.Pipe.WaterFlow.PlayDisconnectAnimation();
                }

                bezierCurve.target = FindNearestConnector(bezierCurve.transform).transform;
                bezierCurve.BeginDrag();
                bezierCurve.MoveTo(ray);

                _isHit = true;
                break;
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            _isHit = false;
            bezierCurve.EndDrag();

            if (bezierCurve.target)
            {
                var connector = bezierCurve.target.GetComponent<PipeConnector>();
                var dist = Vector3.Distance(bezierCurve.endPoint.position, connector.transform.position);
                if (connector.radius >= dist)
                {
                    bezierCurve.PlayConnectAnimation().OnComplete(() =>
                    {
                        connector.Pipe.WaterFlow.PlayConnectAnimation();
                    });
                }
                else
                {
                    bezierCurve.PlayIdleAnimation();
                    bezierCurve.target = null;
                }
            }
            else
            {
                bezierCurve.PlayIdleAnimation();
            }
        }

        if (_isHit)
        {
            var x = Input.GetAxis("Mouse X");
            var y = Input.GetAxis("Mouse Y");
            if (x != 0 || y != 0)
            {
                ray.z = 0;
                bezierCurve.target = FindNearestConnector(bezierCurve.transform).transform;
                bezierCurve.MoveTo(ray);
            }
        }
    }
}