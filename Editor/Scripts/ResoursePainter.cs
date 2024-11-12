using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TriInspector;
using UnityEditor;
using UnityEngine;

namespace Flexus.ParticleMapEditor.Editor
{
    [DeclareFoldoutGroup(Constants.Dev)]
    public class ResoursePainter : MonoBehaviour
    {
        [Button]
        private void TestLinq()
        {
            var values = new[] { 1, 2, 3, 4 };
            var selected = values.Select(n => n * 2);
            for (int i = 0; i < 3; i++)
            {
                values[i] *= 10;
                Debug.Log(string.Join(", ", selected));
            }
        }

        [Group(Constants.Dev)] public Texture2D Texture;
        [Group(Constants.Dev)] public LayerMask CanvasLayer;
        [Group(Constants.Dev)] public VerletParticleGenerator ParticleGenerator;
        [Group(Constants.Dev)] public Transform BrushPreview;
        [Group(Constants.Dev)] public Transform PaintBucketPreview;
        [Group(Constants.Dev)] public SpriteRenderer BrushPreviewMaterial;
        [Group(Constants.Dev)] public Transform ZoomStartPreview;
        [Group(Constants.Dev)] public Transform ZoomStopPreview;

        [Group(Constants.Dev)] [SerializeField]
        private KeyCode _zoomingKey = KeyCode.Q;

        [Group(Constants.Dev)] [SerializeField]
        private KeyCode _bucketPaintingKey = KeyCode.G;

        private Vector2 _startUV;
        private Vector2 _stopUV;
        private Vector2Int? _texSize;
        private bool _wasMouseDown;
        private Vector2 _lastUV;
        private Camera _camera;

        public float LastPaintTime { get; private set; }

        private Vector2Int TexSize
        {
            get
            {
                _texSize ??= new Vector2Int(Texture.width, Texture.height);

                return _texSize.Value;
            }
        }

        private Color BrushColor => ParticleGenerator.Settings.BrushColor;
        private float BrushSize => ParticleGenerator.Settings.BrushSize;
        private float BrushInterpolateStep => BrushSize;

        private Camera Camera
        {
            get
            {
                if (!_camera) _camera = Camera.main;
                return _camera;
            }
        }

        private void Update()
        {
            var uvPos = GetMouseUVPosition();

            ZoomStartPreview.gameObject.SetActive(false);
            ZoomStopPreview.gameObject.SetActive(false);
            PaintBucketPreview.gameObject.SetActive(false);
            BrushPreview.gameObject.SetActive(false);

            if (!uvPos.HasValue)
                return;

            bool isMouseDown = false;

            if (Input.GetKey(_zoomingKey))
            {
                ZoomStartPreview.gameObject.SetActive(true);
                ZoomStopPreview.gameObject.SetActive(true);

                if (Input.GetMouseButtonDown(0))
                {
                    _startUV = uvPos.Value;
                    ZoomStartPreview.localPosition = _startUV - 0.5f * Vector2.one;
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    _stopUV = uvPos.Value;
                    CutTexture();
                }

                ZoomStopPreview.localPosition = uvPos.Value - 0.5f * Vector2.one;
            }
            else if (Input.GetKey(_bucketPaintingKey))
            {
                PaintBucketPreview.gameObject.SetActive(true);
                PaintBucketPreview.localPosition = uvPos.Value - 0.5f * Vector2.one;

                if (Input.GetMouseButtonDown(0))
                {
                    ApplyPaintBucket(uvPos.Value);
                }
            }
            else
            {
                BrushPreview.gameObject.SetActive(true);
                BrushPreview.localPosition = uvPos.Value - 0.5f * Vector2.one;
                BrushPreview.localScale = new Vector3(BrushSize * 2f, BrushSize * 2f, 1f);
                var color = BrushColor;
                color.a = 0.5f;
                BrushPreviewMaterial.color = color;

                if (Input.GetMouseButton(0))
                {
                    isMouseDown = true;

                    if (_wasMouseDown)
                    {
                        var deltaDist = (uvPos.Value - _lastUV).magnitude;

                        if (deltaDist > BrushInterpolateStep)
                        {
                            var steps = Mathf.CeilToInt(deltaDist / BrushInterpolateStep);
                            var points = Enumerable.Range(1, steps)
                                .Select(t => Vector2.Lerp(_lastUV, uvPos.Value, t / (float)steps));
                            ApplyBrush2D(points);
                        }
                        else
                        {
                            ApplyBrush2D(uvPos.Value);
                        }
                    }
                    else
                    {
                        ApplyBrush2D(uvPos.Value);
                    }
                }
            }

            _wasMouseDown = isMouseDown;
            _lastUV = uvPos.Value;
        }

#if UNITY_EDITOR
        private void OnDestroy()
        {
            EditorUtility.SetDirty(Texture);
        }
#endif

