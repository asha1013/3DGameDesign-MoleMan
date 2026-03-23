// 
// Copyright (c) 2025 Off The Beaten Track UG
// All rights reserved.
// 
// Maintainer: Jens Bahr
// 

using UnityEngine;

namespace Sparrow.VolumetricLight
{
    /// <summary>
    /// Advanced animation system for VolumetricLights with pulsing, color cycling, and flickering effects
    /// </summary>
    [RequireComponent(typeof(VolumetricLight))]
    public class VolumetricLightAnimation : MonoBehaviour
    {
        [Header("Animation Settings")]
        [Tooltip("Enable or disable all animations")]
        public bool enableAnimations = true;
        
        [Header("Pulsing Effects")]
        [Tooltip("Enable intensity pulsing")]
        public bool enablePulsing = false;
        [Tooltip("Pulsing pattern type")]
        public PulsingPattern pulsingPattern = PulsingPattern.Sine;
        [Tooltip("Speed of the pulsing effect")]
        [Range(0.1f, 10f)] public float pulsingSpeed = 1f;
        [Tooltip("Intensity of the pulsing (0 = no pulsing, 1 = full pulsing)")]
        [Range(0f, 1f)] public float pulsingIntensity = 0.5f;
        [Tooltip("Base intensity multiplier for pulsing")]
        [Range(0.1f, 2f)] public float baseIntensity = 1f;
        
        [Header("Color Cycling")]
        [Tooltip("Enable color cycling")]
        public bool enableColorCycling = false;
        [Tooltip("Color palette for cycling")]
        public ColorPalette colorPalette = ColorPalette.Fire;
        [Tooltip("Speed of color transitions")]
        [Range(0.1f, 5f)] public float colorSpeed = 0.5f;
        [Tooltip("Custom color array for cycling (used when palette is set to Custom)")]
        public Color[] customColors = { Color.red, Color.yellow, Color.blue };
        [Tooltip("Smoothness of color transitions")]
        [Range(0.1f, 2f)] public float colorSmoothness = 1.5f;
        
        [Header("Flickering Effects")]
        [Tooltip("Enable flickering effect")]
        public bool enableFlickering = false;
        [Tooltip("Intensity of flickering")]
        [Range(0f, 1f)] public float flickerIntensity = 0.6f;
        [Tooltip("Speed of flickering")]
        [Range(0.1f, 10f)] public float flickerSpeed = 3f;
        [Tooltip("Randomness in flickering")]
        [Range(0f, 1f)] public float flickerRandomness = 0.7f;
        [Tooltip("Minimum intensity during flickering")]
        [Range(0f, 1f)] public float minFlickerIntensity = 0.2f;
        
        [Header("Advanced Settings")]
        [Tooltip("Offset for animation timing (useful for multiple lights)")]
        [Range(0f, 10f)] public float timeOffset = 0f;
        [Tooltip("Randomize the animation start time")]
        public bool randomizeStartTime = true;
        
        // Private variables
        private VolumetricLight volumetricLight;
        private LightSettings originalSettings;
        private float animationTime;
        private float randomOffset;
        private Color originalColor;
        private float originalIntensity;
        private bool isInitialized = false;
        
        // Color palettes
        public enum ColorPalette
        {
            Fire,
            Ice,
            Neon,
            Sunset,
            Ocean,
            Forest,
            Custom
        }
        
        public enum PulsingPattern
        {
            Sine,
            Square,
            Random
        }
        
        // Predefined color palettes
        public static readonly Color[][] ColorPalettes = new Color[][]
        {
            // Fire
            new Color[] { new Color(1f, 0.2f, 0f), new Color(1f, 0.5f, 0f), new Color(1f, 0.8f, 0f), new Color(1f, 1f, 0.3f) },
            // Ice
            new Color[] { new Color(0.2f, 0.5f, 1f), new Color(0.5f, 0.7f, 1f), new Color(0.8f, 0.9f, 1f), new Color(0.9f, 0.95f, 1f) },
            // Neon
            new Color[] { new Color(1f, 0f, 1f), new Color(0f, 1f, 1f), new Color(1f, 1f, 0f), new Color(0f, 1f, 0f) },
            // Sunset
            new Color[] { new Color(1f, 0.3f, 0f), new Color(1f, 0.6f, 0.2f), new Color(1f, 0.8f, 0.4f), new Color(1f, 0.9f, 0.6f) },
            // Ocean
            new Color[] { new Color(0f, 0.3f, 0.6f), new Color(0.2f, 0.5f, 0.8f), new Color(0.4f, 0.7f, 1f), new Color(0.6f, 0.9f, 1f) },
            // Forest
            new Color[] { new Color(0.2f, 0.4f, 0.1f), new Color(0.4f, 0.6f, 0.2f), new Color(0.6f, 0.8f, 0.3f), new Color(0.8f, 1f, 0.4f) }
        };
        
