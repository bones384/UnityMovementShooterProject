using System.Collections.Generic;
using Entities.Netcode.GameMechanics;
using UnityEngine;

public class KothVisualisation : MonoBehaviour
{
    public Renderer pointRenderer;
    public Material neutralMaterial; // Create a gray material and assign it here!
    private float _lastCaptureProgress;
    private int _lastOwner = -1;

    private void Update()
    {
        if (!KothBridge.IsActive || PlayerVisualisationManager.LocalPlayer == null ||
            PlayerVisualisationManager.Instance == null)
            return;
        var state = KothBridge.State;
// 1. Capture Start (Went from 0 to something)

        if (_lastCaptureProgress == 0 && KothBridge.State.CaptureProgress > 0)
            AudioManager.Instance.Play3D(SFX.KothStart, transform.position);

        // 2. Captured (Owner changed)
        if (_lastOwner != state.CurrentOwner && state.CurrentOwner != -1)
            AudioManager.Instance.Play2D(SFX.KothCap); // Play globally
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