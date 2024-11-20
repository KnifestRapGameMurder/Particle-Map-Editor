using System;
using System.Collections.Generic;
using System.Linq;
using TriInspector;
using UnityEngine;

namespace Flexus.ParticleMapEditor.Editor
{
    [CreateAssetMenu(fileName = "LevelObjects", menuName = Constants.MapGenerating + "/" + "LevelObjects")]
    public class LevelObjectsConfig : ScriptableObject
    {
        public List<LevelObjectConfig> levelObjects;

        private void OnValidate()
        {
            foreach (var levelObject in levelObjects.Where(levelObject => string.IsNullOrEmpty(levelObject.id)))
            {
                levelObject.id = Guid.NewGuid().ToString();
            }

            foreach (var group in levelObjects.GroupBy(lo => lo.id))
            {
                foreach (var levelObjectConfig in group.Skip(1))
                {
                    levelObjectConfig.id = Guid.NewGuid().ToString();
                }
            }
        }
    }

    [Serializable]
    public class LevelObjectConfig : IParticleType
    {
        [HideInInspector] public string id;
        public string name;
        public float radius;
        public Transform prefab;
        public string Id => id;
    }
}