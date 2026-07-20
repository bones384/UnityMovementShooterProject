using System.Collections.Generic;
using Entities.Netcode.GameMechanics;
using UnityEngine;

public class KothVisualisation : MonoBehaviour
{
    public Renderer pointRenderer;
    public Material neutralMaterial;
    private float _lastCaptureProgress;
    private int _lastOwner = -1;

    private void Update()
    {
        if (!KothBridge.IsActive || PlayerVisualisationManager.LocalPlayer == null ||
            PlayerVisualisationManager.Instance == null)
            return;
        var state = KothBridge.State;

        if (_lastCaptureProgress == 0 && KothBridge.State.CaptureProgress > 0)
            AudioManager.Instance.Play3D(SFX.KothStart, transform.position);
        
        if (_lastOwner != state.CurrentOwner && state.CurrentOwner != -1)
            AudioManager.Instance.Play2D(SFX.KothCap);
        _lastCaptureProgress = state.CaptureProgress;
        _lastOwner = state.CurrentOwner;

        var myTeam = PlayerVisualisationManager.LocalPlayer.Value.TeamIndex;
        var currentOwner = KothBridge.State.CurrentOwner;
        var materials = new List<Material>();
        if (currentOwner == -1)
            materials.Add(neutralMaterial);
        else if (currentOwner == myTeam)
            materials.Add(PlayerVisualisationManager.Instance.friendMaterial);
        else
            materials.Add(PlayerVisualisationManager.Instance.enemyMaterial);
        pointRenderer.SetMaterials(materials);
    }
}