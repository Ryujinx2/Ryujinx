﻿using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.OpenGL.Image;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Graphics.OpenGL
{
    class DisposedTexture
    {
        public TextureCreateInfo Info;
        public TextureView View;
        public float ScaleFactor;
        public int RemainingFrames;
    }

    /// <summary>
    /// A structure for caching resources that can be reused without recreation, such as textures.
    /// </summary>
    class ResourceCache
    {
        private const int DisposedCacheFrames = 2;

        private object _lock = new object();
        private Dictionary<uint, List<DisposedTexture>> _textures = new Dictionary<uint, List<DisposedTexture>>();

        private uint GetTextureKey(TextureCreateInfo info)
        {
            return ((uint)info.Width) | ((uint)info.Height << 16);
        }

        /// <summary>
        /// Add a texture that is not being used anymore to the resource cache to be used later.
        /// Both the texture's view and storage should be completely unused.
        /// </summary>
        /// <param name="view">The texture's view</param>
        public void AddTexture(TextureView view)
        {
            lock (_lock)
            {
                uint key = GetTextureKey(view.Info);

                List<DisposedTexture> list;
                if (!_textures.TryGetValue(key, out list))
                {
                    list = new List<DisposedTexture>();
                    _textures.Add(key, list);
                }

                list.Add(new DisposedTexture()
                {
                    Info = view.Info,
                    View = view,
                    ScaleFactor = view.ScaleFactor,
                    RemainingFrames = DisposedCacheFrames
                });
            }
        }

        /// <summary>
        /// Attempt to obtain a texture from the resource cache with the desired parameters.
        /// </summary>
        /// <param name="info">The creation info for the desired texture</param>
        /// <param name="scaleFactor">The scale factor for the desired texture</param>
        /// <returns>A TextureView with the description specified, or null if one was not found.</returns>
        public TextureView TryGetTexture(TextureCreateInfo info, float scaleFactor)
        {
            lock (_lock)
            {
                uint key = GetTextureKey(info);

                List<DisposedTexture> list;
                if (!_textures.TryGetValue(key, out list))
                {
                    return null;
                }

                foreach (DisposedTexture texture in list)
                {
                    if (texture.View.Info.Equals(info) && scaleFactor == texture.ScaleFactor)
                    {
                        list.Remove(texture);
                        return texture.View;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Update the cache, removing any resources that have expired.
        /// </summary>
        public void Tick()
        {
            lock (_lock)
            {
                foreach (List<DisposedTexture> list in _textures.Values)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        DisposedTexture tex = list[i];

                        if (--tex.RemainingFrames < 0)
                        {
                            tex.View.Dispose();
                            list.RemoveAt(i--);
                        }
                    }
                }
            }
        }
    }
}