        private Vector2? GetMouseUVPosition()
        {
            if (!Camera) return null;

            var ray = Camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 10, CanvasLayer)) return hit.textureCoord;
            return null;
        }

        private void ApplyBrush2D(IEnumerable<Vector2> uvPos)
        {
            var brushPos = uvPos.Select(uv => new Vector2Int((int)(uv.x * TexSize.x), (int)(uv.y * TexSize.y)))
                .ToList();
            var sqrBrushRadius = (int)(TexSize.x * BrushSize * TexSize.x * BrushSize);

            for (var i = 0; i < TexSize.x; i++)
            {
                for (var j = 0; j < TexSize.y; j++)
                {
                    var pixel = new Vector2Int(i, j);
                    if (brushPos.Any(brush => (brush - pixel).sqrMagnitude <= sqrBrushRadius))
                        Texture.SetPixel(i, j, BrushColor);
                }
            }

            ApplyTexture();
        }

        private void ApplyBrush2D(Vector2 uvPos)
        {
            var brushPos = new Vector2Int((int)(uvPos.x * TexSize.x), (int)(uvPos.y * TexSize.y));
            var sqrBrushRadius = (int)(TexSize.x * BrushSize * TexSize.x * BrushSize);

            for (var i = 0; i < TexSize.x; i++)
            {
                for (var j = 0; j < TexSize.y; j++)
                {
                    var pixel = new Vector2Int(i, j);
                    var sqrDist = (brushPos - pixel).sqrMagnitude;

                    if (sqrDist <= sqrBrushRadius)
                        Texture.SetPixel(i, j, BrushColor);
                }
            }

            ApplyTexture();
        }

        private void ApplyPaintBucket(Vector2 uvPos)
        {
            // Calculate the starting pixel position based on the UV coordinates.
            var startPixel = new Vector2Int((int)(uvPos.x * TexSize.x), (int)(uvPos.y * TexSize.y));

            // Get the color of the starting pixel.
            var targetColor = Texture.GetPixel(startPixel.x, startPixel.y);

            // If the brush color is the same as the target color, do nothing to prevent infinite loop.
            if (targetColor == BrushColor)
                return;


            // Use a queue for the flood-fill algorithm.
            var pixelQueue = new Queue<Vector2Int>();
            pixelQueue.Enqueue(startPixel);

            // Create a HashSet to keep track of visited pixels.
            var visitedPixels = new HashSet<Vector2Int> { startPixel };

            // While there are still pixels to process.
            while (pixelQueue.Count > 0)
            {
                var currentPixel = pixelQueue.Dequeue();

                // Replace the color of the current pixel with the brush color.
                Texture.SetPixel(currentPixel.x, currentPixel.y, BrushColor);

                // Check the 4 neighboring pixels (up, down, left, right).
                foreach (var neighbor in GetNeighbors(currentPixel))
                {
                    // If the neighbor has not been visited yet and its color matches the target color.
                    if (visitedPixels.Contains(neighbor) ||
                        Texture.GetPixel(neighbor.x, neighbor.y) != targetColor) continue;

                    pixelQueue.Enqueue(neighbor);
                    visitedPixels.Add(neighbor);
                }
            }

            ApplyTexture();
        }

        // Helper function to get the 4 neighbors of a pixel.
        private IEnumerable<Vector2Int> GetNeighbors(Vector2Int pixel)
        {
            // Define the relative positions of the 4 neighbors.
            var neighbors = new[]
            {
                new Vector2Int(pixel.x + 1, pixel.y), // Right
                new Vector2Int(pixel.x - 1, pixel.y), // Left
                new Vector2Int(pixel.x, pixel.y + 1), // Up
                new Vector2Int(pixel.x, pixel.y - 1) // Down
            };

            // Ensure the neighbors are within the bounds of the texture.
            foreach (var neighbor in neighbors)
            {
                if (neighbor.x >= 0 && neighbor.x < TexSize.x && neighbor.y >= 0 && neighbor.y < TexSize.y)
                {
                    yield return neighbor;
                }
            }
        }

        [Button, PropertyOrder(-1000)]
        [PropertySpace(SpaceAfter = 10)]
        // ReSharper disable once UnusedMember.Local
        private void Clear()
        {
            Texture.SetPixels(Enumerable.Repeat(Color.black, Texture.width * Texture.height).ToArray());
            ApplyTexture();
        }

        private void CutTexture()
        {
            var min = new Vector2Int(
                ((int)(Mathf.Min(_startUV.x, _stopUV.x) * TexSize.x)),
                ((int)(Mathf.Min(_startUV.y, _stopUV.y) * TexSize.y)));
            var max = new Vector2Int(
                ((int)(Mathf.Max(_startUV.x, _stopUV.x) * TexSize.x)),
                ((int)(Mathf.Max(_startUV.y, _stopUV.y) * TexSize.y)));

            var oldColors = GetTexureColors();

            for (var i = 0; i < TexSize.x; i++)
            {
                for (var j = 0; j < TexSize.y; j++)
                {
                    var pixel = new Vector2Int(i, j);
                    var sourcePixel = new Vector2(pixel.x, pixel.y).Remap(Vector2Int.zero, TexSize, min, max);
                    Texture.SetPixel(pixel.x, pixel.y,
                        oldColors[new Vector2Int(((int)sourcePixel.x), ((int)sourcePixel.y))]);
                }
            }

            ApplyTexture();
        }

        private void ApplyTexture()
        {
            Texture.Apply();
            LastPaintTime = Time.time;
        }

        public Dictionary<Vector2Int, Color> GetTexureColors()
        {
            Dictionary<Vector2Int, Color> colors = new();

            for (var i = 0; i < TexSize.x; i++)
            for (var j = 0; j < TexSize.y; j++)
                colors[new Vector2Int(i, j)] = Texture.GetPixel(i, j);

            return colors;
        }

        public void SetTexturePixels(Dictionary<Vector2Int, Color> pixels)
        {
            foreach (var pixel in pixels)
                Texture.SetPixel(pixel.Key.x, pixel.Key.y, pixel.Value);

            ApplyTexture();
        }
    }
}