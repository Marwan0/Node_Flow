using UnityEngine;
using UnityEditor;
using System.IO;
using Ntreev.Library.Psd;

namespace Object_Flow.PsdToUI.Editor
{
    public static class PsdTextureExporter
    {
        public static Sprite ExportSprite(IPsdLayer layer, string exportFolderPath)
        {
            if (!layer.HasImage || layer.Width <= 0 || layer.Height <= 0)
                return null;

            // Ensure directory exists
            if (!Directory.Exists(exportFolderPath))
            {
                Directory.CreateDirectory(exportFolderPath);
            }

            // Extract channels byte arrays
            byte[] red = null, green = null, blue = null, alpha = null;

            foreach (var channel in layer.Channels)
            {
                if (channel.Type == ChannelType.Red) red = channel.Data;
                else if (channel.Type == ChannelType.Green) green = channel.Data;
                else if (channel.Type == ChannelType.Blue) blue = channel.Data;
                else if (channel.Type == ChannelType.Alpha) alpha = channel.Data;
            }

            // Generate pixels array
            int width = layer.Width;
            int height = layer.Height;
            Color32[] pixels = new Color32[width * height];
            int bitsPerChannel = layer.Depth;

            for (int i = 0; i < pixels.Length; i++)
            {
                byte r = SampleChannelByte(red, i, bitsPerChannel);
                byte g = SampleChannelByte(green, i, bitsPerChannel);
                byte b = SampleChannelByte(blue, i, bitsPerChannel);
                // If there's no alpha channel, it's fully opaque. 
                // Wait, some layers might have alpha=255 as default. 
                byte a = alpha != null ? SampleChannelByte(alpha, i, bitsPerChannel) : (byte)255; 

                pixels[i] = new Color32(r, g, b, a);
            }

            // Flip Y-axis since Unity textures are bottom-to-top and PSD is top-to-bottom
            Color32[] flippedPixels = new Color32[pixels.Length];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    flippedPixels[(height - 1 - y) * width + x] = pixels[y * width + x];
                }
            }

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(flippedPixels);
            texture.Apply();

            byte[] pngData = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);

            string safeLayerName = GetSafeFilename(layer.Name);
            string fileGuid = System.Guid.NewGuid().ToString().Substring(0, 5); // Add pseudo-uniqueness to avoid overwrites
            string filePath = Path.Combine(exportFolderPath, $"{safeLayerName}_{fileGuid}.png");

            File.WriteAllBytes(filePath, pngData);
            AssetDatabase.Refresh();

            return ConfigureTextureAsSprite(filePath);
        }

        private static Sprite ConfigureTextureAsSprite(string assetPath)
        {
            // The path must be relative to project for AssetDatabase
            string projectPath = assetPath.Replace("\\", "/");
            if (projectPath.StartsWith(Application.dataPath))
            {
                projectPath = "Assets" + projectPath.Substring(Application.dataPath.Length);
            }

            TextureImporter importer = AssetImporter.GetAtPath(projectPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(projectPath);
        }

        private static string GetSafeFilename(string filename)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                filename = filename.Replace(c.ToString(), "_");
            }
            return filename;
        }

        private static byte SampleChannelByte(byte[] data, int pixelIndex, int bitsPerChannel)
        {
            if (data == null)
            {
                return 0;
            }

            switch (bitsPerChannel)
            {
                case 8:
                    return pixelIndex < data.Length ? data[pixelIndex] : (byte)0;
                case 16:
                    {
                        int byteIndex = pixelIndex * 2;
                        return byteIndex < data.Length ? data[byteIndex] : (byte)0;
                    }
                case 32:
                    {
                        int byteIndex = pixelIndex * 4;
                        if (byteIndex + 3 >= data.Length)
                        {
                            return 0;
                        }

                        float value;
                        if (System.BitConverter.IsLittleEndian)
                        {
                            byte[] bytes = new byte[4]
                            {
                                data[byteIndex + 3],
                                data[byteIndex + 2],
                                data[byteIndex + 1],
                                data[byteIndex]
                            };
                            value = System.BitConverter.ToSingle(bytes, 0);
                        }
                        else
                        {
                            value = System.BitConverter.ToSingle(data, byteIndex);
                        }

                        value = Mathf.Clamp01(value);
                        return (byte)Mathf.RoundToInt(value * 255f);
                    }
                default:
                    return pixelIndex < data.Length ? data[pixelIndex] : (byte)0;
            }
        }
    }
}
