using UnityEngine;

namespace CrystalDefenders.Gameplay
{
    public static class WaveAdaptiveExtensions
    {
        // Computes difficulty multiplier for health scaling
        public static float ComputeDifficultyMultiplier(int wave, int playerResources, int towerHealth)
        {
            float waveFactor = 1f + (wave - 1) * 0.1f;
            float resourceFactor = Mathf.Clamp01(playerResources / 500f) * 0.5f + 1f; // up to +50%
            float towerFactor = Mathf.Clamp01((1000 - towerHealth) / 1000f);
            float safetyBrake = 1f - 0.4f * towerFactor; // reduce diff when tower is hurt
            return waveFactor * resourceFactor * safetyBrake;
        }

        // Computes spawn multiplier for adaptive enemy count
        public static float ComputeSpawnMultiplier(int wave, int playerResources, int towerHealth)
        {
            float baseFactor = 1f + (wave - 1) * 0.05f;

            // More resources = increase pressure
            float resourceFactor = Mathf.Clamp01(playerResources / 600f) * 0.6f + 1f; // up to +60%

            // If tower HP is HIGH, increase pressure. If it's low, ease off.
            float hpFactor = towerHealth > 800 ? 1.3f : towerHealth > 500 ? 1.1f : 0.85f;

            return baseFactor * resourceFactor * hpFactor;
        }

        // Determines whether all enemy types should spawn this wave
        public static bool ShouldSpawnAllEnemyTypes(int wave, int playerResources, int towerHealth, int thresholdWave = 5)
        {
            // Player is considered doing well if difficulty multiplier < 1.2 (optional tweakable)
            float diff = ComputeDifficultyMultiplier(wave, playerResources, towerHealth);
            bool playerDoingWell = diff < 1.2f;

            // Spawn all enemies if past threshold wave OR even wave + player doing well
            return wave >= thresholdWave || (wave % 2 == 0 && playerDoingWell);
        }
    }
}