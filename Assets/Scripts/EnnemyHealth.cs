using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnnemyHealth : MonoBehaviour
{
    public int m_Health = 3;

    public Slider healthbar;

    public GameObject bloodVFX;

    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player") && collision.gameObject.tag == "Arrow")
        {
            Vector3 arrowRot = collision.gameObject.transform.rotation.eulerAngles;

            arrowRot.y += 180;

            Instantiate(bloodVFX, collision.contacts[0].point, Quaternion.Euler(arrowRot));
            Damage();
        }
    }

    public void Damage()
    {
        m_Health -= 1;
        healthbar.value = m_Health;
        healthbar.fillRect.GetComponent<Image>().color = m_Health == 2 ? Color.yellow : Color.red;

        if (m_Health <= 0)
        {
            healthbar.gameObject.SetActive(false);
            StartCoroutine(Die());
        }
    }

    private IEnumerator Die()
    {
        yield return new WaitForSeconds(0.8f);
        Destroy(transform.parent.gameObject);
    }
}
