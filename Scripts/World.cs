global using Godot;
global using System;
global using System.Collections.Generic;
global using System.Collections.Concurrent;
global using System.Diagnostics;
global using System.Runtime.CompilerServices;
global using System.Threading;
global using System.Text.RegularExpressions;
global using System.Threading.Tasks;
global using System.Linq;

using Noise = SimplexNoise.Noise;

namespace Project2D;

public enum BiomeType
{
	Desert,
	Savanna,
	TropicalRainforest,
	Grassland,
	Woodland,
	SeasonalForest,
	TemperateRainforest,
	BorealForest,
	Tundra,
	Ice
}

public partial class World : TileMap
{
	private int Size { get; set; } = 300;
	private int SeedTiles { get; set; } = 209323094;
	private int SeedBiomes { get; set; } = 309333032;
	private float FrequencyMoisture { get; set; } = 0.02f;
	private float FrequencyHeat { get; set; } = 0.01f;
	private Dictionary<Vector2, Tile> Tiles { get; set; } = new();
	private BiomeType[,] BiomeTable { get; set; } = new BiomeType[6, 6]
	{
		// COLDEST              COLDER           COLD                      HOT                           HOTTER                       HOTTEST
		{ BiomeType.Ice, BiomeType.Tundra, BiomeType.Grassland,    BiomeType.Desert,              BiomeType.Desert,              BiomeType.Desert            }, // DRYEST
		{ BiomeType.Ice, BiomeType.Tundra, BiomeType.Grassland,    BiomeType.Desert,              BiomeType.Desert,              BiomeType.Desert            }, // DRYER
		{ BiomeType.Ice, BiomeType.Tundra, BiomeType.Woodland,     BiomeType.Woodland,            BiomeType.Savanna,             BiomeType.Savanna           }, // DRY
		{ BiomeType.Ice, BiomeType.Tundra, BiomeType.BorealForest, BiomeType.Woodland,            BiomeType.Savanna,             BiomeType.Savanna           }, // WET
		{ BiomeType.Ice, BiomeType.Tundra, BiomeType.BorealForest, BiomeType.SeasonalForest,      BiomeType.TropicalRainforest,  BiomeType.TropicalRainforest}, // WETTER
		{ BiomeType.Ice, BiomeType.Tundra, BiomeType.BorealForest, BiomeType.TemperateRainforest, BiomeType.TropicalRainforest,  BiomeType.TropicalRainforest}  // WETTEST
	};

	public override void _Ready()
	{
		var moisture = CalcNoise(SeedTiles, FrequencyMoisture);
		var heat = CalcNoise(SeedBiomes, FrequencyHeat);
		var amplitude = CalcNoise(SeedTiles, FrequencyMoisture);

		var biomes = new Dictionary<BiomeType, Biome>
		{
			{ BiomeType.Tundra,              new BiomeTundra(this)              },
			{ BiomeType.Ice,                 new BiomeIce(this)                 },
			{ BiomeType.Grassland,           new BiomeGrassland(this)           },
			{ BiomeType.Woodland,            new BiomeWoodland(this)            },
			{ BiomeType.BorealForest,        new BiomeBorealForest(this)        },
			{ BiomeType.SeasonalForest,      new BiomeSeasonalForest(this)      },
			{ BiomeType.TemperateRainforest, new BiomeTemperateRainforest(this) },
			{ BiomeType.TropicalRainforest,  new BiomeTropicalRainForest(this)  },
			{ BiomeType.Desert,              new BiomeDesert(this)              },
			{ BiomeType.Savanna,             new BiomeSavanna(this)             }
		};

		for (int x = 0; x < Size; x++)
			for (int z = 0; z < Size; z++)
			{
				// Generate the tiles
				var biome = GetBiome(moisture[x, z], heat[x, z]);
				biomes[biome].Generate(x, z, amplitude[x, z]);

				// Store information about each tile
				var tile = new Tile();
				tile.Moisture = moisture[x, z];
				tile.Heat = heat[x, z];
				tile.BiomeType = biome;

				Tiles[new Vector2(x, z)] = tile;
			}
	}

	private BiomeType GetBiome(float moistureNoise, float heatNoise)
	{
		var moistureType = RemapNoise(moistureNoise, 0, BiomeTable.GetLength(0) - 1);
		var heatType = RemapNoise(heatNoise, 0, BiomeTable.GetLength(0) - 1);

		return BiomeTable[(int)moistureType, (int)heatType];
	}

	private float RemapNoise(float noise, float min, float max) =>
		noise.Remap(0, 241.19427f, min, max);

	// Seems to calculate values between 0 and ~241.19427 (frequency has a small impact on max value)
	private float[,] CalcNoise(int seed, float frequency)
	{
		Noise.Seed = seed;
		return Noise.Calc2D(Size, Size, frequency);
	}

	public void SetTile(int worldX, int worldZ, int tileX = 0, int tileY = 0) =>
		SetCell(0, new Vector2i(-Size / 2, -Size / 2) + new Vector2i(worldX, worldZ), 0, new Vector2i(tileX, tileY));
}
