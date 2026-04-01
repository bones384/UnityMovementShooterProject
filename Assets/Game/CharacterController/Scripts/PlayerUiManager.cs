using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Game.CharacterController
{
    public class PlayerUiManager : MonoBehaviour
    {
        public static PlayerUiManager Instance { get; set; }

        // UI
        public TextMeshProUGUI ammoDisplay;
        public Image reloadProgressBar;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }
    }
}

