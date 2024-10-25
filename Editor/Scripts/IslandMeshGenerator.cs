using UnityEngine;
using UnityEngine.UI;
using TriInspector; // Assuming you're using TriInspector

namespace Flexus.ParticleMapEditor.Editor
{
    public class IslandMeshGenerator : MonoBehaviour
    {
        public Texture2D islandTexture; // The mask texture
        public int upscaleFactor = 4; // How much to upscale the texture for smoothing
        public int blurIterations = 3; // Number of times to apply the blur
        public float blurRadius = 1f; // Radius of the Gaussian blur
        public float width = 10f; // Width of the generated island in X and Z
        public float height = 5f; // Height of the generated island in Y
        public int meshResolution = 256; // Resolution of the grid (vertices along X and Z)
        public RawImage texturePreview; // Assign a UI RawImage in the inspector to display the preview
        public MeshFilter meshFilter; // Assign the MeshFilter in the inspector to display the generated mesh

        [Button("Preview Upscaled Texture and Generate Mesh")]
        public void PreviewUpscaledTextureAndGenerateMesh()
        {
            if (islandTexture == null)
            {
                Debug.LogError("Island Texture is not assigned!");
                return;
            }

            // Step 1: Upscale the texture
            Texture2D upscaledTexture = UpscaleTexture(islandTexture, upscaleFactor);

            // Step 2: Convert non-black areas to white (creating a clean black-and-white mask)
            Texture2D blackAndWhiteTexture = ConvertToBlackAndWhite(upscaledTexture);

            // Step 3: Apply Gaussian blur to create smooth edges
            Texture2D blurredTexture = ApplyGaussianBlur(blackAndWhiteTexture, blurIterations, blurRadius);

            // Step 4: Display the upscaled and modified texture in the UI
            DisplayTexture(blurredTexture);

            // Step 5: Generate the mesh based on the blurred texture (used as a heightmap)
            Mesh islandMesh = GenerateIslandMesh(blurredTexture, width, height, meshResolution);
            meshFilter.mesh = islandMesh;
        }

        Texture2D UpscaleTexture(Texture2D texture, int factor)
        {
            int newWidth = texture.width * factor;
            int newHeight = texture.height * factor;
            Texture2D upscaledTexture = new Texture2D(newWidth, newHeight);

            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    // Nearest neighbor upscale
                    Color color = texture.GetPixel(x / factor, y / factor);
                    upscaledTexture.SetPixel(x, y, color);
                }
            }
            upscaledTexture.Apply();
            return upscaledTexture;
        }

        Texture2D ConvertToBlackAndWhite(Texture2D texture)
        {
            int width = texture.width;
            int height = texture.height;

            Texture2D blackAndWhiteTexture = new Texture2D(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color color = texture.GetPixel(x, y);

                    // If the pixel is not close to black, convert it to white
                    if (color.grayscale > 0.1f) // Treat anything with intensity > 0.1 as non-black
                    {
                        blackAndWhiteTexture.SetPixel(x, y, Color.white);
                    }
                    else
                    {
                        blackAndWhiteTexture.SetPixel(x, y, Color.black);
                    }
                }
            }

            blackAndWhiteTexture.Apply();
            return blackAndWhiteTexture;
        }

        Texture2D ApplyGaussianBlur(Texture2D texture, int iterations, float radius)
        {
            Texture2D blurredTexture = texture;

            for (int i = 0; i < iterations; i++)
            {
                blurredTexture = GaussianBlur(blurredTexture, radius);
            }

            return blurredTexture;
        }

        Texture2D GaussianBlur(Texture2D texture, float radius)
        {
            int width = texture.width;
            int height = texture.height;
            Texture2D blurredTexture = new Texture2D(width, height);

            float[] kernel = GenerateGaussianKernel(radius);
            int kernelSize = kernel.Length;

            // Apply blur horizontally
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color blurredColor = new Color(0, 0, 0);
                    float weightSum = 0f;

                    for (int k = -kernelSize / 2; k <= kernelSize / 2; k++)
                    {
                        int sampleX = Mathf.Clamp(x + k, 0, width - 1);
                        Color sampleColor = texture.GetPixel(sampleX, y);
                        float weight = kernel[k + kernelSize / 2];
                        blurredColor += sampleColor * weight;
                        weightSum += weight;
                    }

                    blurredTexture.SetPixel(x, y, blurredColor / weightSum);
                }
            }

            blurredTexture.Apply();

            // Apply blur vertically
            Texture2D finalBlurredTexture = new Texture2D(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color blurredColor = new Color(0, 0, 0);
                    float weightSum = 0f;

                    for (int k = -kernelSize / 2; k <= kernelSize / 2; k++)
                    {
                        int sampleY = Mathf.Clamp(y + k, 0, height - 1);
                        Color sampleColor = blurredTexture.GetPixel(x, sampleY);
                        float weight = kernel[k + kernelSize / 2];
                        blurredColor += sampleColor * weight;
                        weightSum += weight;
                    }

                    finalBlurredTexture.SetPixel(x, y, blurredColor / weightSum);
                }
            }

            finalBlurredTexture.Apply();
            return finalBlurredTexture;
        }

        float[] GenerateGaussianKernel(float radius)
        {
            int size = Mathf.CeilToInt(radius * 3f) * 2 + 1; // Kernel size based on radius
            float[] kernel = new float[size];
            float sigma = radius / 2f;
            float sum = 0f;
            int halfSize = size / 2;

            for (int i = 0; i < size; i++)
            {
                float x = i - halfSize;
                kernel[i] = Mathf.Exp(-x * x / (2 * sigma * sigma)) / (Mathf.Sqrt(2 * Mathf.PI) * sigma);
                sum += kernel[i];
            }

            // Normalize the kernel
            for (int i = 0; i < size; i++)
            {
                kernel[i] /= sum;
            }

            return kernel;
        }

        Mesh GenerateIslandMesh(Texture2D texture, float width, float height, int resolution)
        {
            int texWidth = texture.width;
            int texHeight = texture.height;

            // Create a grid mesh with the specified resolution
            Vector3[] vertices = new Vector3[resolution * resolution];
            Vector2[] uvs = new Vector2[resolution * resolution];
            int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];

            float scaleX = width / (resolution - 1);
            float scaleZ = width / (resolution - 1);

            int vertexIndex = 0;
            int triangleIndex = 0;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    // World position
                    float posX = x * scaleX - width / 2;
                    float posZ = y * scaleZ - width / 2;

                    // Sample height from the blurred texture
                    float texU = (float)x / resolution * texWidth;
                    float texV = (float)y / resolution * texHeight;
                    float pixelHeight = texture.GetPixelBilinear(texU / texWidth, texV / texHeight).grayscale;

                    float posY = pixelHeight * height;

                    // Set vertex position and UVs
                    vertices[vertexIndex] = new Vector3(posX, posY, posZ);
                    uvs[vertexIndex] = new Vector2((float)x / resolution, (float)y / resolution);

                    // Create triangles if not on the last row/column
                    if (x < resolution - 1 && y < resolution - 1)
                    {
                        int a = vertexIndex;
                        int b = vertexIndex + resolution;
                        int c = vertexIndex + resolution + 1;
                        int d = vertexIndex + 1;

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
            Mesh mesh = new Mesh();
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
