﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using VoxelPlay;

namespace VE.Importers
{
    public static class MagicaVoxelImporter
    {
        #region giawa dev blog functions
        // credit: https://www.giawa.com/www.giawa.com/magicavoxel-c-importer/index.html

        // this is the default palette of voxel colors (the RGBA chunk is only included if the palette is differe)
        private static ushort[] voxColors = new ushort[] { 32767, 25599, 19455, 13311, 7167, 1023, 32543, 25375, 19231, 13087, 6943, 799, 32351, 25183,
    19039, 12895, 6751, 607, 32159, 24991, 18847, 12703, 6559, 415, 31967, 24799, 18655, 12511, 6367, 223, 31775, 24607, 18463, 12319, 6175, 31,
    32760, 25592, 19448, 13304, 7160, 1016, 32536, 25368, 19224, 13080, 6936, 792, 32344, 25176, 19032, 12888, 6744, 600, 32152, 24984, 18840,
    12696, 6552, 408, 31960, 24792, 18648, 12504, 6360, 216, 31768, 24600, 18456, 12312, 6168, 24, 32754, 25586, 19442, 13298, 7154, 1010, 32530,
    25362, 19218, 13074, 6930, 786, 32338, 25170, 19026, 12882, 6738, 594, 32146, 24978, 18834, 12690, 6546, 402, 31954, 24786, 18642, 12498, 6354,
    210, 31762, 24594, 18450, 12306, 6162, 18, 32748, 25580, 19436, 13292, 7148, 1004, 32524, 25356, 19212, 13068, 6924, 780, 32332, 25164, 19020,
    12876, 6732, 588, 32140, 24972, 18828, 12684, 6540, 396, 31948, 24780, 18636, 12492, 6348, 204, 31756, 24588, 18444, 12300, 6156, 12, 32742,
    25574, 19430, 13286, 7142, 998, 32518, 25350, 19206, 13062, 6918, 774, 32326, 25158, 19014, 12870, 6726, 582, 32134, 24966, 18822, 12678, 6534,
    390, 31942, 24774, 18630, 12486, 6342, 198, 31750, 24582, 18438, 12294, 6150, 6, 32736, 25568, 19424, 13280, 7136, 992, 32512, 25344, 19200,
    13056, 6912, 768, 32320, 25152, 19008, 12864, 6720, 576, 32128, 24960, 18816, 12672, 6528, 384, 31936, 24768, 18624, 12480, 6336, 192, 31744,
    24576, 18432, 12288, 6144, 28, 26, 22, 20, 16, 14, 10, 8, 4, 2, 896, 832, 704, 640, 512, 448, 320, 256, 128, 64, 28672, 26624, 22528, 20480,
    16384, 14336, 10240, 8192, 4096, 2048, 29596, 27482, 23254, 21140, 16912, 14798, 10570, 8456, 4228, 2114, 1  };

        private struct MagicaVoxelData
        {
            public byte x;
            public byte y;
            public byte z;
            public byte color;

            public MagicaVoxelData(BinaryReader stream, bool subsample)
            {
                // swap z and y
                // magica voxel uses z for its vertical axis
                // voxel play uses y for its vertical axis
                x = (byte)(subsample ? stream.ReadByte() / 2 : stream.ReadByte());
                z = (byte)(subsample ? stream.ReadByte() / 2 : stream.ReadByte());
                y = (byte)(subsample ? stream.ReadByte() / 2 : stream.ReadByte());
                color = stream.ReadByte();
            }
        }


