using UnityEngine;

public class ProjectileVisualisationManager : MonoBehaviour
{
    public static ProjectileVisualisationManager Instance;

    [Tooltip("The GameObject containing your Particle System")]
    public GameObject rocketTrailPrefab;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
}