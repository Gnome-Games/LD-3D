using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMeleeMovement : MonoBehaviour
{
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private float attackRange = 0.8f;
    [SerializeField] private LayerMask detectionLayer;

    private PlayerHealth playerHealth;

    private NavMeshAgent agent;
    private Animator animator;

    private bool inCoroutine = false;
    private bool followingPlayer = false;
    private Transform targetPlayer;

    void Start()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        animator.SetFloat("Speed", agent.velocity.magnitude);

        DetectPlayer();

        if (followingPlayer && targetPlayer != null)
        {
            agent.destination = targetPlayer.position;

            if (agent.hasPath && agent.remainingDistance <= attackRange && !inCoroutine && !playerHealth.IsDead())
            {
                StartCoroutine(Attack());
            }
        }
    }

    private void DetectPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, detectionLayer);

        Transform closestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestTarget = hit.transform;
                }
            }
        }

        if (closestTarget != null)
        {
            targetPlayer = closestTarget;
            playerHealth = targetPlayer.GetComponent<PlayerHealth>();
            followingPlayer = true;
        }
        else
        {
            followingPlayer = false;
            playerHealth = null;
            targetPlayer = null;
        }
    }

    IEnumerator Attack()
    {
        inCoroutine = true;

        if (!followingPlayer)
        {
            inCoroutine = false;
            yield break;
        }

        agent.isStopped = true;
        animator.SetTrigger("Attack");


        // Wait for damage timing
        yield return new WaitForSeconds(0.5f);

        if (playerHealth != null)
            playerHealth.Damage(true);

        // Wait until attack animation ends
        yield return new WaitForSeconds(0.5f);


        agent.isStopped = false;
        inCoroutine = false;
    }
}
