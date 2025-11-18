using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Content
{
    /// <summary>
    /// Applies biome themes to generated maps including tilesets, lighting,
    /// environmental effects, and atmospheric variations.
    /// </summary>
    public class BiomeApplicator
    {
        #region Private Fields

        private readonly IAssetLoader _assetLoader;
        private readonly System.Random _random;
        private int _seed;
        
        // Applied biome tracking
        private readonly Dictionary<int, BiomeConfiguration> _roomBiomes;
        private readonly List<EnvironmentalEffectInstance> _activeEffects;
        private readonly Dictionary<string, Material> _originalMaterials;
        
        // Performance tracking
        private int _tilesProcessed;
        private int _objectsProcessed;
        private long _applicationTimeMs;

        #endregion

        #region Events

        public event Action<BiomeConfiguration> OnBiomeApplied;
        public event Action<string, Exception> OnBiomeApplicationFailed;
        public event Action<EnvironmentalEffect> OnEnvironmentalEffectStarted;

        #endregion

        #region Constructor

        public BiomeApplicator(IAssetLoader assetLoader, int seed = 0)
        {
            _assetLoader = assetLoader ?? throw new ArgumentNullException(nameof(assetLoader));
            _seed = seed;
            _random = new System.Random(_seed);
            
            _roomBiomes = new Dictionary<int, BiomeConfiguration>();
            _activeEffects = new List<EnvironmentalEffectInstance>();
            _originalMaterials = new Dictionary<string, Material>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Applies biome themes to an entire map
        /// </summary>
        public BiomeApplicationResult ApplyBiomeToMap(MapData map, BiomeConfiguration primaryBiome, 
            Dictionary<RoomClassification, BiomeConfiguration> roomTypeBiomes = null)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (primaryBiome == null) throw new ArgumentNullException(nameof(primaryBiome));

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new BiomeApplicationResult();

            try
            {
                // Validate biome configuration
                var validation = primaryBiome.Validate();
                if (!validation.IsValid)
                {
                    throw new InvalidOperationException($"Primary biome validation failed: {string.Join(", ", validation.Errors)}");
                }

                // Apply primary biome to map-wide elements
                ApplyMapWideBiome(map, primaryBiome, result);

                // Apply room-specific biomes if provided
                if (roomTypeBiomes != null)
                {
                    ApplyRoomSpecificBiomes(map, roomTypeBiomes, result);
                }
                else
                {
                    // Apply primary biome to all rooms
                    ApplyBiomeToAllRooms(map, primaryBiome, result);
                }

                // Apply environmental effects
                ApplyEnvironmentalEffects(map, primaryBiome, result);

                // Apply audio configuration
                ApplyAudioConfiguration(map, primaryBiome, result);

                stopwatch.Stop();
                _applicationTimeMs = stopwatch.ElapsedMilliseconds;

                result.Success = true;
                result.ApplicationTimeMs = _applicationTimeMs;
                result.TilesProcessed = _tilesProcessed;
                result.ObjectsProcessed = _objectsProcessed;

                OnBiomeApplied?.Invoke(primaryBiome);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.ApplicationTimeMs = stopwatch.ElapsedMilliseconds;

                OnBiomeApplicationFailed?.Invoke(primaryBiome.BiomeID, ex);
                throw;
            }
        }

        /// <summary>
        /// Applies biome transition effects between different biome regions
        /// </summary>
        public void ApplyBiomeTransitions(MapData map, Dictionary<RectInt, BiomeConfiguration> biomeRegions)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (biomeRegions == null || biomeRegions.Count == 0) return;

            // Find transition zones between biome regions
            var transitionZones = FindTransitionZones(biomeRegions);

            // Apply blending effects in transition zones
            foreach (var zone in transitionZones)
            {
                ApplyTransitionBlending(map, zone);
            }
        }

        /// <summary>
        /// Updates the random seed for reproducible biome application
        /// </summary>
        public void SetSeed(int seed)
        {
            _seed = seed;
            _random = new System.Random(_seed);
        }

        /// <summary>
        /// Gets performance metrics for the last biome application
        /// </summary>
        public BiomeApplicationMetrics GetMetrics()
        {
            return new BiomeApplicationMetrics
            {
                TilesProcessed = _tilesProcessed,
                ObjectsProcessed = _objectsProcessed,
                ApplicationTimeMs = _applicationTimeMs,
                ActiveEffectsCount = _activeEffects.Count,
                AppliedBiomesCount = _roomBiomes.Count
            };
        }

        /// <summary>
        /// Clears all applied biome effects and restores original state
        /// </summary>
        public void ClearBiomeEffects()
        {
            // Stop environmental effects
            foreach (var effect in _activeEffects)
            {
                effect.Stop();
            }
            _activeEffects.Clear();

            // Restore original materials
            RestoreOriginalMaterials();

            // Clear biome tracking
            _roomBiomes.Clear();
            _originalMaterials.Clear();

            // Reset metrics
            _tilesProcessed = 0;
            _objectsProcessed = 0;
            _applicationTimeMs = 0;
        }

        #endregion

        #region Private Methods

        private void ApplyMapWideBiome(MapData map, BiomeConfiguration biome, BiomeApplicationResult result)
        {
            // Apply global lighting settings
            ApplyGlobalLighting(biome, result);

            // Apply global fog settings
            ApplyGlobalFog(biome, result);

            // Apply global post-processing
            ApplyGlobalPostProcessing(biome, result);
        }

        private void ApplyRoomSpecificBiomes(MapData map, Dictionary<RoomClassification, BiomeConfiguration> roomTypeBiomes, 
            BiomeApplicationResult result)
        {
            foreach (var room in map.Rooms)
            {
                if (roomTypeBiomes.TryGetValue(room.Classification, out var roomBiome))
                {
                    ApplyBiomeToRoom(room, roomBiome, result);
                    _roomBiomes[room.RoomID] = roomBiome;
                }
                else
                {
                    // Room type not specified, skip biome application for this room
                    result.SkippedRooms.Add(room.RoomID);
                }
            }
        }

        private void ApplyBiomeToAllRooms(MapData map, BiomeConfiguration biome, BiomeApplicationResult result)
        {
            foreach (var room in map.Rooms)
            {
                ApplyBiomeToRoom(room, biome, result);
                _roomBiomes[room.RoomID] = biome;
            }
        }

        private void ApplyBiomeToRoom(RoomData room, BiomeConfiguration biome, BiomeApplicationResult result)
        {
            try
            {
                // Apply tileset to room
                ApplyTilesetToRoom(room, biome, result);

                // Apply color variations
                ApplyColorVariations(room, biome, result);

                // Apply room-specific lighting
                ApplyRoomLighting(room, biome, result);

                result.ProcessedRooms.Add(room.RoomID);
            }
            catch (Exception ex)
            {
                result.FailedRooms.Add(room.RoomID);
                result.Errors.Add($"Room {room.RoomID}: {ex.Message}");
            }
        }

        private void ApplyTilesetToRoom(RoomData room, BiomeConfiguration biome, BiomeApplicationResult result)
        {
            // Get tilemap for this room (assuming room-based tilemap organization)
            var tilemap = GetRoomTilemap(room);
            if (tilemap == null) return;

            // Get biome-specific tileset
            var tileset = biome.GetRandomTileset(_random);
            if (tileset == null) return;

            // Apply tiles to room bounds
            for (int x = room.Bounds.xMin; x <= room.Bounds.xMax; x++)
            {
                for (int y = room.Bounds.yMin; y <= room.Bounds.yMax; y++)
                {
                    var position = new Vector3Int(x, y, 0);
                    var currentTile = tilemap.GetTile(position);
                    
                    if (currentTile != null)
                    {
                        // Replace with biome-specific tile
                        var newTile = GetBiomeTile(currentTile, tileset, biome);
                        if (newTile != null)
                        {
                            tilemap.SetTile(position, newTile);
                            _tilesProcessed++;
                        }
                    }
                }
            }
        }

        private void ApplyColorVariations(RoomData room, BiomeConfiguration biome, BiomeApplicationResult result)
        {
            if (!biome.ApplyColorTinting) return;

            var tilemap = GetRoomTilemap(room);
            if (tilemap == null) return;

            // Apply color tinting to tilemap
            var color = biome.ColorPalette.GetRandomColor(_random);
            tilemap.color = biome.GetRandomColorVariation(color, _random);

            _tilesProcessed += room.Bounds.width * room.Bounds.height;
        }

        private void ApplyGlobalLighting(BiomeConfiguration biome, BiomeApplicationResult result)
        {
            // Configure global lighting settings
            var light = RenderSettings.ambientLight;
            light = biome.AmbientLightColor;
            RenderSettings.ambientLight = light;
            RenderSettings.ambientIntensity = biome.AmbientLightIntensity;

            result.AppliedEffects.Add("Global Lighting");
        }

        private void ApplyGlobalFog(BiomeConfiguration biome, BiomeApplicationResult result)
        {
            // Check if biome has fog effect
            var fogEffect = biome.EnvironmentalEffects.FirstOrDefault(e => e.EffectType == EffectType.Fog);
            if (fogEffect != null)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = biome.ColorPalette.SecondaryColor;
                RenderSettings.fogDensity = fogEffect.Intensity * 0.1f;

                result.AppliedEffects.Add("Global Fog");
            }
        }

        private void ApplyGlobalPostProcessing(BiomeConfiguration biome, BiomeApplicationResult result)
        {
            // Apply post-processing effects if available
            // This would integrate with Unity's Post Processing Stack
            // For now, we'll track that it should be applied
            result.AppliedEffects.Add("Post Processing");
        }

        private void ApplyRoomLighting(RoomData room, BiomeConfiguration biome, BiomeApplicationResult result)
        {
            // Create room-specific light if biome requires it
            if (biome.AmbientLightIntensity != 1.0f || biome.AmbientLightColor != Color.white)
            {
                var roomCenter = room.bounds.center;
                
                // Create or update room light object
                var lightObject = GameObject.Find($"RoomLight_{room.RoomID}");
                if (lightObject == null)
                {
                    lightObject = new GameObject($"RoomLight_{room.RoomID}");
                }

                var light = lightObject.GetComponent<Light>() ?? lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = biome.AmbientLightColor;
                light.intensity = biome.AmbientLightIntensity;
                light.range = Mathf.Max(room.Bounds.width, room.Bounds.height);
                light.transform.position = new Vector3(roomCenter.x, roomCenter.y, -10);

                result.AppliedEffects.Add($"Room Lighting {room.RoomID}");
                _objectsProcessed++;
            }
        }

        private void ApplyEnvironmentalEffects(MapData map, BiomeConfiguration biome, BiomeApplicationResult result)
        {
            foreach (var effectConfig in biome.EnvironmentalEffects)
            {
                try
                {
                    var effect = CreateEnvironmentalEffect(effectConfig, biome);
                    if (effect != null)
                    {
                        effect.Start();
                        _activeEffects.Add(effect);
                        result.AppliedEffects.Add($"Environmental: {effectConfig.EffectName}");
                        OnEnvironmentalEffectStarted?.Invoke(effectConfig);
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Environmental effect '{effectConfig.EffectName}' failed: {ex.Message}");
                }
            }
        }

        private void ApplyAudioConfiguration(MapData map, BiomeConfiguration biome, BiomeApplicationResult result)
        {
            if (biome.AmbientMusic != null)
            {
                // Play ambient music
                var audioSource = GameObject.Find("BiomeAudioSource")?.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    var audioObject = new GameObject("BiomeAudioSource");
                    audioSource = audioObject.AddComponent<AudioSource>();
                    audioSource.loop = true;
                    audioSource.playOnAwake = false;
                }

                audioSource.clip = biome.AmbientMusic;
                audioSource.volume = biome.MusicVolume;
                audioSource.Play();

                result.AppliedEffects.Add("Ambient Music");
            }

            // Play ambient sounds
            foreach (var sound in biome.AmbientSounds)
            {
                if (sound != null)
                {
                    // Create sound effect object
                    var soundObject = new GameObject($"AmbientSound_{sound.name}");
                    var audioSource = soundObject.AddComponent<AudioSource>();
                    audioSource.clip = sound;
                    audioSource.volume = biome.SoundVolume;
                    audioSource.loop = true;
                    audioSource.spatialBlend = 0.5f; // Semi-3D sound
                    audioSource.Play();

                    result.AppliedEffects.Add($"Ambient Sound: {sound.name}");
                    _objectsProcessed++;
                }
            }
        }

        private EnvironmentalEffectInstance CreateEnvironmentalEffect(EnvironmentalEffect config, BiomeConfiguration biome)
        {
            switch (config.EffectType)
            {
                case EffectType.Rain:
                    return new RainEffect(config, biome);
                case EffectType.Snow:
                    return new SnowEffect(config, biome);
                case EffectType.Fog:
                    return new FogEffect(config, biome);
                case EffectType.Dust:
                    return new DustEffect(config, biome);
                case EffectType.Steam:
                    return new SteamEffect(config, biome);
                default:
                    return new GenericParticleEffect(config, biome);
            }
        }

        private List<TransitionZone> FindTransitionZones(Dictionary<RectInt, BiomeConfiguration> biomeRegions)
        {
            var zones = new List<TransitionZone>();
            var regions = biomeRegions.Keys.ToList();

            for (int i = 0; i < regions.Count; i++)
            {
                for (int j = i + 1; j < regions.Count; j++)
                {
                    var zone = FindTransitionZone(regions[i], regions[j]);
                    if (zone != null)
                    {
                        zones.Add(zone);
                    }
                }
            }

            return zones;
        }

        private TransitionZone FindTransitionZone(RectInt region1, RectInt region2)
        {
            // Find overlapping or adjacent areas between regions
            var overlap = RectInt.Min(region1, region2);
            
            if (overlap.width > 0 && overlap.height > 0)
            {
                return new TransitionZone
                {
                    Bounds = overlap,
                    FromRegion = region1,
                    ToRegion = region2,
                    BlendWidth = 2 // 2-tile transition width
                };
            }

            return null;
        }

        private void ApplyTransitionBlending(MapData map, TransitionZone zone)
        {
            // Apply gradual blending between biomes in transition zones
            for (int x = zone.Bounds.xMin; x <= zone.Bounds.xMax; x++)
            {
                for (int y = zone.Bounds.yMin; y <= zone.Bounds.yMax; y++)
                {
                    var position = new Vector3Int(x, y, 0);
                    var blendFactor = CalculateBlendFactor(position, zone);
                    
                    // Apply blended tile or color
                    ApplyBlendedTile(position, blendFactor, zone);
                }
            }
        }

        private float CalculateBlendFactor(Vector3Int position, TransitionZone zone)
        {
            // Calculate distance-based blend factor
            var center = zone.Bounds.center;
            var distance = Vector3Int.Distance(position, center);
            var maxDistance = Mathf.Max(zone.Bounds.width, zone.Bounds.height) / 2f;
            
            return Mathf.Clamp01(distance / maxDistance);
        }

        private void ApplyBlendedTile(Vector3Int position, float blendFactor, TransitionZone zone)
        {
            // Implementation would blend tiles or colors based on blendFactor
            // This is a placeholder for the blending logic
            _tilesProcessed++;
        }

        private Tilemap GetRoomTilemap(RoomData room)
        {
            // Find tilemap for this room (implementation depends on tilemap organization)
            // This is a simplified approach - actual implementation would depend on how tilemaps are structured
            var tilemaps = GameObject.FindObjectsOfType<Tilemap>();
            return tilemaps.FirstOrDefault(tm => tm.name.Contains($"Room_{room.RoomID}")) ?? tilemaps.FirstOrDefault();
        }

        private TileBase GetBiomeTile(TileBase currentTile, TilesetConfiguration tileset, BiomeConfiguration biome)
        {
            // Map current tile to biome-specific equivalent
            // This would use the tileset configuration to find appropriate replacement
            // For now, return the current tile (no replacement)
            return currentTile;
        }

        private void RestoreOriginalMaterials()
        {
            // Restore original materials to objects
            foreach (var kvp in _originalMaterials)
            {
                var obj = GameObject.Find(kvp.Key);
                if (obj != null)
                {
                    var renderer = obj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material = kvp.Value;
                    }
                }
            }
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Result of biome application operation
    /// </summary>
    [Serializable]
    public class BiomeApplicationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public long ApplicationTimeMs { get; set; }
        public int TilesProcessed { get; set; }
        public int ObjectsProcessed { get; set; }
        public List<int> ProcessedRooms { get; set; } = new List<int>();
        public List<int> SkippedRooms { get; set; } = new List<int>();
        public List<int> FailedRooms { get; set; } = new List<int>();
        public List<string> AppliedEffects { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Performance metrics for biome application
    /// </summary>
    [Serializable]
    public class BiomeApplicationMetrics
    {
        public int TilesProcessed { get; set; }
        public int ObjectsProcessed { get; set; }
        public long ApplicationTimeMs { get; set; }
        public int ActiveEffectsCount { get; set; }
        public int AppliedBiomesCount { get; set; }
        public float AverageTilesPerMs => ApplicationTimeMs > 0 ? (float)TilesProcessed / ApplicationTimeMs : 0f;
    }

    /// <summary>
    /// Represents a transition zone between two biome regions
    /// </summary>
    internal class TransitionZone
    {
        public RectInt Bounds { get; set; }
        public RectInt FromRegion { get; set; }
        public RectInt ToRegion { get; set; }
        public int BlendWidth { get; set; }
    }

    /// <summary>
    /// Base class for environmental effect instances
    /// </summary>
    internal abstract class EnvironmentalEffectInstance
    {
        protected readonly EnvironmentalEffect _config;
        protected readonly BiomeConfiguration _biome;

        protected EnvironmentalEffectInstance(EnvironmentalEffect config, BiomeConfiguration biome)
        {
            _config = config;
            _biome = biome;
        }

        public abstract void Start();
        public abstract void Stop();
        public abstract bool IsPlaying { get; }
    }

    // Specific environmental effect implementations
    internal class RainEffect : EnvironmentalEffectInstance
    {
        public RainEffect(EnvironmentalEffect config, BiomeConfiguration biome) : base(config, biome) { }
        public override void Start() { /* Implement rain effect */ }
        public override void Stop() { /* Stop rain effect */ }
        public override bool IsPlaying => false; /* Track playing state */
    }

    internal class SnowEffect : EnvironmentalEffectInstance
    {
        public SnowEffect(EnvironmentalEffect config, BiomeConfiguration biome) : base(config, biome) { }
        public override void Start() { /* Implement snow effect */ }
        public override void Stop() { /* Stop snow effect */ }
        public override bool IsPlaying => false;
    }

    internal class FogEffect : EnvironmentalEffectInstance
    {
        public FogEffect(EnvironmentalEffect config, BiomeConfiguration biome) : base(config, biome) { }
        public override void Start() { /* Implement fog effect */ }
        public override void Stop() { /* Stop fog effect */ }
        public override bool IsPlaying => false;
    }

    internal class DustEffect : EnvironmentalEffectInstance
    {
        public DustEffect(EnvironmentalEffect config, BiomeConfiguration biome) : base(config, biome) { }
        public override void Start() { /* Implement dust effect */ }
        public override void Stop() { /* Stop dust effect */ }
        public override bool IsPlaying => false;
    }

    internal class SteamEffect : EnvironmentalEffectInstance
    {
        public SteamEffect(EnvironmentalEffect config, BiomeConfiguration biome) : base(config, biome) { }
        public override void Start() { /* Implement steam effect */ }
        public override void Stop() { /* Stop steam effect */ }
        public override bool IsPlaying => false;
    }

    internal class GenericParticleEffect : EnvironmentalEffectInstance
    {
        public GenericParticleEffect(EnvironmentalEffect config, BiomeConfiguration biome) : base(config, biome) { }
        public override void Start() { /* Implement generic particle effect */ }
        public override void Stop() { /* Stop particle effect */ }
        public override bool IsPlaying => false;
    }

    #endregion
}