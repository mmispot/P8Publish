//"67" - Milan 17 - 03 - 2026
//Tutorial that I used: https://youtu.be/woXLV8cIe7s?si=b24wDIp8BBeuNMQz
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform bulletSpawn;
    public float bulletVelocity = 30;
    public float bulletPrefabLifeTime = 3f;

    void Update()
    {
        //Left mouse click
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            FireWeapon();
        }

    }

    private void FireWeapon()
    {
        //Instantiate bullet
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawn.position, Quaternion.identity);

        //Shoot bullet
        bullet.GetComponent<Rigidbody>().AddForce(bulletSpawn.forward.normalized * bulletVelocity, ForceMode.Impulse);

        //Destroy bullet after a few sec
        StartCoroutine(DestroyBulletAfterTime(bullet, bulletPrefabLifeTime));


    }

    private IEnumerator DestroyBulletAfterTime(GameObject bullet, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(bullet);
    }


}
