using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ResourceFramework
{
    internal class Bundle : ABundle
    {
        /// <summary>
        /// 加载AssetBundle
        /// </summary>
        internal override void Load()
        {
            if (assetBundle)
            {
                throw new Exception($"{nameof(Bundle)}.{nameof(Load)}() {nameof(assetBundle)} not null , Url:{url}.");
            }

            string file = BundleManager.instance.GetFileUrl(url);

#if UNITY_EDITOR || UNITY_STANDALONE
            if (!File.Exists(file))
            {
                throw new FileNotFoundException($"{nameof(Bundle)}.{nameof(Load)}() {nameof(file)} not exist, file:{file}.");
            }
#endif

            assetBundle = AssetBundle.LoadFromFile(file, 0, BundleManager.instance.offset);

            done = true;
        }

        /// <summary>
        /// 卸载bundle
        /// </summary>
        internal override void UnLoad()
        {
            if (assetBundle)
            {
                done = false;
                assetBundle.Unload(true);
                assetBundle = null;
            }
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <param name="name">资源名称</param>
        /// <returns>指定名字的资源</returns>
        internal override Object LoadAsset(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException($"{nameof(Bundle)}.{nameof(LoadAsset)}() name is null.");

            if (assetBundle == null)
                throw new NullReferenceException($"{nameof(Bundle)}.{nameof(LoadAsset)}() Bundle is null.");

            return assetBundle.LoadAsset(name);
        }
    }
}