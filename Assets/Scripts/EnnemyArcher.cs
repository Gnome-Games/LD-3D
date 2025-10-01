using KevinIglesias;
using UnityEngine;

public class EnnemyArcher : MonoBehaviour
{
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private LayerMask detectionLayer;

    private Transform player;

    private bool animIdle = true;

    private HumanArcherController controller;
    private Animator animator;

    private 

    void Start()
    {
        controller = GetComponent<HumanArcherController>();
        animator = GetComponent<Animator>();
    }


    void Update()
    {
        if (player != transform && player != null)
        {
            Quaternion newRot = Quaternion.LookRotation(player.position - transform.position);

            Vector3 eulerRotation = newRot.eulerAngles;
            transform.rotation = Quaternion.Euler(0, eulerRotation.y, 0);

            animIdle = false;
            animator.SetTrigger("Shoot");
            animator.ResetTrigger("CancelShoot");
        }
        else
        {
            if (!animIdle)
            {
                animIdle = true;
                animator.ResetTrigger("Shoot");
                animator.SetTrigger("CancelShoot");
            }
        }
    }

    private void FixedUpdate()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, detectionLayer);
        if (hits.Length > 0 && hits[0] != null && hits[0].tag == "Player")
        {
            player = hits[0].transform;
        }
        else
            player = transform;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
