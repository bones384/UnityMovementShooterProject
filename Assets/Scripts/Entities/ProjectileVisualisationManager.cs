using UnityEngine;

public class ProjectileVisualisationManager : MonoBehaviour
{
    public static ProjectileVisualisationManager Instance;
    
    public GameObject rocketTrailPrefab;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
}