        private void Awake()
        {
            volumetricLight = GetComponent<VolumetricLight>();
            if (volumetricLight == null)
            {
                Debug.LogError("VolumetricLightAnimation requires a VolumetricLight component!");
                enabled = false;
                return;
            }
            
            if (randomizeStartTime)
            {
                randomOffset = Random.Range(0f, 10f);
            }
        }
        
        private void Start()
        {
            InitializeAnimation();
        }
        
        private void InitializeAnimation()
        {
            if (isInitialized) return;
            
            // Store original settings
            originalSettings = new LightSettings(volumetricLight.settings);
            originalColor = volumetricLight.settings.overrideLightColor ? volumetricLight.settings.newColor : Color.white;
            originalIntensity = volumetricLight.settings.overrideLightColor ? volumetricLight.settings.newIntensity : 1f;
            
            // Ensure color override is enabled for animations
            if (!volumetricLight.settings.overrideLightColor)
            {
                volumetricLight.settings.overrideLightColor = true;
                volumetricLight.settings.newColor = originalColor;
                volumetricLight.settings.newIntensity = originalIntensity;
            }
            
            isInitialized = true;
        }
        
        private void Update()
        {
            if (!enableAnimations || !isInitialized) return;
            
            animationTime = Time.time + timeOffset + randomOffset;
            
            UpdatePulsing();
            UpdateColorCycling();
            UpdateFlickering();
            
            // Apply changes to the volumetric light
            volumetricLight.UpdateVolumetricLight();
        }
        
        private void UpdatePulsing()
        {
            if (!enablePulsing) return;
            
            float pulseValue = 0f;
            
            switch (pulsingPattern)
            {
                case PulsingPattern.Sine:
                    pulseValue = Mathf.Sin(animationTime * pulsingSpeed * Mathf.PI * 2f) * 0.5f + 0.5f;
                    break;
                case PulsingPattern.Square:
                    pulseValue = Mathf.Sin(animationTime * pulsingSpeed * Mathf.PI * 2f) > 0 ? 1f : 0f;
                    break;
                case PulsingPattern.Random:
                    pulseValue = Mathf.PerlinNoise(animationTime * pulsingSpeed, 0f);
                    break;
            }
            
            float intensityMultiplier = baseIntensity + (pulseValue * pulsingIntensity);
            volumetricLight.settings.newIntensity = originalIntensity * intensityMultiplier;
        }
        
        private void UpdateColorCycling()
        {
            if (!enableColorCycling) return;
            
            Color[] palette = GetCurrentPalette();
            if (palette == null || palette.Length == 0) return;
            
            // Handle single color palette
            if (palette.Length == 1)
            {
                volumetricLight.settings.newColor = palette[0];
                return;
            }
            
            float cycleTime = animationTime * colorSpeed;
            float normalizedTime = cycleTime % 1f;
            
            // Create a smooth cycle that goes through all colors and back to the first
            float scaledTime = normalizedTime * palette.Length;
            int colorIndex = Mathf.FloorToInt(scaledTime) % palette.Length;
            int nextColorIndex = (colorIndex + 1) % palette.Length;
            float lerpFactor = scaledTime - Mathf.Floor(scaledTime);
            
            Color targetColor = Color.Lerp(palette[colorIndex], palette[nextColorIndex], lerpFactor);
            
            // Apply smoothness - this controls how much the cycling affects the color
            if (colorSmoothness < 1f)
            {
                targetColor = Color.Lerp(originalColor, targetColor, colorSmoothness);
            }
            
            volumetricLight.settings.newColor = targetColor;
        }
        
