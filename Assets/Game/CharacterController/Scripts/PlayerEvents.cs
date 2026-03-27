using System.Collections;
using UnityEngine;
using UnityEngine.Events;
namespace Game.CharacterController
{
    public class PlayerEvents : MonoBehaviour
    {
        public UnityEvent playerDeath;
        public PlayerController _playerController;
        void Start()
        {
            if (playerDeath != null)
            {
                playerDeath = new UnityEvent();
            }
            playerDeath.AddListener(Death);

            _playerController = GetComponent<PlayerController>();
        }

        void Update()
        {
            if (_playerController.playerHP <= 0 && playerDeath != null)
            {
                playerDeath.Invoke();
            }
        }
        void Death()
        {
            _playerController.playerHP = 100;
            GetComponent<PlayerController>().enabled = false;
            print("Player died");
            StartCoroutine(Respawn());
        }
        IEnumerator Respawn()
        {
            yield return new WaitForSeconds(4);
            GetComponent<PlayerController>().enabled = true;
            _playerController.transform.position = Vector3.zero;    // For now respawn at (0,0,0)
            print("Player respawned");
        }
    }
}
