using UnityEngine;

namespace CrystalDefenders.Gameplay
{
    /// <summary>
    /// Provides adaptive scaling logic for waves in the game.
    /// Calculates multipliers for enemy health, spawn counts, and defensive pressure.
    /// </summary>
    public static class WaveAdaptiveExtensions
    {
        /// <summary>
        /// Computes the overall difficulty multiplier for enemies based on wave, player resources, and tower health.
        /// </summary>
        /// <param name="wave">Current wave number.</param>
        /// <param name="playerResources">Player's accumulated resources.</param>
        /// <param name="towerHealth">Current health of the main tower.</param>
        /// <returns>Difficulty multiplier to scale enemy HP.</returns>
        public static float ComputeDifficultyMultiplier(int wave, int playerResources, int towerHealth)
        {
            // Wave-based scaling: slower initial ramp for early waves
            float waveFactor = 1f + (wave - 1) * 0.08f; // +8% per wave

            // Resource-based scaling: enemies get tougher as resources increase, capped at +30%
            float resourceFactor = 1f + Mathf.Clamp01((playerResources - 400f) / 600f) * 0.3f;

            // Tower health safety brake: reduce difficulty if tower is heavily damaged
            float towerFactor = Mathf.Clamp01((1000 - towerHealth) / 1000f);
            float safetyBrake = 1f - 0.5f * towerFactor; // 50% reduction if tower at 0 HP

            // Early game adjustment to keep first few waves manageable
            float earlyGameEase = wave == 1 ? 1f : wave == 2 ? 0.9f : 1f;

            // Final difficulty multiplier
            return waveFactor * resourceFactor * safetyBrake * earlyGameEase;
        }

        /// <summary>
        /// Computes the spawn multiplier to determine the number of enemies per wave.
        /// </summary>
        /// <param name="wave">Current wave number.</param>
        /// <param name="playerResources">Player's accumulated resources.</param>
        /// <param name="towerHealth">Current health of the main tower.</param>
        /// <returns>Spawn multiplier for enemy count.</returns>
        public static float ComputeSpawnMultiplier(int wave, int playerResources, int towerHealth)
        {
            // Base wave scaling: small increment per wave
            float baseFactor = 1f + (wave - 1) * 0.03f;

            // Resource-based scaling: slightly delayed start and slower ramp
            float resourceFactor = 1f + Mathf.Clamp01((playerResources - 300f) / 600f) * 0.4f;

            // Tower health influence: more enemies if tower is strong, fewer if weak
            float hpFactor = towerHealth > 800 ? 1.15f :
                             towerHealth > 500 ? 1.0f :
                             0.85f;

            // Clamp final multiplier to prevent extreme spikes
            return Mathf.Clamp(baseFactor * resourceFactor * hpFactor, 0.7f, 1.6f);
        }

        /// <summary>
        /// Adjusts difficulty based on defender health pressure.
        /// </summary>
        /// <param name="defenderHealthFactor">Normalized defender health ratio (0-1).</param>
        /// <returns>Multiplier for adjusting enemy pressure against defenders.</returns>
        public static float ComputeDefenderPressureAdjustment(float defenderHealthFactor)
        {
            return defenderHealthFactor >= 0.8f ? 1.15f :   // strong defenses → +15%
                   defenderHealthFactor >= 0.5f ? 1.0f :    // moderate → no change
                   0.8f;                                    // weak → -20%
        }

        /// <summary>
        /// Determines whether all enemy types should spawn this wave.
        /// </summary>
        /// <param name="wave">Current wave number.</param>
        /// <param name="playerResources">Player's accumulated resources.</param>
        /// <param name="towerHealth">Current health of the main tower.</param>
        /// <param name="thresholdWave">Wave threshold to force all enemy types to appear.</param>
        /// <returns>True if all enemy types should spawn, otherwise false.</returns>
        public static bool ShouldSpawnAllEnemyTypes(int wave, int playerResources, int towerHealth, int thresholdWave = 5)
        {
            // Compute current difficulty
            float diff = ComputeDifficultyMultiplier(wave, playerResources, towerHealth);

            // Condition: doing well → difficulty is moderate
            bool doingWell = diff < 1.3f;

            // Spawn all enemy types either after thresholdWave or on even waves if player is doing well
            return wave >= thresholdWave || (wave % 2 == 0 && doingWell);
        }
    }
}