using System;
using System.Collections.Generic;

namespace CalradiaForge.Core.SDK.Combat
{
    public static class ForgeFormationController
    {
        public static void Initialize() { }

        public static float CalculateFormationWidth(int troopCount, float interval, int rankCount)
        {
            if (rankCount <= 0) rankCount = 1;
            int troopsPerRank = (int)Math.Ceiling((double)troopCount / rankCount);
            return troopsPerRank * interval;
        }

        public static float CalculateOptimalDepth(int troopCount, bool expectCavalryCharge)
        {
            if (expectCavalryCharge)
            {
                // Deep ranks absorb cavalry momentum
                return Math.Min(6f, Math.Max(3f, (float)Math.Ceiling(troopCount / 20.0)));
            }
            // Thin lines maximize missile or front-line melee coverage
            return 2f;
        }
    }

    public static class ForgeSiegeEngineManager
    {
        public static void Initialize() { }

        public static float CalculateWallDamage(string engineType, int wallTier, float distance)
        {
            float baseDmg = 100f;
            if (string.Equals(engineType, "Trebuchet", StringComparison.OrdinalIgnoreCase)) baseDmg = 250f;
            else if (string.Equals(engineType, "Onager", StringComparison.OrdinalIgnoreCase)) baseDmg = 160f;
            else if (string.Equals(engineType, "Ballista", StringComparison.OrdinalIgnoreCase)) baseDmg = 40f;

            float wallResistance = wallTier * 0.25f + 1.0f;
            float distancePenalty = Math.Max(0.5f, 1.0f - (distance / 400f) * 0.3f);
            return (baseDmg / wallResistance) * distancePenalty;
        }
    }

    public static class ForgeDamageModifier
    {
        public static void Initialize() { }

        public static float CalculateEffectiveDamage(float rawDamage, string damageType, float targetArmor)
        {
            // Bannerlord Armor calculation:
            // Cut suffers high armor reduction.
            // Pierce has medium penetration.
            // Blunt penetrates heavy armor most effectively.
            float soakFactor = 1.0f;
            if (string.Equals(damageType, "Cut", StringComparison.OrdinalIgnoreCase))
            {
                soakFactor = 1.0f / (1.0f + (targetArmor * 0.025f));
            }
            else if (string.Equals(damageType, "Pierce", StringComparison.OrdinalIgnoreCase))
            {
                soakFactor = 1.0f / (1.0f + (targetArmor * 0.015f));
            }
            else if (string.Equals(damageType, "Blunt", StringComparison.OrdinalIgnoreCase))
            {
                soakFactor = 1.0f / (1.0f + (targetArmor * 0.008f));
            }
            return Math.Max(1f, rawDamage * soakFactor);
        }
    }

    public static class ForgeArmorPenetration
    {
        public static void Initialize() { }

        public static float CalculateArmorSoak(float armorValue, float weaponThrustBonus)
        {
            return Math.Max(0f, armorValue - weaponThrustBonus);
        }
    }

    public static class ForgeWeaponBreakage
    {
        public static void Initialize() { }

        public static bool CheckWeaponBreak(float impactForce, float weaponDurability, float breakChanceMultiplier = 1.0f)
        {
            if (weaponDurability <= 0) return true;
            float chance = (impactForce / (weaponDurability * 10f)) * breakChanceMultiplier;
            return chance > 0.85f;
        }
    }

    public static class ForgeMoraleShock
    {
        public static void Initialize() { }

        public static float CalculateShock(int casualtiesInWindow, int totalTroops, bool isCommanderDead, bool isFlanked)
        {
            if (totalTroops <= 0) return 100f;
            float casualtyPct = (float)casualtiesInWindow / totalTroops;
            float shock = casualtyPct * 50f;
            if (isCommanderDead) shock += 25f;
            if (isFlanked) shock += 15f;
            return Math.Min(100f, shock);
        }
    }

    public static class ForgeFleeLogic
    {
        public static void Initialize() { }

        public static bool ShouldTroopFlee(float currentMorale, float healthPct, bool nearbyAllies)
        {
            if (currentMorale <= 15f) return true;
            if (currentMorale <= 30f && healthPct < 0.25f && !nearbyAllies) return true;
            return false;
        }
    }

