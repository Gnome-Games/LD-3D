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

            transform.rotation = newRot;

            animIdle = false;
            animator.SetTrigger("ShootFast");
            animator.ResetTrigger("CancelShoot");
        }
        else
        {
            if (!animIdle)
            {
                animIdle = true;
                animator.ResetTrigger("ShootFast");
                animator.SetTrigger("CancelShoot");
            }
        }
    }

    private void FixedUpdate()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, detectionLayer);
        if (hits.Length > 0 && hits[0] != null)
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
