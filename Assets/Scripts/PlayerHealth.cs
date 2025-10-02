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

    public void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.layer == LayerMask.NameToLayer("Ennemy") && collision.gameObject.tag == "Arrow")
        {
            Damage();
        }
    }

    public void Damage()
    {
        health -= 1;
        healthbar.value = health;
        healthbar.fillRect.GetComponent<Image>().color = health == 2 ? Color.yellow : Color.red;

        if (health <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (deadScreen != null)
            deadScreen.SetActive(true);

        StartCoroutine(DieCoroutine());
    }

    private IEnumerator DieCoroutine()
    {
        yield return new WaitForSeconds(2);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
