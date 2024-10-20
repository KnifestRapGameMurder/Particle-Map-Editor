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
        [Group(Constants.Dev)] public Texture2D Texture;
        [Group(Constants.Dev)] public LayerMask CanvasLayer;
        [Group(Constants.Dev)] public VerletParticleGenerator ParticleGenerator;
        [Group(Constants.Dev)] public Transform BrushPreview;
        [Group(Constants.Dev)] public Transform PaintBucketPreview;
        [Group(Constants.Dev)] public SpriteRenderer BrushPreviewMaterial;
        [Group(Constants.Dev)] public Transform ZoomStartPreview;
        [Group(Constants.Dev)] public Transform ZoomStopPreview;
        [Group(Constants.Dev)]
        [SerializeField] private KeyCode _zoomingKey = KeyCode.Q;
        [Group(Constants.Dev)]
        [SerializeField] private KeyCode _bucketPaintingKey = KeyCode.G;

        private Vector2 _startUV;
        private Vector2 _stopUV;
        private Vector2Int? _texSize;

        private Vector2Int TexSize
        {
            get
            {
                if (_texSize == null)
                    _texSize = new Vector2Int(Texture.width, Texture.height);

                return _texSize.Value;
            }
        }

        public Color BrushColor => ParticleGenerator.Settings.BrushColor;
        public float BrushSize => ParticleGenerator.Settings.BrushSize;

        void Update()
        {
            Vector2? uvPos = GetMouseUVPosition();

            ZoomStartPreview.gameObject.SetActive(false);
            ZoomStopPreview.gameObject.SetActive(false);
            PaintBucketPreview.gameObject.SetActive(false);
            BrushPreview.gameObject.SetActive(false);

            if (!uvPos.HasValue)
                return;

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
                Color color = BrushColor;
                color.a = 0.5f;
                BrushPreviewMaterial.color = color;

                if (Input.GetMouseButton(0))
                    ApplyBrush2D(uvPos.Value);
            }
        }

#if UNITY_EDITOR
        private void OnDestroy()
        {
            EditorUtility.SetDirty(Texture);
        }
#endif

        Vector2? GetMouseUVPosition()
        {
            Vector3 mousePos = Input.mousePosition;
            Ray ray = Camera.main.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out var hit, 10, CanvasLayer))
                return hit.textureCoord;

            return null;
        }

        private void ApplyBrush2D(Vector2 uvPos)
        {
            Vector2Int brushPos = new Vector2Int((int)(uvPos.x * TexSize.x), (int)(uvPos.y * TexSize.y));
            int sqrBrushRadius = (int)(TexSize.x * BrushSize * TexSize.x * BrushSize);

            for (int i = 0; i < TexSize.x; i++)
            {
                for (int j = 0; j < TexSize.y; j++)
                {
                    Vector2Int pixel = new Vector2Int(i, j);
                    int sqrDist = (brushPos - pixel).sqrMagnitude;

                    if (sqrDist <= sqrBrushRadius)
                        Texture.SetPixel(i, j, BrushColor);
                }
            }

            Texture.Apply();
        }

        private void ApplyPaintBucket(Vector2 uvPos)
        {
            // Calculate the starting pixel position based on the UV coordinates.
            Vector2Int startPixel = new Vector2Int((int)(uvPos.x * TexSize.x), (int)(uvPos.y * TexSize.y));

            // Get the color of the starting pixel.
            Color targetColor = Texture.GetPixel(startPixel.x, startPixel.y);

            // If the brush color is the same as the target color, do nothing to prevent infinite loop.
            if (targetColor == BrushColor)
                return;


            // Use a queue for the flood-fill algorithm.
            Queue<Vector2Int> pixelQueue = new Queue<Vector2Int>();
            pixelQueue.Enqueue(startPixel);

            // Create a HashSet to keep track of visited pixels.
            HashSet<Vector2Int> visitedPixels = new HashSet<Vector2Int> { startPixel };

            // While there are still pixels to process.
            while (pixelQueue.Count > 0)
            {
                Vector2Int currentPixel = pixelQueue.Dequeue();

                // Replace the color of the current pixel with the brush color.
                Texture.SetPixel(currentPixel.x, currentPixel.y, BrushColor);

                // Check the 4 neighboring pixels (up, down, left, right).
                foreach (Vector2Int neighbor in GetNeighbors(currentPixel))
                {
                    // If the neighbor has not been visited yet and its color matches the target color.
                    if (!visitedPixels.Contains(neighbor) && Texture.GetPixel(neighbor.x, neighbor.y) == targetColor)
                    {
                        pixelQueue.Enqueue(neighbor);
                        visitedPixels.Add(neighbor);
                    }
                }
            }

            // Apply the changes to the texture.
            Texture.Apply();
        }

        // Helper function to get the 4 neighbors of a pixel.
        private IEnumerable<Vector2Int> GetNeighbors(Vector2Int pixel)
        {
            // Define the relative positions of the 4 neighbors.
            Vector2Int[] neighbors = new Vector2Int[]
            {
                new Vector2Int(pixel.x + 1, pixel.y), // Right
                new Vector2Int(pixel.x - 1, pixel.y), // Left
                new Vector2Int(pixel.x, pixel.y + 1), // Up
                new Vector2Int(pixel.x, pixel.y - 1)  // Down
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
        private void Clear()
        {
            Texture.SetPixels(Enumerable.Repeat(Color.black, Texture.width * Texture.height).ToArray());
            Texture.Apply();
        }

        private void CutTexture()
        {

            Vector2Int min = new Vector2Int(
                ((int)(Mathf.Min(_startUV.x, _stopUV.x) * TexSize.x)),
                ((int)(Mathf.Min(_startUV.y, _stopUV.y) * TexSize.y)));
            Vector2Int max = new Vector2Int(
                ((int)(Mathf.Max(_startUV.x, _stopUV.x) * TexSize.x)),
                ((int)(Mathf.Max(_startUV.y, _stopUV.y) * TexSize.y)));

            Dictionary<Vector2Int, Color> oldColors = GetTexureColors();

            for (int i = 0; i < TexSize.x; i++)
            {
                for (int j = 0; j < TexSize.y; j++)
                {
                    Vector2Int pixel = new Vector2Int(i, j);
                    Vector2 sourcePixel = new Vector2(pixel.x, pixel.y).Remap(Vector2Int.zero, TexSize, min, max);
                    Texture.SetPixel(pixel.x, pixel.y, oldColors[new Vector2Int(((int)sourcePixel.x), ((int)sourcePixel.y))]);
                }
            }

            Texture.Apply();
        }

        public Dictionary<Vector2Int, Color> GetTexureColors()
        {
            Dictionary<Vector2Int, Color> colors = new();

            for (int i = 0; i < TexSize.x; i++)
                for (int j = 0; j < TexSize.y; j++)
                    colors[new Vector2Int(i, j)] = Texture.GetPixel(i, j);

            return colors;
        }

        public void SetTexturePixels(Dictionary<Vector2Int, Color> pixels)
        {
            foreach (var pixel in pixels)
                Texture.SetPixel(pixel.Key.x, pixel.Key.y, pixel.Value);

            Texture.Apply();
        }
    }
}