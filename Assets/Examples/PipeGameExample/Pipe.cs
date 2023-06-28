using UnityEngine;

public class Pipe : MonoBehaviour
{
    private WaterFlow _waterFlow;

    public WaterFlow WaterFlow => _waterFlow;

    void Start()
    {
        _waterFlow = GetComponentInChildren<WaterFlow>();

        foreach (var connector in GetComponentsInChildren<PipeConnector>())
        {
            connector.Pipe = this;
        }
    }
}