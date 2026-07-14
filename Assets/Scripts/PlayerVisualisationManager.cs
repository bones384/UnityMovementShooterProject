using System;
using System.Collections.Generic;
using Entities.Netcode;
using Unity.Cinemachine;
using Unity.Cinemachine.Editor;
using Unity.Entities;
using UnityEngine;

public class PlayerVisualisationManager : MonoBehaviour
{
    public static GameObject PlayerViewPrefab;
    public static PlayerStateComponent? LocalPlayer = null;

    public GameObject playerPrefab;
    public static PlayerVisualisationManager Instance;
    public Material friendMaterial;
    public Material enemyMaterial;

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            PlayerViewPrefab = playerPrefab;

        }
    }

    public void Start()
    {
    }

    public static PlayerVisualisation CreatePlayerView()
    {
        return Instantiate(PlayerViewPrefab)
            .GetComponent<PlayerVisualisation>();
    }

    public static class PlayerViewRegistry
    {
        public static Dictionary<Entity, PlayerVisualisation> Views
            = new();
    }
}