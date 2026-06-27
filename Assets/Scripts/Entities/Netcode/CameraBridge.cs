using UnityEngine;

public class CameraBridge : MonoBehaviour
{
    public Vector3 TargetPosition = new(0, 1.8f, 0);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
    }

    private void LateUpdate()
    {
        /*transform.position =
            Vector3.Lerp(
                transform.position,
                (Vector3)TargetPosition + new Vector3(0, 5, -10),
                Time.deltaTime * 10f);*/
        if (PlayerVisualisationManager.LocalPlayer == null) return;
        var localPlayer = PlayerVisualisationManager.LocalPlayer.Value;

        transform.position = PlayerVisualisationManager.PlayerViewRegistry.Views[localPlayer].transform.position +
                             TargetPosition;
    }
}