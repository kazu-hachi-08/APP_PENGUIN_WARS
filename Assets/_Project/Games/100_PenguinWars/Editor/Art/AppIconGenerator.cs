using System.IO;
using MiniGame.PenguinWars.Art;
using MiniGame.PenguinWars.Battle;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>
    /// アプリアイコン（仕様書 §7.4）: 空と雪の上に、かぶとと剣・盾の味方ペンギンを大きく描く。
    /// キャラと同じ合成でドット絵を作り、外部素材なしで Rebuild のたびに同じ絵を作り直せるようにする
    /// </summary>
    public static class AppIconGenerator
    {
        public const string IconPath = PenguinSpriteWriter.SpriteDirectory + "/AppIcon.png";

        private const int CanvasSize = 64;
        // 32ドットのキャラを2倍にしてアイコンの枠いっぱいに描く。小さく表示されても形が分かるように
        private const int PenguinScale = CanvasSize / PenguinFrameComposer.CanvasSize;
        // 64 → 1024px。各ストアが求める一番大きいサイズに、ドットをぼかさず拡大する
        private const int OutputScale = 16;
        private const int GroundHeight = 10;

        private static readonly PenguinLook IconLook = new PenguinLook(head: "helmet", hand: "sword_shield");

        private static readonly Color32 SkyTop = new Color32(90, 150, 225, 255);
        private static readonly Color32 SkyHorizon = new Color32(215, 235, 250, 255);
        private static readonly Color32 SnowTop = new Color32(248, 252, 255, 255);
        private static readonly Color32 SnowShade = new Color32(190, 215, 240, 255);

        /// <summary>SceneBuilder から呼ぶ。PNG を書いて、全プラットフォーム共通のアイコンに設定する</summary>
        public static void Generate()
        {
            Color32[] pixels = Upscale(BuildIcon(), CanvasSize, OutputScale);
            int size = CanvasSize * OutputScale;
            WritePng(pixels, size);
            ConfigureImporter();

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }

        private static Color32[] BuildIcon()
        {
            var pixels = new Color32[CanvasSize * CanvasSize];
            DrawBackground(pixels);
            DrawPenguin(pixels);
            return pixels;
        }

        private static void DrawBackground(Color32[] pixels)
        {
            for (int y = 0; y < CanvasSize; y++)
            {
                Color32 sky = Color32.Lerp(SkyHorizon, SkyTop, (float)(y - GroundHeight) / (CanvasSize - GroundHeight));
                for (int x = 0; x < CanvasSize; x++)
                {
                    // 雪の縁は2ドット幅の影にして、ペンギンの足元と地面の境目を見せる
                    Color32 color = y < GroundHeight - 2 ? SnowTop : y < GroundHeight ? SnowShade : sky;
                    pixels[y * CanvasSize + x] = color;
                }
            }
        }

        /// <summary>足元を雪の上に合わせて、キャラのコマを拡大して重ねる（透明なドットは背景を残す）</summary>
        private static void DrawPenguin(Color32[] pixels)
        {
            int src = PenguinFrameComposer.CanvasSize;
            Color32[] penguin = PenguinFrameComposer.Compose(IconLook, Side.Left, PenguinFrame.Walk0);
            int footY = GroundHeight - 2;
            for (int y = 0; y < src; y++)
            {
                for (int x = 0; x < src; x++)
                {
                    Color32 color = penguin[y * src + x];
                    if (color.a == 0) continue;
                    FillBlock(pixels, x * PenguinScale, footY + y * PenguinScale, PenguinScale, color);
                }
            }
        }

        private static void FillBlock(Color32[] pixels, int left, int bottom, int size, Color32 color)
        {
            for (int y = bottom; y < bottom + size && y < CanvasSize; y++)
            {
                for (int x = left; x < left + size && x < CanvasSize; x++) pixels[y * CanvasSize + x] = color;
            }
        }

        private static Color32[] Upscale(Color32[] source, int sourceSize, int scale)
        {
            int size = sourceSize * scale;
            var result = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++) result[y * size + x] = source[y / scale * sourceSize + x / scale];
            }
            return result;
        }

        private static void WritePng(Color32[] pixels, int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(IconPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceUpdate);
        }

        /// <summary>各プラットフォームのサイズへ縮めるとき、ドットの境目をぼかさないようにする</summary>
        private static void ConfigureImporter()
        {
            var importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }
}
