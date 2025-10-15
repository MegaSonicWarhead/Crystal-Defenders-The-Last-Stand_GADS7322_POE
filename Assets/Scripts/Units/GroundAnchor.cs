using UnityEngine;

namespace CrystalDefenders.Units
{
    /// <summary>
    /// Ensures a defender or object is properly positioned on the ground
    /// and configures ArticulationBodies for stability.
    /// </summary>
    [DisallowMultipleComponent]
    public class GroundAnchor : MonoBehaviour
    {
        [Header("Snap Settings")]
        [SerializeField] private float snapProbeHeight = 5f;     // Start raycast above object
        [SerializeField] private float snapProbeDistance = 50f;  // Max distance to probe downward

        private void Start()
        {
            SnapToGround();
            ConfigureArticulationBodies();
        }

        /// <summary>
        /// Raycasts downward and snaps object to terrain or surface below.
        /// </summary>
        private void SnapToGround()
        {
            Vector3 probeStart = transform.position + Vector3.up * snapProbeHeight;
            if (Physics.Raycast(probeStart, Vector3.down, out RaycastHit hit, snapProbeDistance))
            {
                transform.position = hit.point;
            }
        }

        /// <summary>
        /// Adjusts ArticulationBodies to be immovable and stable.
        /// </summary>
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