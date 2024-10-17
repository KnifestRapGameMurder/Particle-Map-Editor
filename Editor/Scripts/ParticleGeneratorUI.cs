using UnityEngine;
using UnityEngine.UI;

namespace Flexus.ParticleMapEditor.Editor
{
    public class ParticleGeneratorUI : MonoBehaviour
    {
        [SerializeField] private Text _resCount;

        public void SetResCount(int count) => _resCount.text = $"RES COUNT: {count}";
    }
}