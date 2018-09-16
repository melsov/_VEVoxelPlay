using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VoxelPlay;
using UnityEngine;

namespace VE.Generators
{
    [CreateAssetMenu(menuName = "Voxel Play/Terrain Generators/Vox Engines Terrain Generator", fileName = "VETerrainGenerator", order = 101)]
    public class VETerrainGenerator : VoxelPlayTerrainGenerator
    {

        public VoxelDefinition topVoxel;
        public VoxelDefinition dirtVoxel;

        public float noiseScale = 42;
        public float isSolidThreshhold = .9f;

        public float altitude = 50f;

        public float hillScale = .2f;
        public float baseHeight = .15f;

        public override void Init()
        {
            //nothing to do here at the moment
        }


        float HeightXZScaled01(float x, float z)
        {
            return -1f; // <<<--delet me!!
            // TODO:
            // Generate a height 
            // Use float noise = Simplex.Noise.Generate( x divided by noiseScale, z divided by noiseScale);
            // multiply noise by hillScale
            // add baseHeight to noise;
            // return noise

        }



        public override void GetHeightAndMoisture(float x, float z, out float altitude, out float moisture)
        {

            if (!env.applicationIsPlaying) Init(); // we may need this line later. at the moment it does nothing

            altitude = .4f;
            moisture = 0f;
            // TODO: set altitude and moisture
            // set moisture to zero for now
            // set altitude to the result of HeightXZScaled01

        }

        /// <summary>
        /// Paints the terrain inside the chunk defined by its central "position"
        /// </summary>
        /// <returns><c>true</c>, if terrain was painted, <c>false</c> otherwise.</returns>
        /// <param name="position">Central position of the chunk.</param>
        public override bool PaintChunk(VoxelChunk chunk)
        {
            return _PaintChunk(chunk);
        }



        bool _PaintChunk(VoxelChunk chunk)
        {

            //
            // We already determined the height.
            // Here we need to fill in any voxels that should exists below the height, in this chunk.
            // You'll use the height, to know how high up to go before you've hit air.
            // Or, if the entire chunk is below the surface, you'll just fill the whole thing
            //

            //
            // You'll use these variables in a moment
            //
            /*
            int startX = FastMath.FloorToInt(chunk.position.x - 8);
            int startZ = FastMath.FloorToInt(chunk.position.z - 8);

            int chunkBottomPos = FastMath.FloorToInt(chunk.position.y - 8);
            int chunkTopPos = FastMath.FloorToInt(chunk.position.y + 8);
            */

            // TODO: if chunkTopPos < minHeight 
            // set chunk.isAboveSurface to false
            // and return false (no point in proceeding further)


            // TODO: make a bool isAboveSurface. set it to false to begin with.
            // You can uncomment the following line. Most of the rest of the variables won't be 
            // written for you however.

            // bool isAboveSurface = false;

            // make a bool atleastOneVoxel. also false to begin with
            // Make a reference to the array of voxels in the chunk:

            // Voxel[] voxels = chunk.voxels; // or just uncomment this line





            // TODO: write two nested for-loops.
            // The outer one should be:
            /*
            for(int z = 0; z < 16; ++z)
            {
                // stuff goes here
            }
            */
            // The next one should be int x = 0 , x < 16
            // Write a very similar for loop inside of the z for loop
            // except use x instead of z

            // (or the outer one could be x and the next one z, the order doesn't matter here)
            // VoxelFun chunks are size 16*16*16, by the way



            // BIG TODO (don't get too scared; a lot of this is long-winded explanation):
            // Inside the x for-loop

            //    declare a var heightMapInfo = env.GetHeightMapInfoFast(startX + x, startZ + z); 

            //     ('var' in C# means "computer, please infer the correct type for me. I don't feel like typing it out myself")
            //    make an int surfaceLevel = heightMapInfo.groundLevel
            //
            //    if chunkTopPos is above surfaceLevel
            //         set isAboveSurface to true 
            //
            //    declare an int localY equal to surfaceLevel minus chunkBottomPos

            //    if localY is greater than 16, set it to 16
            //
            //    Note: if the bottom of this chunk is above the surface, localY will be negative and we want that
            //    
            //     Iterate over the y column.
            //     for y = 0  ... y < localY ... y++ 
            //         
            //            
            //          find the correct array index in the 'voxels' array based on which x, y, z we're at.
            //          (EXPLANATION: take a second to ponder the situation: the 'voxels' array is a '1D' array.
            //           you get items from it with one number not three: voxels[ SOME_NUMBER ].

            //           we have to find the right way to map x, y, and z to a number.
            //           The people who made voxel play decided to order the indices such that:
            //                  x is least significant. each x counts for 1
            //                  z is next. each z counts for a column. so each z counts for 16
            //                  y is most significant. each y counts for an 'xz' layer. so each y counts for 16 * 16)

            //           
            //
            //            So, find the correct index:
            //            Make an int index equal to y times ONE_Y_ROW plus z times ONE_Z_ROW plus x
            //                 (ONE_Y_ROW = 16*16, ONE_Z_ROW = 16. you may as well use these constants instead of writing out the numbers)
            //
            //            Finally...set the voxel to grass
            //            use 
            //            voxels[index].SetFast(topVoxel, 15, 1, Misc.color32White);
            //            also...we now know that this chunk has at least one voxel:
            //            atleastOneVoxel = true; // set to true

            //
            //  After the end of all three for-loops
            //  chunk.isAboveSurface = isAboveSurface;
            //  return atleastOneVoxel;

            return true;
        }
    }
}
