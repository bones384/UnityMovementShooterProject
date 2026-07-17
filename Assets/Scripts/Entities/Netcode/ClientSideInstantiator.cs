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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        if (Application.isBatchMode)
        {
            Debug.Log("Server");

            return;
        }

        Debug.Log("Not server");
        mainCamera = Camera.main;
        // this.gameObject.AddComponent<PlayerBridge>();
    }

    // Update is called once per frame
    private void Update()
    {
    }
}