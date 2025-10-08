using System.Collections.Generic;
using UnityEngine;

namespace CrystalDefenders.Units
{
	[RequireComponent(typeof(CrystalDefenders.Combat.Health))]
	public class EnemyFast : Enemy
	{
		private void Awake()
		{
			var h = GetComponent<CrystalDefenders.Combat.Health>();
			h.requiredDamageTag = "poison"; // only poison damages this enemy
		}
		private void Reset()
		{
			moveSpeed = 5.5f; // faster than base
			contactDamage = 6; // lower damage
			attackRange = 4f;
			attackCooldown = 0.8f;
		}
	}
}


