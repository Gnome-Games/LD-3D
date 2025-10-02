using KevinIglesias;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnnemyMeleeMovement : MonoBehaviour
{
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private float attackRange = 0.8f;
    [SerializeField] private LayerMask detectionLayer;

    private Animator animator;

    [SerializeField] private PlayerHealth playerHealth;

    private NavMeshAgent agent;

    [SerializeField] private bool inCoroutine;

    private bool followingPlayer = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }


    void Update()
    {
        animator.SetFloat("Speed", agent.velocity.magnitude);

        if(agent.remainingDistance < attackRange && agent.destination != transform.position && !inCoroutine)
        {
            StartCoroutine(Attack());
            inCoroutine = true;
        }
    }

    private void FixedUpdate()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, detectionLayer);
        if (hits.Length > 0 && hits[0] != null && hits[0].tag == "Player")
        {
            followingPlayer = true;
            agent.destination = hits[0].transform.position;
        }
        else
        {
            followingPlayer = false;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    IEnumerator Attack()
    {
        if(!followingPlayer)
            yield break;

        animator.SetTrigger("Attack");
        yield return new WaitForSeconds(1);
        if(playerHealth)
            playerHealth.Damage();
        animator.ResetTrigger("Attack");
        inCoroutine = false;
    }
}
