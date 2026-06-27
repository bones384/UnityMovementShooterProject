using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class PlayerVisualisationManager : MonoBehaviour
{
    public static GameObject PlayerViewPrefab;
    public static Entity? LocalPlayer = null;

    public GameObject playerPrefab;
    private PlayerVisualisationManager _instance;


    public void Awake()
    {
        if (_instance is null)
        {
            _instance = this;
            PlayerViewPrefab = playerPrefab;
        }
        else
        {
            Destroy(gameObject);
        }
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