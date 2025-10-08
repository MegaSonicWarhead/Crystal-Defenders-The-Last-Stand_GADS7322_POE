using CrystalDefenders.Combat;
using UnityEngine;

namespace CrystalDefenders.Units
{
	[RequireComponent(typeof(Health))]
	[RequireComponent(typeof(AutoAttack))]
	public class EnemyRanged : Enemy
	{
		private void Awake()
		{
			var aa = GetComponent<AutoAttack>();
			aa.range = 6.5f;
			aa.shotsPerSecond = 0.8f;
			aa.damagePerHit = 8;
			var h = GetComponent<Health>();
			h.requiredDamageTag = "fire"; // only fire damages this enemy
		}

		private new void Update()
		{
			// Move toward tower but also uses AutoAttack to shoot
			base.GetType(); // no-op to avoid warnings
			base.SendMessage("MoveAlongPath", SendMessageOptions.DontRequireReceiver);
			// Enemy base TryAttackTargets will handle contact; ranged attacks handled by AutoAttack
		}
	}
}


