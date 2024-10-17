using System.Collections.Generic;
using System.Linq;
using TriInspector;
using UnityEngine;

namespace Flexus.ParticleMapEditor.Editor
{
    [CreateAssetMenu(fileName = Constants.Types, menuName = Constants.MapGenerating + "/" + Constants.Types)]
    public class ParticleTypes : ScriptableObject
    {
        [Space]
        public List<ParticleType> Types;

#if UNITY_EDITOR
        //[Button, PropertyOrder(-1000)]
        //private void AddType()
        //{
        //    ParticleType newType = new()
        //    {
        //        Name = "New Type " + Types.Count,
        //        Color = Random.ColorHSV()
        //    };

        //    Types.Add(newType);
        //}
#endif
    }
}