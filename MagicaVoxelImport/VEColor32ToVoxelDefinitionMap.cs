using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VoxelPlay;

namespace VE.Importers
{
    [System.Serializable]
    public struct ColorToVoxDef
    {
        public Color32 color;
        public VoxelDefinition voxDef;
        public bool replaceColorWithWhite;
    }

    public class VEColor32ToVoxelDefinitionMap : ScriptableObject
    {
        public ColorToVoxDef[] Map;

        public struct VoxDefReplace
        {
            public VoxelDefinition voxDef;
            public bool replaceColorsWithWhite;
        }

        Dictionary<Color32, VoxDefReplace> _colorSet;
        Dictionary<Color32, VoxDefReplace> colorSet {
            get {
                if(_colorSet == null)
                {
                    _colorSet = GetHashSet();
                }
                return _colorSet;
            }
        }

        Dictionary<Color32, VoxDefReplace> GetHashSet()
        {
            var lookup = new Dictionary<Color32, VoxDefReplace>();
            foreach(var cv in Map)
            {
                if(!lookup.ContainsKey(cv.color))
                    lookup.Add(cv.color, new VoxDefReplace
                    {
                        voxDef = cv.voxDef,
                        replaceColorsWithWhite = cv.replaceColorWithWhite
                    });
            }
            return lookup;
        }

        VoxDefReplace Lookup(Color32 c)
        {
            if (colorSet.ContainsKey(c))
            {
                return colorSet[c];
            }
            return default(VoxDefReplace);
        }

        public bool Lookup(Color32 c, out VoxDefReplace vdef)
        {
            if (colorSet.ContainsKey(c))
            {
                vdef = colorSet[c];
                return true;
            }
            vdef = Lookup(c);
            return false;
        }


    }

}
