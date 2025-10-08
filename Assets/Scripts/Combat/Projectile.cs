using CrystalDefenders.Combat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
	public float speed = 12f;
	[SerializeField] protected int damage;
	[SerializeField] protected Transform target;
	[SerializeField] protected string damageTag = null; // e.g. "poison" or "fire"

	public virtual void Initialize(Transform target, int damage)
	{
		this.target = target;
		this.damage = damage;
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
		if (!string.IsNullOrEmpty(damageTag))
			health.ApplyDamage(damage, damageTag);
		else
			health.ApplyDamage(damage);
	}
}
