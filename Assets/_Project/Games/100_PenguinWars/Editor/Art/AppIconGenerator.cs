using System.IO;
using PenguinWars.Art;
using PenguinWars.Battle;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace PenguinWars.Editor
{
    /// <summary>
    /// アプリアイコン（仕様書 §7.4）: 空と雪の上に、かぶとと剣・盾の味方ペンギンを大きく描く。
    /// キャラと同じ合成でドット絵を作り、外部素材なしで Rebuild のたびに同じ絵を作り直せるようにする
    /// </summary>
    public static class AppIconGenerator
    {
        public const string IconPath = PenguinSpriteWriter.SpriteDirectory + "/AppIcon.png";
        // Android のアダプティブアイコン用の2層（背景は全面、前景は透明の上にペンギンだけ）
        public const string AdaptiveBackgroundPath = PenguinSpriteWriter.SpriteDirectory + "/AppIconBackground.png";
        public const string AdaptiveForegroundPath = PenguinSpriteWriter.SpriteDirectory + "/AppIconForeground.png";

        private const int CanvasSize = 64;
        // 32ドットのキャラを2倍にしてアイコンの枠いっぱいに描く。小さく表示されても形が分かるように
        private const int PenguinScale = CanvasSize / PenguinFrameComposer.CanvasSize;
        // 64 → 1024px。各ストアが求める一番大きいサイズに、ドットをぼかさず拡大する
        private const int OutputScale = 16;
        private const int GroundHeight = 10;

        // アダプティブは端末が外周を切り抜き、中央 66/108 の円だけが必ず見える。
        // キャラを等倍で中央に置くとその円にほぼ収まり、地面も見える範囲まで上げる
        private const int AdaptivePenguinScale = 1;
        private const int AdaptivePenguinLeft = (CanvasSize - PenguinFrameComposer.CanvasSize) / 2;
        private const int AdaptiveGroundHeight = AdaptivePenguinLeft + 2;

        private const int AndroidBackgroundLayer = 0;
        private const int AndroidForegroundLayer = 1;

        private static readonly PenguinLook IconLook = new PenguinLook(head: "helmet", hand: "sword_shield");

        private static readonly Color32 SkyTop = new Color32(90, 150, 225, 255);
        private static readonly Color32 SkyHorizon = new Color32(215, 235, 250, 255);
        private static readonly Color32 SnowTop = new Color32(248, 252, 255, 255);
        private static readonly Color32 SnowShade = new Color32(190, 215, 240, 255);

        /// <summary>SceneBuilder から呼ぶ。PNG を書いて、全プラットフォーム共通のアイコンと Android のアイコンに設定する</summary>
        public static void Generate()
        {
            Texture2D icon = WriteIcon(IconPath, BuildIcon());
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

            Texture2D background = WriteIcon(AdaptiveBackgroundPath, BuildAdaptiveBackground());
            Texture2D foreground = WriteIcon(AdaptiveForegroundPath, BuildAdaptiveForeground());
            ApplyAndroidIcons(background, foreground);
        }

        /// <summary>
        /// 最低 API 26 の Android はアダプティブアイコンしか使わず、Default Icon は参照されないので、ここで2層を入れる
        /// </summary>
        private static void ApplyAndroidIcons(Texture2D background, Texture2D foreground)
        {
            foreach (PlatformIconKind kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android))
            {
                PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
                foreach (PlatformIcon platformIcon in icons)
                {
                    platformIcon.SetTexture(background, AndroidBackgroundLayer);
                    platformIcon.SetTexture(foreground, AndroidForegroundLayer);
                }
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
            }
        }

        private static Color32[] BuildIcon()
        {
            var pixels = new Color32[CanvasSize * CanvasSize];
            DrawBackground(pixels, GroundHeight);
            DrawPenguin(pixels, PenguinScale, 0, GroundHeight);
            return pixels;
        }

        private static Color32[] BuildAdaptiveBackground()
        {
            var pixels = new Color32[CanvasSize * CanvasSize];
            DrawBackground(pixels, AdaptiveGroundHeight);
            return pixels;
        }

        /// <summary>Color32 の既定値は透明なので、ペンギン以外は背景の層が透けて見える</summary>
        private static Color32[] BuildAdaptiveForeground()
        {
            var pixels = new Color32[CanvasSize * CanvasSize];
            DrawPenguin(pixels, AdaptivePenguinScale, AdaptivePenguinLeft, AdaptiveGroundHeight);
            return pixels;
        }

        private static void DrawBackground(Color32[] pixels, int groundHeight)
        {
            for (int y = 0; y < CanvasSize; y++)
            {
                Color32 sky = Color32.Lerp(SkyHorizon, SkyTop, (float)(y - groundHeight) / (CanvasSize - groundHeight));
                for (int x = 0; x < CanvasSize; x++)
                {
                    // 雪の縁は2ドット幅の影にして、ペンギンの足元と地面の境目を見せる
                    Color32 color = y < groundHeight - 2 ? SnowTop : y < groundHeight ? SnowShade : sky;
                    pixels[y * CanvasSize + x] = color;
                }
            }
        }

        /// <summary>足元を雪の上に合わせて、キャラのコマを拡大して重ねる（透明なドットは背景を残す）</summary>
        private static void DrawPenguin(Color32[] pixels, int scale, int left, int groundHeight)
        {
            int src = PenguinFrameComposer.CanvasSize;
            Color32[] penguin = PenguinFrameComposer.Compose(IconLook, Side.Left, PenguinFrame.Walk0);
            int footY = groundHeight - 2;
            for (int y = 0; y < src; y++)
            {
                for (int x = 0; x < src; x++)
                {
                    Color32 color = penguin[y * src + x];
                    if (color.a == 0) continue;
                    FillBlock(pixels, left + x * scale, footY + y * scale, scale, color);
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

        private static Texture2D WriteIcon(string path, Color32[] pixels)
        {
            WritePng(path, Upscale(pixels, CanvasSize, OutputScale), CanvasSize * OutputScale);
            ConfigureImporter(path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
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

        private static void WritePng(string path, Color32[] pixels, int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        /// <summary>各プラットフォームのサイズへ縮めるとき、ドットの境目をぼかさないようにする</summary>
        private static void ConfigureImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }
}
