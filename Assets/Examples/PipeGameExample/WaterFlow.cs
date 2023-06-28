using DG.Tweening;
using UnityEngine;

public class WaterFlow : MonoBehaviour
{
    private SpriteRenderer _renderer;
    private readonly float _waveThickness = 0.03f;

    private static readonly int Cutoff = Shader.PropertyToID("_Cutoff");
    private static readonly int IsFalling = Shader.PropertyToID("_IsFalling");
    private static readonly int WaveThickness = Shader.PropertyToID("_WaveThickness");

    void Start()
    {
        _renderer = GetComponent<SpriteRenderer>();

        var newMat = _renderer.material;
        newMat.SetFloat(WaveThickness, 0);
        newMat.SetFloat(IsFalling, 0);
        newMat.SetFloat(Cutoff, 1);
    }

    public void PlayConnectAnimation()
    {
        var mat = _renderer.material;
        DOTween.Kill(mat);
        
        mat.SetFloat(WaveThickness, _waveThickness);
        mat.SetFloat(IsFalling, 0);
        mat.SetFloat(Cutoff, 1);
        mat.DOFloat(0, Cutoff, 0.3f).SetEase(Ease.Linear).OnComplete(OnAnimationComplete);
    }

    public void PlayDisconnectAnimation()
    {
        var mat = _renderer.material;
        DOTween.Kill(mat);
        
        mat.SetFloat(WaveThickness, _waveThickness);
        mat.SetFloat(IsFalling, 1);
        mat.SetFloat(Cutoff, 1);
        mat.DOFloat(0, Cutoff, 0.3f).SetEase(Ease.Linear).OnComplete(OnAnimationComplete);
    }

    private void OnAnimationComplete()
    {
        var mat = _renderer.material;
        mat.SetFloat(WaveThickness, 0);
    }
}