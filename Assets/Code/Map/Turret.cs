using UnityEngine;

public class Turret : MonoBehaviour
{
    public GameObject bulletPrefab;   // 子弹预制体
    public Transform firePoint;       // 炮口位置
    public float fireRate = 1f;       // 射击间隔（秒）
    public float bulletSpeed = 10f;   // 子弹速度

    private float fireTimer;

    private void Update()
    {
        fireTimer += Time.deltaTime;

        if (fireTimer >= fireRate)
        {
            Shoot();
            fireTimer = 0f;
        }
    }

    void Shoot()
    {
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogError("[Turret] 缺少 prefab 或 firePoint！");
            return;
        }

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = firePoint.right * bulletSpeed; //  子弹沿炮口方向发射
        }

        //Debug.Log("[Turret] Fired bullet at " + firePoint.position);
    }
}
