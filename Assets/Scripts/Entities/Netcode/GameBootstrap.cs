using Unity.NetCode;
using UnityEngine;
using UnityEngine.Scripting;

namespace Entities.Netcode
{
    [Preserve]
    public class GameBootstrap : ClientServerBootstrap
    {
        public override bool Initialize(string defaultWorldName)
        {
            return false;
            Cursor.lockState = CursorLockMode.Locked;
            AutoConnectPort = 7979;
            return base.Initialize(defaultWorldName);
        }
    }
}