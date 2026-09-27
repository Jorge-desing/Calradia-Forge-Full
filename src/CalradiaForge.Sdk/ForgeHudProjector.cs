using System;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// 3D-to-2D screen coordinate projection for MissionView HUD overlays and map track decay
    /// based on Bannerlord mission and world map architecture.
    /// </summary>
    public static class ForgeHudProjector
    {
        public static (bool isVisibleOnScreen, float screenX, float screenY, float depth) ProjectWorldToScreen(
            float worldX, float worldY, float worldZ,
            float camX, float camY, float camZ,
            float camPitchRad, float camYawRad,
            float fovDeg, int screenWidth, int screenHeight)
        {
            // Vector from camera to target
            float dx = worldX - camX;
            float dy = worldY - camY;
            float dz = worldZ - camZ;

            // Rotate into camera space by Yaw (around Z/up) then Pitch (around horizontal)
            float cosYaw = (float)Math.Cos(-camYawRad);
            float sinYaw = (float)Math.Sin(-camYawRad);
            float x1 = dx * cosYaw - dy * sinYaw;
            float y1 = dx * sinYaw + dy * cosYaw;
            float z1 = dz;

            float cosPitch = (float)Math.Cos(-camPitchRad);
            float sinPitch = (float)Math.Sin(-camPitchRad);
            float localX = x1;
            float localY = y1 * cosPitch - z1 * sinPitch; // Forward depth
            float localZ = y1 * sinPitch + z1 * cosPitch; // Up

            if (localY <= 0.1f)
            {
                // Behind the camera plane
                return (false, 0f, 0f, localY);
            }

            screenWidth = Math.Max(1, screenWidth);
            screenHeight = Math.Max(1, screenHeight);
            fovDeg = Math.Max(10f, Math.Min(170f, fovDeg));

            // Perspective divide
            float fovRad = fovDeg * (float)Math.PI / 180f;
            float tanHalfFov = (float)Math.Tan(fovRad * 0.5f);
            if (tanHalfFov <= 0.0001f) tanHalfFov = 0.0001f;
            float aspectRatio = (float)screenWidth / screenHeight;

            float normX = localX / (localY * tanHalfFov * aspectRatio);
            float normY = localZ / (localY * tanHalfFov);

            float screenX = (normX + 1.0f) * 0.5f * screenWidth;
            float screenY = (1.0f - normY) * 0.5f * screenHeight; // Top-left is (0,0)

            bool isVisible = screenX >= 0 && screenX <= screenWidth && screenY >= 0 && screenY <= screenHeight;
            return (isVisible, screenX, screenY, localY);
        }

        public static float CalculateTrackDecay(float terrainDecayMultiplier, int partySize, float daysPassed)
        {
            partySize = Math.Max(0, partySize);
            daysPassed = Math.Max(0f, daysPassed);
            terrainDecayMultiplier = Math.Max(0f, terrainDecayMultiplier);

            // Bannerlord map tracks:
            // Larger parties leave deeper tracks that take longer to decay.
            // Wet/snow terrain preserves tracks longer (lower decay multiplier).
            float initialTrackStrength = Math.Min(100f, partySize * 1.5f);
            float dailyLoss = 15f * terrainDecayMultiplier;

            float remainingStrength = initialTrackStrength - (dailyLoss * daysPassed);
            return Math.Max(0f, remainingStrength);
        }
    }
}
