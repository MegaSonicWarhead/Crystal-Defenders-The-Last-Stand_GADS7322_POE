using CrystalDefenders.Combat;
using UnityEngine;

namespace CrystalDefenders.Units
{
	[RequireComponent(typeof(Defender))]
	[RequireComponent(typeof(AutoAttack))]
	[RequireComponent(typeof(GroundAnchor))]
	public class PoisonArcherConfig : MonoBehaviour
	{
		[SerializeField] private GameObject poisonArrowProjectilePrefab;
		[SerializeField] private float range = 6f;
		[SerializeField] private float shotsPerSecond = 1.8f;
		[SerializeField] private int damagePerHit = 10;

		private void Awake()
		{
			var aa = GetComponent<AutoAttack>();
			aa.range = range;
			aa.shotsPerSecond = shotsPerSecond;
			aa.damagePerHit = damagePerHit;
			// Assign projectile via reflection-safe field
			var projField = typeof(AutoAttack).GetField("projectilePrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			if (projField != null && poisonArrowProjectilePrefab != null)
			{
				projField.SetValue(aa, poisonArrowProjectilePrefab);
			}
		}
	}
}


