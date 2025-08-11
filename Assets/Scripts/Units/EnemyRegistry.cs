using System.Collections.Generic;
using UnityEngine;

namespace CrystalDefenders.Units
{
    public static class EnemyRegistry
    {
        private static readonly List<Enemy> enemies = new List<Enemy>();
        public static IReadOnlyList<Enemy> Enemies => enemies;

        public static void Register(Enemy e)
        {
            if (e != null && !enemies.Contains(e)) enemies.Add(e);
        }

        public static void Unregister(Enemy e)
        {
            if (e != null) enemies.Remove(e);
        }
    }
}


