using System;
using System.Linq;
using CrystalDefenders.Combat;
using CrystalDefenders.Units;
using UnityEngine;

namespace CrystalDefenders.Gameplay
{
	public static class WaveAdaptiveExtensions
	{
		public static float ComputeDifficultyMultiplier(int wave, int playerResources, int towerHealth)
		{
			float waveFactor = 1f + (wave - 1) * 0.1f;
			float resourceFactor = Mathf.Clamp01(playerResources / 500f) * 0.5f + 1f; // up to +50%
			float towerFactor = Mathf.Clamp01((1000 - towerHealth) / 1000f);
			float safetyBrake = 1f - 0.4f * towerFactor; // reduce diff when tower is hurt
			return waveFactor * resourceFactor * safetyBrake;
		}
	}
}


