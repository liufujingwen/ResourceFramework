using System;

namespace ResourceFramework
{
    internal abstract class AResourceAsync : AResource
    {
        public abstract bool Update();

        /// <summary>
        /// 异步加载资源
        /// </summary>
        internal abstract void LoadAssetAsync();
    }
}