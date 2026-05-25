using System;
using Entities.Netcode;
using Unity.Entities;
using UnityEngine;

public class ClientSideInstantiator : MonoBehaviour
{
    public static ClientSideInstantiator Instance;

    public Camera mainCamera;
    public Vector3 cameraOffset;
    
    
    private void Awake()
    {
        if (Instance is null) Instance = this;
        else Destroy(gameObject);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Application.isBatchMode)        {
            Debug.Log("Server");

            return;
        }
        Debug.Log("Not server");
        mainCamera = Camera.main;
       // this.gameObject.AddComponent<PlayerBridge>();

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
