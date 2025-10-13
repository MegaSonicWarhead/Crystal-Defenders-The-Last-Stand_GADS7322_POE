using CrystalDefenders.Combat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 12f;
    [SerializeField] public int damage;
    [SerializeField] public Transform target;
    [SerializeField] public string damageTag = null; //  "poison" or "fire"

    // Use this to initialize the projectile with target and damage
    public virtual void Initialize(Transform target, int damage, string tag = null)
    {
        this.target = target;
        this.damage = damage;

        // Ensure damageTag is set
        if (!string.IsNullOrEmpty(tag))
            this.damageTag = tag;
    }

    protected virtual void Update()
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
            OnImpact(transform.position, target);
            Destroy(gameObject);
        }
    }

    protected virtual void OnImpact(Vector3 hitPosition, Transform hitTarget)
    {
        var h = hitTarget != null ? hitTarget.GetComponent<Health>() : null;
        if (h != null)
        {
            OnHit(h);
        }
    }

    protected virtual void OnHit(Health health)
    {
        if (health == null) return;

        // Debug log to verify correct tags
        Debug.Log($"Projectile hitting {health.gameObject.name} | damageTag={damageTag} | enemy requiredTag={health.requiredDamageTag}");

        // Apply damage respecting requiredDamageTag
        if (!string.IsNullOrEmpty(damageTag))
            health.ApplyDamage(damage, damageTag);
        else
            health.ApplyDamage(damage);
    }
}