using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ResourceFramework
{
    internal abstract class ABundleAsync : ABundle
    {
        internal abstract bool Update();

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="name">资源名称</param>
        /// <param name="type">资源Type</param>
        /// <returns>AssetBundleRequest</returns>
        internal abstract AssetBundleRequest LoadAssetAsync(string name, Type type);
    }
}