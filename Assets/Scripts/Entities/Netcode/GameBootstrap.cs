using UnityEngine;
using Unity.NetCode;


namespace Entities.Netcode
{
    [UnityEngine.Scripting.Preserve]
    public class GameBootstrap : ClientServerBootstrap
    {
        public override bool Initialize(string defaultWorldName)
        {
            Cursor.lockState = CursorLockMode.Locked; 
            AutoConnectPort = 7979;
            return base.Initialize(defaultWorldName);
        }
    }
}