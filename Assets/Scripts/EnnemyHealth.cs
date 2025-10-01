using UnityEngine;
using UnityEngine.UI;

public class EnnemyHealth : MonoBehaviour
{
    public int m_Health = 3;

    public Slider healthbar;
    
    public void Damage()
    {
        m_Health -= 1;
        healthbar.value = m_Health;
        healthbar.fillRect.GetComponent<Image>().color = m_Health == 2 ? Color.yellow : Color.red;

        if (m_Health <= 0)
        {
            Destroy(transform.parent.gameObject);
        }
    }
}
