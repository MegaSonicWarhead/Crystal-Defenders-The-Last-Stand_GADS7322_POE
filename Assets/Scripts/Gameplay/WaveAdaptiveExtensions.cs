using UnityEngine;

namespace CrystalDefenders.Gameplay
{
    public static class WaveAdaptiveExtensions
    {
        // Scales enemy HP difficulty
        public static float ComputeDifficultyMultiplier(int wave, int playerResources, int towerHealth)
        {
            // Slower start, smoother ramp
            float waveFactor = 1f + (wave - 1) * 0.08f; // +8% per wave instead of 10%

            // Resources make enemies slightly tougher, but only past 400 resources
            float resourceFactor = 1f + Mathf.Clamp01((playerResources - 400f) / 600f) * 0.3f; // up to +30%

            // Reduce difficulty if tower is hurt — stronger safety brake
            float towerFactor = Mathf.Clamp01((1000 - towerHealth) / 1000f);
            float safetyBrake = 1f - 0.5f * towerFactor; // 50% reduction at 0 HP

            // Ensure first few waves stay mild
            float earlyGameEase = wave == 1 ? 1f : wave == 2 ? 0.9f : 1f;

            return waveFactor * resourceFactor * safetyBrake * earlyGameEase;
        }

        // Controls number of enemies per wave
        public static float ComputeSpawnMultiplier(int wave, int playerResources, int towerHealth)
        {
            // Very small per-wave increment
            float baseFactor = 1f + (wave - 1) * 0.03f;

            // Resource scaling — starts later and ramps slower
            float resourceFactor = 1f + Mathf.Clamp01((playerResources - 300f) / 600f) * 0.4f;

            // Tower health check (if tower is strong, slightly more enemies)
            float hpFactor = towerHealth > 800 ? 1.15f :
                             towerHealth > 500 ? 1.0f :
                             0.85f;

            // Smooth clamp to avoid spikes
            return Mathf.Clamp(baseFactor * resourceFactor * hpFactor, 0.7f, 1.6f);
        }

        // Defender pressure: slightly softer curve
        public static float ComputeDefenderPressureAdjustment(float defenderHealthFactor)
        {
            return defenderHealthFactor >= 0.8f ? 1.15f :   // healthy defenses → +15%
                   defenderHealthFactor >= 0.5f ? 1.0f :    // neutral
                   0.8f;                                    // weak → -20%
        }

        // When to spawn all enemy types
        public static bool ShouldSpawnAllEnemyTypes(int wave, int playerResources, int towerHealth, int thresholdWave = 5)
        {
            float diff = ComputeDifficultyMultiplier(wave, playerResources, towerHealth);
            bool doingWell = diff < 1.3f;
            return wave >= thresholdWave || (wave % 2 == 0 && doingWell);
        }
    }
}