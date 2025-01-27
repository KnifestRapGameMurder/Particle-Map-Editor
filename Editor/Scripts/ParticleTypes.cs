#if UNITY_EDITOR
using System.Collections.Generic;
using TriInspector;
using UnityEngine;

namespace Flexus.ParticleMapEditor.Editor
{
    [CreateAssetMenu(fileName = Constants.Types, menuName = Constants.MapGenerating + "/" + Constants.Types)]
    public class ParticleTypes : ScriptableObject
    {
        [Space]
        public List<ParticleType> Types;

        private void OnEnable()
        {
            Types.ForEach(_ => _.CheckAndFixId());
        }

        [Button("Check and Fix Ids")]
        private void OnValidate()
        {
            //Debug.Log("OnValidate");
            
            for (int i = 0; i < Types.Count - 1; i++)
            {
                for (int j = i + 1; j < Types.Count; j++)
                {
                    //Debug.Log($"{i} {j}");

                    if (Types[i].Id == Types[j].Id)
                    {
                        Types[j].UpdateId();
                    }
                }
            }
        }
    }
}
#endif