﻿using System.Diagnostics;
using SkiaSharp;

namespace z1.IO;

[DebuggerDisplay("{Filename}")]
internal readonly struct Asset
{
    // The assets are so small that we can buffer them all into memory without an issue.
    private static readonly Dictionary<string, byte[]> _assets = new(128);

    public string Filename { get; } // Good for debug purposes only.

    private readonly byte[] _assetData;

    public Asset(string filename)
    {
        Filename = filename;
        if (!_assets.TryGetValue(filename, out _assetData!))
        {
            throw new Exception($"Unable to find asset {filename}");
        }
    }

    public static void Initialize()
    {
        foreach (var kv in AssetLoader.Initialize())
        {
            _assets.Add(kv.Key, kv.Value);
        }
    }

    public byte[] ReadAllBytes() => _assetData;
    public MemoryStream GetStream() => new(_assetData);
    public SKBitmap DecodeSKBitmap() => SKBitmap.Decode(_assetData);

    public SKBitmap DecodeSKBitmapTileData()
    {
        var bitmap = DecodeSKBitmap(SKAlphaType.Unpremul);
        Graphics.PreprocessPalette(bitmap);
        return bitmap;
    }

    public SKBitmap DecodeSKBitmap(SKAlphaType alphaType)
    {
        using var original = SKBitmap.Decode(_assetData);
        var bitmap = new SKBitmap(original.Width, original.Height, original.ColorType, alphaType);
        using var canvas = new SKCanvas(bitmap);
        canvas.DrawBitmap(original, 0, 0);
        return bitmap;
    }
}