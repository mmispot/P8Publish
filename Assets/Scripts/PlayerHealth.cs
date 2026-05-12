using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI healthText;
   
     void Start()
    {
        healthText.text = "Health: " + health.ToString();
    }
     void Update()
    {
        healthText.text = "Health: " + health.ToString();
    }
    public int health = 100;

    public void TakeDamage(int damageAmount)
    {
        health -= damageAmount;
        Debug.Log("Player took damage! Health is now: " + health);
    }
}
