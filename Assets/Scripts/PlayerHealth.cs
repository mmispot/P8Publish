using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int health = 100;

    public void TakeDamage(int damageAmount)
    {
        health -= damageAmount;
        Debug.Log("Player took damage! Health is now: " + health);
    }
}