        /// <summary>
        /// Load a MagicaVoxel .vox format file into the custom ushort[] structure that we use for voxel chunks.
        /// </summary>
        /// <param name="stream">An open BinaryReader stream that is the .vox file.</param>
        /// <param name="overrideColors">Optional color lookup table for converting RGB values into my internal engine color format.</param>
        /// <returns>The voxel chunk data for the MagicaVoxel .vox file.</returns>
        private static Color32[] FromMagica(BinaryReader stream, out Vector3Int size)
        {
            // check out http://voxel.codeplex.com/wikipage?title=VOX%20Format&referringTitle=Home for the file format used below
            // we're going to return a voxel chunk worth of data
            Color32[] data = null; // new Color32[32 * 128 * 32]; // new ushort[32 * 128 * 32];
            Color32[] colors = null;
            MagicaVoxelData[] voxelData = null;

            string magic = new string(stream.ReadChars(4));
            /*int version = */ stream.ReadInt32(); // never used, just advance stream cursor

            // a MagicaVoxel .vox file starts with a 'magic' 4 character 'VOX ' identifier
            int sizex = 0, sizey = 0, sizez = 0;
            size = new Vector3Int(0, 0, 0);

            if (magic == "VOX ")
            {
                bool subsample = false;

                while (stream.BaseStream.Position < stream.BaseStream.Length)
                {
                    // each chunk has an ID, size and child chunks
                    char[] chunkId = stream.ReadChars(4);
                    int chunkSize = stream.ReadInt32();
                    /*int childChunks = */ stream.ReadInt32(); // never used but we should advance the cursor
                    string chunkName = new string(chunkId);

                    // there are only 2 chunks we only care about, and they are SIZE and XYZI
                    if (chunkName == "SIZE")
                    {
                        // in magica voxel z is up.
                        // in voxel play y is up.
                        // swap z and y 
                        sizex = stream.ReadInt32();
                        sizez = stream.ReadInt32();
                        sizey = stream.ReadInt32();

                        if (sizex > 32 || sizey > 32) subsample = true;

                        stream.ReadBytes(chunkSize - 4 * 3);
                    }
                    else if (chunkName == "XYZI")
                    {
                        // XYZI contains n voxels
                        int numVoxels = stream.ReadInt32();
                        //int div = (subsample ? 2 : 1); //never used

                        // each voxel has x, y, z and color index values
                        voxelData = new MagicaVoxelData[numVoxels];
                        for (int i = 0; i < voxelData.Length; i++)
                            voxelData[i] = new MagicaVoxelData(stream, subsample);
                    }
                    else if (chunkName == "RGBA")
                    {
                        colors = new Color32[256];

                        for (int i = 0; i < 256; i++)
                        {
                            byte r = stream.ReadByte();
                            byte g = stream.ReadByte();
                            byte b = stream.ReadByte();
                            /*byte a = */ stream.ReadByte(); // never used. advance cursor

                            colors[i] = new Color32(r, g, b, 255); 
                        }
                    }
                    else stream.ReadBytes(chunkSize);   // read any excess bytes
                }

                data = new Color32[sizex * sizey * sizez];

                if (voxelData.Length == 0)
                {
                     return data; // failed to read any valid voxel data
                }

                if(sizex == 0 || sizey == 0 || sizez == 0)
                {
                    return data;
                }

                if (colors == null)
                {
                    Debug.LogWarning("no colors array");
                }
                else Debug.Log("colors len: " + colors.Length);

                // now push the voxel data into our voxel chunk structure
                for (int i = 0; i < voxelData.Length; i++)
                {
                    // do not store this voxel if it lies out of range of the voxel chunk (32x128x32)
                    if (voxelData[i].x > 31 || voxelData[i].y > 127 || voxelData[i].z > 31) continue; 

                    // use the voxColors array by default, or overrideColor if it is available
                    int voxel = voxelData[i].x + voxelData[i].z * sizex + voxelData[i].y * sizex * sizez;

                    Color32 c;
                    if(colors != null)
                    {
                        c = colors[voxelData[i].color - 1];
                    } else
                    {
                        c = FromUShortColor(voxColors[voxelData[i].color - 1]);
                    }
                    data[voxel] = c;
                }
            }

            size = new Vector3Int(sizex, sizey, sizez);
            return data;
        }



        private static Color32 FromUShortColor(ushort u)
        {
            /*
            // convert RGBA to our custom voxel format (16 bits, 0RRR RRGG GGGB BBBB)
               colors[i] = (ushort)(((r & 0x1f) << 10) | ((g & 0x1f) << 5) | (b & 0x1f));
             */
            Color32 c = new Color32();
            c.r = (byte)(((u >> 10) & 0x1f) * 8);
            c.g = (byte)(((u >> 5) & 0x1f) * 8);
            c.b = (byte)((u & 0x1f) * 8);
            c.a = 255;
            return c;
        }

        #endregion

        public static ColorBasedModelDefinition ImportBinary(Stream stream, Encoding encoding)
        {
            var br = new BinaryReader(stream, encoding);
            Vector3Int size;
            var data = FromMagica(br, out size);
            ColorBasedModelDefinition result = new ColorBasedModelDefinition();
            result.sizeX = size.x;
            result.sizeY = size.y;
            result.sizeZ = size.z;
            Debug.Log("size: " + size.ToString() + " data len: " + data.Length);

            result.colors = data;

            VoxelPlayFormatTools.DBUGCheckNonAlphaZeroColors(result.colors);

            return result;
        }
    }
}
