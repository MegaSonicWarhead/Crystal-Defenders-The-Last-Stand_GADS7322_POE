using System.Collections;
using UnityEngine;

namespace CrystalDefenders.Combat
{
	public class PoisonArrowProjectile : Projectile
	{
		[SerializeField] private int poisonTickDamage = 2;
		[SerializeField] private float poisonTickInterval = 0.5f;
		[SerializeField] private float poisonDuration = 4f;

		private void Reset()
		{
			damageTag = "poison";
		}

		protected override void OnHit(Health health)
		{
			base.OnHit(health);
			if (health != null && gameObject.activeInHierarchy)
			{
				StartCoroutine(ApplyPoison(health));
			}
		}

		private IEnumerator ApplyPoison(Health targetHealth)
		{
			float elapsed = 0f;
			while (elapsed < poisonDuration && targetHealth != null && targetHealth.CurrentHealth > 0)
			{
				if (!string.IsNullOrEmpty(damageTag))
					targetHealth.ApplyDamage(poisonTickDamage, damageTag);
				else
					targetHealth.ApplyDamage(poisonTickDamage);
				yield return new WaitForSeconds(poisonTickInterval);
				elapsed += poisonTickInterval;
			}
		}
	}
}


