using UnityEngine;

namespace Flexus.ParticleMapEditor.Editor
{
    public class ParticleGeneratorUI : MonoBehaviour
    {
        [SerializeField] private TMPro.TMP_Text _resCount;

        public void SetResCount(int count) => _resCount.text = $"RES COUNT: {count}";
    }
}