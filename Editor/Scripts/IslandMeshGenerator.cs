using System;
using UnityEngine;
using UnityEngine.UI;
using TriInspector; // Assuming you're using TriInspector

namespace Flexus.ParticleMapEditor.Editor
{
    public class IslandMeshGenerator : MonoBehaviour
    {
        public IslandMeshGeneratingSettings settings;
        public MeshFilter meshFilter;
        public RawImage texturePreview;

        private Texture2D IslandTexture => settings.islandTexture;
        private int AreaSize => settings.areaSize;
        private int PixelsPerUnit => settings.pixelsPerUnit;
        private float BorderRadius => settings.borderRadius;
        private float BlurRadius => settings.blurRadius;
        private int BlurPower => settings.blurPower;
        private int MeshResolutionPower => settings.meshResolutionPower;
        private float IslandHeight => settings.islandHeight;
        private bool IsResize => settings.resize;
        private bool IsNormalizeColor => settings.normalizeColor;
        private bool IsAddBorders => settings.addBorders;
        private bool IsBlur => settings.blur;

        [Button("Generate")]
        public void Generate()
        {
            if (IslandTexture == null)
            {
                Debug.LogError("Island Texture is not assigned!");
                return;
            }

            var texture = IslandTexture;

            if (IsResize) texture = ResizeTexture(texture, AreaSize * PixelsPerUnit, AreaSize * PixelsPerUnit);
            if (IsNormalizeColor) texture = ConvertToBlackAndWhite(texture);
            if (IsAddBorders) texture = AddBorders(texture, BorderRadius * PixelsPerUnit);
            if (IsBlur) texture = BlurTexture(texture, BlurRadius * PixelsPerUnit, Mathf.Pow(2, BlurPower));

            DisplayTexture(texture);

            var meshResolution = (int)(AreaSize * Math.Pow(2, MeshResolutionPower));
            var islandMesh = GenerateIslandMesh(texture, AreaSize, IslandHeight, meshResolution);
            meshFilter.mesh = islandMesh;
        }

        private static Texture2D ResizeTexture(Texture2D texture, int width, int height)
        {
            var rt = RenderTexture.GetTemporary(width, height);
            RenderTexture.active = rt;

            // Copy the original texture to the render texture at the new size
            Graphics.Blit(texture, rt);

            // Create a new Texture2D with the resized dimensions
            var resizedTexture = new Texture2D(width, height, texture.format, false);
            resizedTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            resizedTexture.Apply();

            // Clean up
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            return resizedTexture;
        }


        Texture2D UpscaleTexture(Texture2D texture, int factor)
        {
            var newWidth = texture.width * factor;
            var newHeight = texture.height * factor;
            var upscaledTexture = new Texture2D(newWidth, newHeight);

            for (var y = 0; y < newHeight; y++)
            {
                for (var x = 0; x < newWidth; x++)
                {
                    // Nearest neighbor upscale
                    var color = texture.GetPixel(x / factor, y / factor);
                    upscaledTexture.SetPixel(x, y, color);
                }
            }

            upscaledTexture.Apply();
            return upscaledTexture;
        }

        private static Texture2D ConvertToBlackAndWhite(Texture2D texture)
        {
            var width = texture.width;
            var height = texture.height;

            var blackAndWhiteTexture = new Texture2D(width, height);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var color = texture.GetPixel(x, y);
                    blackAndWhiteTexture.SetPixel(x, y, color.grayscale > 0.1f ? Color.white : Color.black);
                }
            }

