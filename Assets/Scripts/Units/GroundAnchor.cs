using UnityEngine;

namespace CrystalDefenders.Units
{
	[DisallowMultipleComponent]
	public class GroundAnchor : MonoBehaviour
	{
		[SerializeField] private float snapProbeHeight = 5f;
		[SerializeField] private float snapProbeDistance = 50f;

		private void Start()
		{
			SnapToGround();
			ConfigureArticulationBodies();
		}

		private void SnapToGround()
		{
			Vector3 probeStart = transform.position + Vector3.up * snapProbeHeight;
			if (Physics.Raycast(probeStart, Vector3.down, out RaycastHit hit, snapProbeDistance))
			{
				transform.position = hit.point;
			}
		}

		private void ConfigureArticulationBodies()
		{
			var bodies = GetComponentsInChildren<ArticulationBody>(true);
			for (int i = 0; i < bodies.Length; i++)
			{
				var body = bodies[i];
				body.useGravity = false;
				body.immovable = true;
				body.linearDamping = Mathf.Max(body.linearDamping, 1f);
				body.angularDamping = Mathf.Max(body.angularDamping, 1f);
			}
		}
	}
}


