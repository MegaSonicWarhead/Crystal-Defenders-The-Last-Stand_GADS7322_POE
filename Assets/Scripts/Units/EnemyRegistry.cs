using System.Collections.Generic;
using UnityEngine;

namespace CrystalDefenders.Units
{
    /// <summary>
    /// Global registry for all active enemies in the scene.
    /// Useful for towers or systems that need to iterate over current enemies.
    /// </summary>
    public static class EnemyRegistry
    {
        // Internal list storing all active enemies
        private static readonly List<Enemy> enemies = new List<Enemy>();

        /// <summary>
        /// Read-only access to the current active enemies
        /// </summary>
        public static IReadOnlyList<Enemy> Enemies => enemies;

        /// <summary>
        /// Register an enemy to the global list
        /// </summary>
        public static void Register(Enemy e)
        {
            if (e != null && !enemies.Contains(e))
                enemies.Add(e);
        }

        /// <summary>
        /// Unregister an enemy from the global list
        /// </summary>
        public static void Unregister(Enemy e)
        {
            if (e != null)
                enemies.Remove(e);
        }
    }
}