            blackAndWhiteTexture.Apply();
            return blackAndWhiteTexture;
        }

        private static Texture2D AddBorders(Texture2D texture, float radius)
        {
            var blurredTexture = new Texture2D(texture.width, texture.height);
            var width = texture.width;
            var height = texture.height;

            var originalPixels = texture.GetPixels();
            var blurredPixels = new Color[originalPixels.Length];

            var radiusInt = Mathf.CeilToInt(radius);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var blurredPixel = new Color(0, 0, 0, 0);

                    for (var i = -radiusInt; i <= radiusInt; i++)
                    {
                        for (var j = -radiusInt; j <= radiusInt; j++)
                        {
                            var sampleX = Mathf.Clamp(x + i, 0, width - 1);
                            var sampleY = Mathf.Clamp(y + j, 0, height - 1);

                            var sampled = originalPixels[sampleY * width + sampleX];
                            blurredPixel.r = Mathf.Max(blurredPixel.r, sampled.r);
                            blurredPixel.g = Mathf.Max(blurredPixel.g, sampled.g);
                            blurredPixel.b = Mathf.Max(blurredPixel.b, sampled.b);
                            blurredPixel.a = Mathf.Max(blurredPixel.a, sampled.a);
                        }
                    }

                    blurredPixels[y * width + x] = blurredPixel;
                }
            }

            blurredTexture.SetPixels(blurredPixels);
            blurredTexture.Apply();
            return blurredTexture;
        }

        private static Texture2D BlurTexture(Texture2D texture, float radius, float power = 1f)
        {
            var blurredTexture = new Texture2D(texture.width, texture.height);
            var width = texture.width;
            var height = texture.height;

            var originalPixels = texture.GetPixels();
            var blurredPixels = new Color[originalPixels.Length];

            var radiusInt = Mathf.CeilToInt(radius);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var blurredPixel = new Color(0, 0, 0, 0);
                    var sampleCount = 0;

                    for (var i = -radiusInt; i <= radiusInt; i++)
                    {
                        for (var j = -radiusInt; j <= radiusInt; j++)
                        {
                            var sampleX = Mathf.Clamp(x + i, 0, width - 1);
                            var sampleY = Mathf.Clamp(y + j, 0, height - 1);

                            blurredPixel += originalPixels[sampleY * width + sampleX];
                            sampleCount++;
                        }
                    }

                    blurredPixel /= sampleCount;
                    blurredPixel.r = Mathf.Pow(blurredPixel.r, power);
                    blurredPixel.g = Mathf.Pow(blurredPixel.g, power);
                    blurredPixel.b = Mathf.Pow(blurredPixel.b, power);
                    blurredPixels[y * width + x] = blurredPixel;
                }
            }

            blurredTexture.SetPixels(blurredPixels);
            blurredTexture.Apply();
            return blurredTexture;
        }


        private static Texture2D GaussianBlur(Texture2D texture, float radius)
        {
            var width = texture.width;
            var height = texture.height;
            var blurredTexture = new Texture2D(width, height);

            var kernel = GenerateGaussianKernel(radius);
            var kernelSize = kernel.Length;

            // Apply blur horizontally
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var blurredColor = new Color(0, 0, 0);
                    var weightSum = 0f;

                    for (var k = -kernelSize / 2; k <= kernelSize / 2; k++)
                    {
                        var sampleX = Mathf.Clamp(x + k, 0, width - 1);
                        var sampleColor = texture.GetPixel(sampleX, y);
                        var weight = kernel[k + kernelSize / 2];
                        blurredColor += sampleColor * weight;
                        weightSum += weight;
                    }

                    blurredTexture.SetPixel(x, y, blurredColor / weightSum);
                }
            }

            blurredTexture.Apply();

            // Apply blur vertically
            var finalBlurredTexture = new Texture2D(width, height);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var blurredColor = new Color(0, 0, 0);
                    var weightSum = 0f;

                    for (var k = -kernelSize / 2; k <= kernelSize / 2; k++)
                    {
                        var sampleY = Mathf.Clamp(y + k, 0, height - 1);
                        var sampleColor = blurredTexture.GetPixel(x, sampleY);
                        var weight = kernel[k + kernelSize / 2];
                        blurredColor += sampleColor * weight;
                        weightSum += weight;
                    }

                    finalBlurredTexture.SetPixel(x, y, blurredColor / weightSum);
                }
            }

            finalBlurredTexture.Apply();
            return finalBlurredTexture;
        }

        private static float[] GenerateGaussianKernel(float radius)
        {
            var size = Mathf.CeilToInt(radius * 3f) * 2 + 1; // Kernel size based on radius
            var kernel = new float[size];
            var sigma = radius / 2f;
            var sum = 0f;
            var halfSize = size / 2;

            for (var i = 0; i < size; i++)
            {
                float x = i - halfSize;
                kernel[i] = Mathf.Exp(-x * x / (2 * sigma * sigma)) / (Mathf.Sqrt(2 * Mathf.PI) * sigma);
                sum += kernel[i];
            }

            // Normalize the kernel
            for (var i = 0; i < size; i++)
            {
                kernel[i] /= sum;
            }

            return kernel;
        }

        private static Mesh GenerateIslandMesh(Texture2D texture, float width, float height, int resolution)
        {
            var texWidth = texture.width;
            var texHeight = texture.height;

            resolution += 1;

            // Create a grid mesh with the specified resolution
            var vertices = new Vector3[resolution * resolution];
            var uvs = new Vector2[resolution * resolution];
            var triangles = new int[(resolution - 1) * (resolution - 1) * 6];

            var scaleX = width / (resolution - 1);
            var scaleZ = width / (resolution - 1);

            var vertexIndex = 0;
            var triangleIndex = 0;

            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    // World position
                    var posX = x * scaleX - width / 2;
                    var posZ = y * scaleZ - width / 2;

                    // Sample height from the blurred texture
                    var texU = (float)x / resolution * texWidth;
                    var texV = (float)y / resolution * texHeight;
                    var pixelHeight = texture.GetPixelBilinear(texU / texWidth, texV / texHeight).grayscale;

                    var posY = pixelHeight * height - height;

                    // Set vertex position and UVs
                    vertices[vertexIndex] = new Vector3(posX, posY, posZ);
                    uvs[vertexIndex] = new Vector2((float)x / resolution, (float)y / resolution);

                    // Create triangles if not on the last row/column
                    if (x < resolution - 1 && y < resolution - 1)
                    {
                        var a = vertexIndex;
                        var b = vertexIndex + resolution;
                        var c = vertexIndex + resolution + 1;
                        var d = vertexIndex + 1;

                        // First triangle
                        triangles[triangleIndex] = a;
                        triangles[triangleIndex + 1] = b;
                        triangles[triangleIndex + 2] = c;

                        // Second triangle
                        triangles[triangleIndex + 3] = a;
                        triangles[triangleIndex + 4] = c;
                        triangles[triangleIndex + 5] = d;

                        triangleIndex += 6;
                    }

                    vertexIndex++;
                }
            }

            // Create mesh
            var mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        void DisplayTexture(Texture2D texture)
        {
            // Check if RawImage component is assigned for preview
            if (texturePreview != null)
            {
                texturePreview.texture = texture;
            }
            else
            {
                Debug.LogWarning("No RawImage assigned for preview. Please assign a UI RawImage in the Inspector.");
            }
        }
    }
}