using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Flexus.ParticleMapEditor.Editor
{
    public class ParticleGeneratorUI : MonoBehaviour
    {
        [SerializeField] private TMPro.TMP_Text _totalCount;
        [SerializeField] private TMPro.TMP_Text _resCount;

        private StringBuilder _stringBuilder = new();

        public void SetResCount(int count) => _totalCount.text = $"RES COUNT: {count}";

        public void SetResCount(IEnumerable<ResCountArgs> args)
        {
            _stringBuilder.Clear();

            foreach (ResCountArgs arg in args)
            {
                string colorHex = ColorUtility.ToHtmlStringRGBA(arg.ResColor);
                _stringBuilder.Append($"<color #{colorHex}>{arg.ResName}: {arg.ResCount}</color>\n");
            }

            _resCount.text = _stringBuilder.ToString();
        }

        public struct ResCountArgs
        {
            public string ResName;
            public int ResCount;
            public Color ResColor;
        }
    }
}