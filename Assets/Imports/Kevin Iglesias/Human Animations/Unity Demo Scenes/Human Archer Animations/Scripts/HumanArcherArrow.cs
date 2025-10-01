// -- Human Archer Animations 2.0 | Kevin Iglesias --
// This script is a secondary script that works with HumanArcherController.cs script.
// It animates the bow when entering or exiting an AnimatorController state.
// You can freely edit, expand, and repurpose it as needed. To preserve your custom changes when updating
// to future versions, it is recommended to work from a duplicate of this script.

// Contact Support: support@keviniglesias.com

using UnityEngine;

namespace KevinIglesias
{
    public class HumanArcherArrow : MonoBehaviour
    {
        [SerializeField] private float arrowSpeed = 30f;
        private float arrowLifetime = 2f;
        private Rigidbody rb;

        [SerializeField] private LayerMask originLayer;

        void OnEnable()
        {
            Destroy(this.gameObject, arrowLifetime);
            rb = GetComponent<Rigidbody>();
            rb = GetComponent<Rigidbody>();
            rb.linearVelocity = transform.forward * arrowSpeed;
            Destroy(this.gameObject, arrowLifetime);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.layer != originLayer.value)
            {
                if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
                    collision.transform.GetComponent<Player>().Damage();

                else if(collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                    collision.transform.GetComponent<EnnemyHealth>().Damage();

                Destroy(this.gameObject);
            }
        }
    }
}
