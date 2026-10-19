using System;
using System.Drawing;
using OpenTK.Graphics.ES20;

namespace MissionPlanner.Controls.Scene3D
{
    /// <summary>
    /// Texture creation shared by terrain, markers and text.
    /// </summary>
    public static class GLTextures
    {
        /// <summary>GL_BGRA. Desktop GL accepts it as an upload format, which skips swapping red and blue on the CPU.</summary>
        const PixelFormat Bgra = (PixelFormat)0x80E1;

        const TextureParameterName MaxAnisotropy = (TextureParameterName)0x84FE;
        const GetPName MaxAnisotropyLimit = (GetPName)0x84FF;

        static bool? _bgraSupported;
        static float _anisotropy = -1;

        public static int CreateFromBgra(byte[] pixels, int width, int height, bool mipmaps)
        {
            int texture;
            GL.GenTextures(1, out texture);
            if (texture == 0)
                return 0;

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);

            if (_bgraSupported != false)
            {
                DrainErrors();
                GL.TexImage2D(TextureTarget2d.Texture2D, 0, TextureComponentCount.Rgba, width, height, 0, Bgra,
                    PixelType.UnsignedByte, pixels);
                if (_bgraSupported == null)
                    _bgraSupported = GL.GetError() == ErrorCode.NoError;
            }

            if (_bgraSupported == false)
            {
                var rgba = new byte[pixels.Length];
                for (int i = 0; i < pixels.Length; i += 4)
                {
                    rgba[i] = pixels[i + 2];
                    rgba[i + 1] = pixels[i + 1];
                    rgba[i + 2] = pixels[i];
                    rgba[i + 3] = pixels[i + 3];
                }

                GL.TexImage2D(TextureTarget2d.Texture2D, 0, TextureComponentCount.Rgba, width, height, 0,
                    PixelFormat.Rgba, PixelType.UnsignedByte, rgba);
            }

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            if (mipmaps)
            {
                GL.GenerateMipmap(TextureTarget.Texture2D);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                    (int)TextureMinFilter.LinearMipmapLinear);
                float anisotropy = Anisotropy();
                if (anisotropy > 1)
                    GL.TexParameter(TextureTarget.Texture2D, MaxAnisotropy, anisotropy);
            }
            else
            {
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            }

            return texture;
        }

        public static int CreateFromImage(Image image, bool mipmaps)
        {
            int width, height;
            var pixels = GMapImagerySource.ToBgra(image, out width, out height);
            return CreateFromBgra(pixels, width, height, mipmaps);
        }

        public static void Delete(ref int texture)
        {
            if (texture != 0)
            {
                GL.DeleteTextures(1, ref texture);
                texture = 0;
            }
        }

        /// <summary>Anisotropic filtering keeps terrain sharp at the oblique angles a ground station mostly looks at.</summary>
        static float Anisotropy()
        {
            if (_anisotropy >= 0)
                return _anisotropy;

            _anisotropy = 0;
            try
            {
                var extensions = GL.GetString(StringName.Extensions) ?? "";
                if (extensions.Contains("GL_EXT_texture_filter_anisotropic") ||
                    extensions.Contains("GL_ARB_texture_filter_anisotropic"))
                {
                    float max;
                    GL.GetFloat(MaxAnisotropyLimit, out max);
                    _anisotropy = Math.Min(8f, max);
                }
            }
            catch
            {
            }

            DrainErrors();
            return _anisotropy;
        }

        static void DrainErrors()
        {
            for (int i = 0; i < 16 && GL.GetError() != ErrorCode.NoError; i++)
            {
            }
        }
    }

    /// <summary>
    /// A vertex buffer refilled every frame for small dynamic geometry (lines, markers, overlays).
    /// </summary>
    public sealed class StreamBuffer : IDisposable
    {
        int _id;

        public void Upload(float[] data, int floatCount)
        {
            if (_id == 0)
                GL.GenBuffers(1, out _id);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _id);
            GL.BufferData(BufferTarget.ArrayBuffer, floatCount * sizeof(float), data, BufferUsageHint.StreamDraw);
        }

        public void Dispose()
        {
            if (_id != 0)
            {
                GL.DeleteBuffers(1, ref _id);
                _id = 0;
            }
        }
    }

    /// <summary>
    /// A growable float array reused across frames so dynamic geometry does not allocate per frame.
    /// </summary>
    public sealed class VertexList
    {
        float[] _data = new float[1024];

        public int Count { get; private set; }

        public float[] Data
        {
            get { return _data; }
        }

        public void Clear()
        {
            Count = 0;
        }

        public void Add(float a, float b, float c)
        {
            Ensure(3);
            _data[Count++] = a;
            _data[Count++] = b;
            _data[Count++] = c;
        }

        public void Add(float a, float b)
        {
            Ensure(2);
            _data[Count++] = a;
            _data[Count++] = b;
        }

        public void Add(float a)
        {
            Ensure(1);
            _data[Count++] = a;
        }

        /// <summary>Position plus RGBA colour: the layout the colour shader expects.</summary>
        public void AddColored(double x, double y, double z, float r, float g, float b, float a)
        {
            Ensure(7);
            _data[Count++] = (float)x;
            _data[Count++] = (float)y;
            _data[Count++] = (float)z;
            _data[Count++] = r;
            _data[Count++] = g;
            _data[Count++] = b;
            _data[Count++] = a;
        }

        void Ensure(int extra)
        {
            if (Count + extra > _data.Length)
                Array.Resize(ref _data, Math.Max(_data.Length * 2, Count + extra));
        }
    }
}
