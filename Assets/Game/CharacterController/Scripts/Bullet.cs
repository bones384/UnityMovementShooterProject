using UnityEngine;
namespace Game.CharacterController
{
    public class Bullet : MonoBehaviour
    {
        private void OnCollisionEnter(Collision collision)
        {
            //Destroy(gameObject);
            if (collision.gameObject.CompareTag("Player"))
            {
                print("hit " + collision.gameObject.name);
            }
            if (collision.gameObject)
            {
                print("projectile hit something");
            }
        }
    }
}