        private void UpdateFlickering()
        {
            if (!enableFlickering) return;
            
            // Use multiple noise samples for more realistic flickering
            float noise1 = Mathf.PerlinNoise(animationTime * flickerSpeed, 0f);
            float noise2 = Mathf.PerlinNoise(animationTime * flickerSpeed * 1.3f, 100f);
            float noise3 = Mathf.PerlinNoise(animationTime * flickerSpeed * 0.7f, 200f);
            
            // Combine noises for more complex flickering
            float combinedNoise = (noise1 + noise2 + noise3) / 3f;
            
            // Add randomness for more variation
            float randomFactor = 1f + (Random.Range(-flickerRandomness, flickerRandomness) * 0.2f);
            combinedNoise *= randomFactor;
            
            // Create more pronounced flickering effect
            float flickerMultiplier = Mathf.Lerp(minFlickerIntensity, 1f, Mathf.Pow(combinedNoise, 0.5f) * flickerIntensity);
            
            // Apply flickering to intensity
            if (enablePulsing)
            {
                // Combine with pulsing if both are enabled
                float currentIntensity = volumetricLight.settings.newIntensity;
                volumetricLight.settings.newIntensity = currentIntensity * flickerMultiplier;
            }
            else
            {
                volumetricLight.settings.newIntensity = originalIntensity * flickerMultiplier;
            }
            
            // Also apply slight color variation for more realistic flickering
            if (enableColorCycling)
            {
                // Add subtle color temperature variation
                float colorVariation = (combinedNoise - 0.5f) * 0.1f * flickerIntensity;
                Color currentColor = volumetricLight.settings.newColor;
                Color flickerColor = new Color(
                    Mathf.Clamp01(currentColor.r + colorVariation),
                    Mathf.Clamp01(currentColor.g + colorVariation * 0.5f),
                    Mathf.Clamp01(currentColor.b - colorVariation * 0.3f),
                    currentColor.a
                );
                volumetricLight.settings.newColor = Color.Lerp(currentColor, flickerColor, flickerIntensity * 0.3f);
            }
        }
        
        private Color[] GetCurrentPalette()
        {
            if (colorPalette == ColorPalette.Custom)
            {
                return customColors.Length > 0 ? customColors : new Color[] { Color.white };
            }
            
            int paletteIndex = (int)colorPalette;
            if (paletteIndex >= 0 && paletteIndex < ColorPalettes.Length)
            {
                return ColorPalettes[paletteIndex];
            }
            
            return new Color[] { Color.white };
        }
        
        /// <summary>
        /// Reset the light to its original state
        /// </summary>
        public void ResetToOriginal()
        {
            if (originalSettings != null)
            {
                volumetricLight.settings.overrideLightColor = originalSettings.overrideLightColor;
                volumetricLight.settings.newColor = originalSettings.newColor;
                volumetricLight.settings.newIntensity = originalSettings.newIntensity;
                volumetricLight.settings.intensityMultiplier = originalSettings.intensityMultiplier;
                volumetricLight.UpdateVolumetricLight();
            }
        }
        
        /// <summary>
        /// Set a custom color palette
        /// </summary>
        public void SetCustomPalette(Color[] colors)
        {
            customColors = colors;
            colorPalette = ColorPalette.Custom;
        }
        
        /// <summary>
        /// Enable/disable specific animation types
        /// </summary>
        public void SetAnimationEnabled(AnimationType type, bool enabled)
        {
            switch (type)
            {
                case AnimationType.Pulsing:
                    enablePulsing = enabled;
                    break;
                case AnimationType.ColorCycling:
                    enableColorCycling = enabled;
                    break;
                case AnimationType.Flickering:
                    enableFlickering = enabled;
                    break;
            }
        }
        
        public enum AnimationType
        {
            Pulsing,
            ColorCycling,
            Flickering
        }
        
        private void OnValidate()
        {
            // Ensure custom colors array is not empty
            if (colorPalette == ColorPalette.Custom && (customColors == null || customColors.Length == 0))
            {
                customColors = new Color[] { Color.white };
            }
            
            // Clamp values to valid ranges
            pulsingIntensity = Mathf.Clamp01(pulsingIntensity);
            colorSmoothness = Mathf.Clamp(colorSmoothness, 0.1f, 2f);
            flickerIntensity = Mathf.Clamp01(flickerIntensity);
            flickerRandomness = Mathf.Clamp01(flickerRandomness);
            minFlickerIntensity = Mathf.Clamp01(minFlickerIntensity);
        }
    }
}
