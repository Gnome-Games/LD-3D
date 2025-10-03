using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    private int health = 3;

    [Header("UI")]
    [SerializeField] private Slider healthbar;
    [SerializeField] private GameObject deadScreen;

    [SerializeField] private GameObject bloodPrefab;

    [SerializeField] private Transform swordContactPoint;


    public bool IsDead()
    {
        return health <= 0;
    }

    public void Damage(bool isMeleeDamage = false)
    {
        health -= 1;
        healthbar.value = health;
        healthbar.fillRect.GetComponent<Image>().color = health == 2 ? Color.yellow : Color.red;

        if (isMeleeDamage) {
            Instantiate(bloodPrefab, swordContactPoint.position, Quaternion.identity);
        }

        if (health <= 0)
        {
            Die();
        }
    }

    public void Die()
    {

        StartCoroutine(DieCoroutine());
    }

    private IEnumerator DieCoroutine()
    {
        healthbar.gameObject.SetActive(false);
        yield return new WaitForSeconds(2);
        if (deadScreen != null)
            deadScreen.SetActive(true);
        yield return new WaitForSeconds(1);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
