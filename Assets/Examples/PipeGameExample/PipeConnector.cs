using UnityEngine;

public class PipeConnector : MonoBehaviour
{
    [SerializeField] public float radius = 0.6f;

    [HideInInspector] public Pipe Pipe { get; set; }

    // Start is called before the first frame update
    void Start()
    {
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}