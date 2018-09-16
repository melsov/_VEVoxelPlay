using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VoxelPlay;
using UnityEngine;

namespace VE.Generators
{
    [CreateAssetMenu(menuName = "Voxel Play/Terrain Generators/Test Terrain Generator", fileName = "TestTerrainGenerator", order = 101)]
    public class VETestTerrainGenerator : VoxelPlayTerrainGenerator
    {

        public VoxelDefinition topVoxel;
        public VoxelDefinition dirtVoxel;

        public Color32 voxelColor1 = new Color32(0, 128, 128, 255);
        public Color32 voxelColor2 = new Color32(122, 122, 12, 255);

        public float noiseScale = 42;
        public float isSolidThreshhold = .9f;

        public float altitude = 50f;

        public float hillScale = .2f;
        public float baseHeight = .15f;

        public override void Init()
        {

        }


        float HeightXZScaled01(float x, float z)
        {
            return SimplexNoise.Noise.Generate(x / noiseScale, z / noiseScale) * hillScale + baseHeight;
        }


        float GetAltitude(float x, float z)
        {
            return HeightXZScaled01(x, z);
        }

        public override void GetHeightAndMoisture(float x, float z, out float altitude, out float moisture)
        {
            if (!env.applicationIsPlaying)
                Init();

            altitude = GetAltitude(x, z);
            moisture = 0f;

        }


        /// <summary>
        /// Paints the terrain inside the chunk defined by its central "position"
        /// </summary>
        /// <returns><c>true</c>, if terrain was painted, <c>false</c> otherwise.</returns>
        /// <param name="position">Central position of the chunk.</param>
        public override bool PaintChunk(VoxelChunk chunk)
        {
            return PaintChunkA(chunk);
        }



        bool PaintChunkA(VoxelChunk chunk)
        {
            if (chunk.position.y + 8 < minHeight)
            {
                chunk.isAboveSurface = false;
                return false;
            }

            int chunkBottomPos = FastMath.FloorToInt(chunk.position.y - 8);
            int chunkTopPos = FastMath.FloorToInt(chunk.position.y + 8);

            bool isAboveSurface = false;
            bool atleastOneVoxel = false;

            Voxel[] voxels = chunk.voxels;
            VoxelDefinition voxDef = topVoxel;

            Vector3 pos;
            Vector3 position = chunk.position - new Vector3(8, 8, 8);
            int z, x, y;
            for (z = 0; z < 16; z++)
            {
                pos.z = position.z + z;
                for (x = 0; x < 16; x++)
                {
                    pos.x = position.x + x;
                    var heightMapInfo = env.GetHeightMapInfoFast(pos.x, pos.z);

                    int surfaceLevel = heightMapInfo.groundLevel;
                    int groundLevel = heightMapInfo.groundLevel;
                    int waterLevel = env.waterLevel > 0 ? env.waterLevel : -1;
                    BiomeDefinition biome = heightMapInfo.biome;

                    if(biome == null)
                    {
                        continue;
                    }

                    if (chunkTopPos > surfaceLevel)
                    {
                        isAboveSurface = true;
                    }

                    int localY = (int)(surfaceLevel - chunkBottomPos);
                    if (localY > 15)
                        localY = 15;

                    int index = z * ONE_Z_ROW + x;
                    for (y = 0; y <= localY; y++)
                    {

                        pos.y = position.y + y;

                        if ((int)pos.y == groundLevel)
                        {
                            voxDef = biome.voxelTop;
                        }
                        else
                        {
                            voxDef = biome.voxelDirt;
                        }

                        voxels[index].SetFast(voxDef, 15, 1, Misc.color32White);
                        atleastOneVoxel = true;

                        //Check if we should add trees or vegetation
                        if ((int)pos.y == groundLevel)
                        {
                            if (pos.y > waterLevel)
                            {
                                float rn = WorldRand.GetValue(pos);
                                if (env.enableTrees &&
                                    biome.treeDensity > 0 &&
                                    rn < biome.treeDensity &&
                                    biome.trees.Length > 0)
                                {
                                    // request a tree
                                    env.RequestTreeCreation(
                                        chunk,
                                        pos,
                                        env.GetTree(biome.trees, rn / biome.treeDensity));
                                }
                                else if(env.enableVegetation &&
                                    biome.vegetationDensity > 0 &&
                                    rn < biome.vegetationDensity &&
                                    biome.vegetation.Length > 0)
                                {
                                    if (index >= 15 * ONE_Y_ROW)
                                    {
                                        // we're at the top layer for this chunk
                                        // place a request for vegetation for the chunk above us
                                        env.RequestVegetationCreation(chunk.top,
                                            index - ONE_Y_ROW * 15,
                                            env.GetVegetation(biome, rn / biome.vegetationDensity));
                                    }
                                    else
                                    {
                                        // set a vegetation voxel
                                        voxels[index + ONE_Y_ROW].Set(env.GetVegetation(biome, rn / biome.vegetationDensity), Misc.color32White);
                                    }
                                }
                            }
                        }

                        index += ONE_Y_ROW;

                    }
                }
            }

            chunk.isAboveSurface = isAboveSurface;
            return atleastOneVoxel;
        }
    }
}
