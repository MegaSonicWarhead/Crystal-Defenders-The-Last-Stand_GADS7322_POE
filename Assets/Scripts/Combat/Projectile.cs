using CrystalDefenders.Combat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 12f;
    private int damage;
    private Transform target;

    public void Initialize(Transform target, int damage)
    {
        this.target = target;
        this.damage = damage;
    }

    private void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Move toward target
        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );

        // Check if reached target
        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            var h = target.GetComponent<Health>();
            if (h != null) h.ApplyDamage(damage);

            Destroy(gameObject);
        }
    }
}