    public static class ForgeCavalryCharge
    {
        public static void Initialize() { }

        public static float CalculateChargeImpact(float mountSpeed, float mountWeight, int targetInfantryDensity)
        {
            float kineticEnergy = 0.5f * mountWeight * (mountSpeed * mountSpeed);
            float resistance = Math.Max(1f, targetInfantryDensity * 1.5f);
            return kineticEnergy / (resistance * 100f);
        }
    }

    public static class ForgePikemanBrace
    {
        public static void Initialize() { }

        public static (bool mountStopped, float riderDamage) EvaluateBrace(float pikeLength, float mountChargeForce, bool isBraced)
        {
            if (!isBraced) return (false, 0f);
            if (pikeLength < 2.5f) return (false, 0f); // Requires reach >= 2.5m

            bool stopped = mountChargeForce < 5000f;
            float dmg = mountChargeForce * 0.05f;
            return (stopped, dmg);
        }
    }

    public static class ForgeArcherVolley
    {
        public static void Initialize() { }

        public static float CalculateSpreadRadius(float distance, int bowSkill, float windFactor)
        {
            float skillFactor = Math.Max(0.2f, 1.0f - (bowSkill / 300f));
            float distFactor = distance / 50f;
            return distFactor * skillFactor * (1.0f + windFactor * 0.5f);
        }
    }

    public static class ForgeFriendlyFireAvoidance
    {
        public static void Initialize() { }

        public static bool IsLineOfSightClear(float shooterDistToTarget, float shooterDistToAlly, float angleToAllyDeg)
        {
            if (shooterDistToAlly >= shooterDistToTarget) return true;
            return angleToAllyDeg > 15f; // clear if ally is not directly in 15 degree cone
        }
    }

    public static class ForgeWeatherCombatMod
    {
        public static void Initialize() { }

        public static float CalculateRangedSpeedPenalty(float rainIntensity, float windSpeed)
        {
            return (rainIntensity * 0.15f) + (windSpeed / 100f * 0.2f);
        }
    }

    public static class ForgeNightVisionPenalties
    {
        public static void Initialize() { }

        public static float CalculateAccuracyPenalty(bool isNight, bool nearTorch)
        {
            if (!isNight) return 0f;
            return nearTorch ? 0.1f : 0.35f;
        }
    }

    public static class ForgeBleedEffect
    {
        public static void Initialize() { }

        public static float CalculateBleedTick(float cutDamageDealt, int secondsPassed)
        {
            float initialRate = cutDamageDealt * 0.05f;
            float decay = Math.Max(0f, 1.0f - (secondsPassed * 0.1f));
            return initialRate * decay;
        }
    }

    public static class ForgePoisonEffect
    {
        public static void Initialize() { }

        public static float CalculatePoisonTick(int poisonTier, float targetConstitution)
        {
            float baseDmg = poisonTier * 2.5f;
            float res = Math.Max(0.5f, targetConstitution / 100f);
            return baseDmg / res;
        }
    }

    public static class ForgeStunEffect
    {
        public static void Initialize() { }

        public static float CalculateStunDuration(float bluntImpact, float targetMaxHealth)
        {
            float ratio = bluntImpact / Math.Max(1f, targetMaxHealth);
            if (ratio < 0.2f) return 0f;
            return Math.Min(2.5f, ratio * 2.0f);
        }
    }

    public static class ForgeCleaveStrike
    {
        public static void Initialize() { }

        public static float CalculateSecondaryTargetDamage(float primaryDamage, float weaponLength, int targetCount)
        {
            if (weaponLength < 1.1f || targetCount <= 1) return 0f;
            float falloff = 0.5f / targetCount;
            return primaryDamage * falloff;
        }
    }

    public static class ForgeHeadshotBonus
    {
        public static void Initialize() { }

        public static float ApplyHeadshot(float baseDamage)
        {
            return baseDamage * 1.75f;
        }
    }

    public static class ForgeDismountChance
    {
        public static void Initialize() { }

