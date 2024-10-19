using System.Numerics;
using Silk.NET.OpenGL;
using SkiaSharp;
using z1.IO;

namespace z1.Render;

internal sealed unsafe class GLImage : IDisposable
{
    private readonly GL _gl;
    private readonly uint _texture;
    private readonly Size _size;

    public GLImage(GL gl, Asset bitmap) : this(gl, bitmap.DecodeSKBitmapTileData())
    {
    }

    public GLImage(GL gl, SKBitmap bitmap)
    {
        _gl = gl;
        _texture = gl.GenTexture();
        _size = new Size(bitmap.Width, bitmap.Height);
        LoadTextureData(bitmap);
    }

    private void LoadTextureData(SKBitmap bitmap)
    {
        if (bitmap.BytesPerPixel != 4) throw new ArgumentException("Bitmap must be 4 bytes per pixel.", nameof(bitmap));

        var bitmapLock = bitmap.Lock();

        _gl.BindTexture(TextureTarget.Texture2D, _texture);

        _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8,
            (uint)bitmap.Width, (uint)bitmap.Height, 0,
            PixelFormat.Bgra, PixelType.UnsignedByte, (void*)bitmapLock.Data);

        _gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        _gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
    }

    public void Draw(
        int srcX, int srcY, int width, int height,
        int destX, int destY,
        ReadOnlySpan<SKColor> palette, DrawingFlags flags)
    {
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _texture);

        var right = srcX + width;
        var bottom = srcY + height;

        // Branchless conditional swaps:
        // https://github.com/jlennox/Benchmarks/blob/main/BranchlessSwap.cs
        var flipHor = -BitOperations.PopCount((uint)(flags & DrawingFlags.FlipHorizontal));
        var flipVert = -BitOperations.PopCount((uint)(flags & DrawingFlags.FlipVertical));

        var tempSrcX = (flipHor & right) | (~flipHor & srcX);
        var tempRight = (flipHor & srcX) | (~flipHor & right);
        srcX = tempSrcX;
        right = tempRight;

        var tempSrcY = (flipVert & bottom) | (~flipVert & srcY);
        var tempBottom = (flipVert & srcY) | (~flipVert & bottom);
        srcY = tempSrcY;
        bottom = tempBottom;

        // Really, all the rendering should be batched together. But Zelda is simple enough
        // that it's not really a performance concern.
        ReadOnlySpan<Point> verticies = stackalloc Point[] {
            new Point(srcX, srcY), new Point(destX, destY),
            new Point(srcX, bottom), new Point(destX, destY + height),
            new Point(right, srcY), new Point(destX + width, destY),
            new Point(right, bottom), new Point(destX + width, destY + height),
        };

        var finalPalette = palette;

        if (!flags.HasFlag(DrawingFlags.NoTransparency))
        {
            finalPalette = stackalloc SKColor[] {
                new SKColor(0),
                palette[1],
                palette[2],
                palette[3],
            };
        }

        _gl.BufferData(
            BufferTargetARB.ArrayBuffer, (nuint)verticies.Length * (nuint)sizeof(Point),
            verticies, BufferUsageARB.StreamDraw);

        var shader = GLSpriteShader.Instance;
        shader.Use();
        shader.SetTextureSize(_size.Width, _size.Height);
        shader.SetOpacity(1f);
        shader.SetPalette(finalPalette);
        _gl.DrawArrays(PrimitiveType.TriangleStrip, 0, (uint)verticies.Length / 2);
    }

    public void Dispose()
    {
        _gl.DeleteTexture(_texture);
    }
}

internal sealed class GLVertexArray : IDisposable
{
    public static GLVertexArray Instance { get; private set; } = null!;

    private readonly GL _gl;
    private readonly uint _vertexArrayObject;
    private readonly uint _vertextBufferObject;

    private unsafe GLVertexArray(GL gl)
    {
        _gl = gl;
        _vertexArrayObject = gl.GenVertexArray();
        gl.BindVertexArray(_vertexArrayObject);

        _vertextBufferObject = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertextBufferObject);

        gl.EnableVertexAttribArray(0);
        gl.EnableVertexAttribArray(1);
        gl.VertexAttribIPointer(0, 2, VertexAttribIType.Int, (uint)sizeof(Point) * 2, null);
        gl.VertexAttribIPointer(1, 2, VertexAttribIType.Int, (uint)sizeof(Point) * 2, sizeof(Point));

        gl.Disable(EnableCap.DepthTest);
        gl.Enable(EnableCap.Blend);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    }

    public static void Initialize(GL gl)
    {
        Instance = new GLVertexArray(gl);
    }

    public void Dispose()
    {
        _gl.DeleteVertexArray(_vertexArrayObject);
        _gl.DeleteBuffer(_vertextBufferObject);
    }
}