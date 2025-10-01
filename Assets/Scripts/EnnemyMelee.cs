using KevinIglesias;
using System.Collections;
using UnityEngine;

public class EnnemyMelee : MonoBehaviour
{
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private float attackRange = 0.8f;
    [SerializeField] private LayerMask detectionLayer;

    private Animator animator;

    [SerializeField] private Player player;

    private UnityEngine.AI.NavMeshAgent agent;

    [SerializeField] private bool inCoroutine;

    void Start()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
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

    IEnumerator Attack()
    {
        animator.SetTrigger("Attack");
        yield return new WaitForSeconds(1);
        player.Damage();
        animator.ResetTrigger("Attack");
        inCoroutine = false;
    }

    private void FixedUpdate()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, detectionLayer);
        if (hits.Length > 0 && hits[0] != null)
        {
            agent.destination = hits[0].transform.position;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