        public static bool CheckDismount(float damageTaken, float ridingSkill, bool isHookWeapon)
        {
            float threshold = 50f + (ridingSkill * 0.3f);
            if (isHookWeapon) threshold *= 0.5f;
            return damageTaken > threshold;
        }
    }

    public static class ForgeShieldBash
    {
        public static void Initialize() { }

        public static float CalculateBashKnockback(float shieldWeight, float agentStrength)
        {
            return (shieldWeight * 0.4f) + (agentStrength * 0.1f);
        }
    }

    public static class ForgeExecutionMoves
    {
        public static void Initialize() { }

        public static bool CanExecute(float targetHpPct, float swingForce)
        {
            return targetHpPct < 0.15f && swingForce > 80f;
        }
    }

    public static class ForgeAmbushTactics
    {
        public static void Initialize() { }

        public static float CalculateSurpriseMoraleDrop(float distanceToEnemy, bool concealedInForest)
        {
            if (!concealedInForest) return 5f;
            return distanceToEnemy < 30f ? 25f : 15f;
        }
    }

    public static class ForgeLootingBehavior
    {
        public static void Initialize() { }

        public static int CalculateBattleLootGold(int enemyTroopCount, float averageTier, int roguerySkill)
        {
            float basePerTroop = 10f + (averageTier * 8f);
            float skillBonus = 1.0f + (roguerySkill * 0.003f);
            return (int)(enemyTroopCount * basePerTroop * skillBonus);
        }
    }

    public static class ForgeBodyguardAssignment
    {
        public static void Initialize() { }

        public static List<(float offsetX, float offsetY)> GetGuardOffsets(int guardCount)
        {
            var list = new List<(float, float)>();
            for (int i = 0; i < guardCount; i++)
            {
                double angle = (2 * Math.PI / guardCount) * i;
                float x = (float)(Math.Cos(angle) * 2.5);
                float y = (float)(Math.Sin(angle) * 2.5);
                list.Add((x, y));
            }
            return list;
        }
    }

    public static class ForgeDuellingSystem
    {
        public static void Initialize() { }

        public static bool IsWithinRing(float agentX, float agentY, float ringCenterX, float ringCenterY, float ringRadius)
        {
            float dx = agentX - ringCenterX;
            float dy = agentY - ringCenterY;
            return (dx * dx + dy * dy) <= (ringRadius * ringRadius);
        }
    }

    public static class ForgeTargetPriority
    {
        public static void Initialize() { }

        public static float ScoreTarget(float distance, float healthRemaining, bool isRangedThreat, bool isFlanking)
        {
            float score = 1000f / Math.Max(1f, distance);
            score += (100f - healthRemaining) * 0.5f; // Finish off injured
            if (isRangedThreat) score += 20f;
            if (isFlanking) score += 35f;
            return score;
        }
    }

    public static class ForgeWeaponSwapLogic
    {
        public static void Initialize() { }

        public static string ChooseBestWeapon(float targetDistance, bool targetMounted, bool hasSpear, bool hasSword, bool hasBow, int arrowsRemaining)
        {
            if (targetDistance > 15f && hasBow && arrowsRemaining > 0) return "Bow";
            if (targetMounted && hasSpear && targetDistance < 10f) return "Spear";
            if (hasSword) return "Sword";
            return "Fists";
        }
    }

    public static class ForgeFormationSpacing
    {
        public static void Initialize() { }

        public static float GetSpacingForTactic(string tactic)
        {
            if (string.Equals(tactic, "ShieldWall", StringComparison.OrdinalIgnoreCase)) return 0.7f;
            if (string.Equals(tactic, "Circle", StringComparison.OrdinalIgnoreCase)) return 0.8f;
            if (string.Equals(tactic, "Square", StringComparison.OrdinalIgnoreCase)) return 0.75f;
            if (string.Equals(tactic, "Loose", StringComparison.OrdinalIgnoreCase)) return 2.0f;
            if (string.Equals(tactic, "Scatter", StringComparison.OrdinalIgnoreCase)) return 3.0f;
            return 1.2f; // Line
        }
    }
}
