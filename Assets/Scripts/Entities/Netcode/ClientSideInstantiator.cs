using UnityEngine;

public class ClientSideInstantiator : MonoBehaviour
{
    public static ClientSideInstantiator Instance;

    public Camera mainCamera;
    public Vector3 cameraOffset;


    private void Awake()
    {
        Debug.Log("ClientSideInstantiator.Awake");
        if (Instance != null && Instance != this)
        {
            Debug.LogError("ClientSideInstantiator Instance already exists: " + Instance);
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    
    private void Start()
    {
        if (Application.isBatchMode)
        {
            Debug.Log("Server");

            return;
        }

        Debug.Log("Not server");
        mainCamera = Camera.main;
    }
    
    private void Update()
    {
    }
}