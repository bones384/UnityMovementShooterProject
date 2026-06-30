using Entities.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerVisualisation : MonoBehaviour
{
    public bool isLocalPlayer;
    public PlayerStateComponent PlayerState;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        if (!isLocalPlayer) return;

        foreach (Transform childTransform in transform)
            childTransform.gameObject.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;
    }

    // Update is called once per frame
    private void Update()
    {
    }

    private void OnGUI()
    {
        if (!isLocalPlayer) return;

        GUI.Label(new Rect(10, 10, 400, 20), $"Position: {PlayerState.Position}");
        GUI.Label(new Rect(10, 30, 400, 20), $"Rotation: {PlayerState.Rotation}");
        GUI.Label(new Rect(10, 50, 400, 20), $"Velocity: {PlayerState.Velocity}");
        GUI.Label(new Rect(10, 70, 200, 20), $"IsSliding: {PlayerState.IsSliding}");
        GUI.Label(new Rect(10, 90, 200, 20), $"IsWallRunning: {PlayerState.IsWallRunning}");
        GUI.Label(new Rect(10, 110, 200, 20), $"IsCrouching: {PlayerState.IsCrouching}");
        GUI.Label(new Rect(10, 130, 200, 20), $"IsJumping: {PlayerState.IsJumping}");
        GUI.Label(new Rect(10, 150, 200, 20), $"IsParrying: {PlayerState.IsParrying}");
    }
}