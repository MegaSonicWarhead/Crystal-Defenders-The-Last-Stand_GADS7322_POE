using CrystalDefenders.Combat;
using UnityEngine;

namespace CrystalDefenders.Units
{
	[RequireComponent(typeof(Defender))]
	[RequireComponent(typeof(AutoAttack))]
	[RequireComponent(typeof(GroundAnchor))]
	public class FireMageConfig : MonoBehaviour
	{
		[SerializeField] private GameObject fireballProjectilePrefab;
		[SerializeField] private float range = 5.5f;
		[SerializeField] private float shotsPerSecond = 1.2f;
		[SerializeField] private int damagePerHit = 12;

		private void Awake()
		{
			var aa = GetComponent<AutoAttack>();
			aa.range = range;
			aa.shotsPerSecond = shotsPerSecond;
			aa.damagePerHit = damagePerHit;
			var projField = typeof(AutoAttack).GetField("projectilePrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			if (projField != null && fireballProjectilePrefab != null)
			{
				projField.SetValue(aa, fireballProjectilePrefab);
			}
		}
	}
}


