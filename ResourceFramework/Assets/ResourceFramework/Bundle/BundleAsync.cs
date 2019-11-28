using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ResourceFramework
{
    /// <summary>
    /// 异步加载的bundle
    /// </summary>
    internal class BundleAsync : ABundleAsync
    {
        /// <summary>
        /// 异步bundle的AssetBundleCreateRequest
        /// </summary>
        private AssetBundleCreateRequest m_AssetBundleCreateRequest;

        /// <summary>
        /// 加载AssetBundle
        /// </summary>
        internal override void Load()
        {
            if (m_AssetBundleCreateRequest != null)
            {
                throw new Exception($"{nameof(BundleAsync)}.{nameof(Load)}() {nameof(m_AssetBundleCreateRequest)} not null, {this}.");
            }

            string file = BundleManager.instance.GetFileUrl(url);

#if UNITY_EDITOR || UNITY_STANDALONE
            if (!File.Exists(file))
            {
                throw new FileNotFoundException($"{nameof(BundleAsync)}.{nameof(Load)}() {nameof(file)} not exist, file:{file}.");
            }
#endif

            m_AssetBundleCreateRequest = AssetBundle.LoadFromFileAsync(url, 0, BundleManager.instance.offset);

            loadBundleTask = new TaskCompletionSource<bool>();
        }

        /// <summary>
        /// 卸载bundle
        /// </summary>
        internal override void UnLoad()
        {
            if (assetBundle)
            {
                m_AssetBundleCreateRequest = null;
                done = false;
                assetBundle.Unload(true);
                assetBundle = null;
                url = null;
                reference = 0;
                loadBundleTask = null;
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
                throw new ArgumentException($"{nameof(BundleAsync)}.{nameof(LoadAsset)}() name is null.");

            if (m_AssetBundleCreateRequest == null)
                throw new NullReferenceException($"{nameof(BundleAsync)}.{nameof(LoadAsset)}() m_AssetBundleCreateRequest is null.");

            if (assetBundle == null)
                assetBundle = m_AssetBundleCreateRequest.assetBundle;

            return assetBundle.LoadAsset(name);
        }

        internal override bool Update()
        {
            if (done)
                return true;

            if (!m_AssetBundleCreateRequest.isDone)
                return false;

            done = true;
            assetBundle = m_AssetBundleCreateRequest.assetBundle;
            loadBundleTask.SetResult(true);

            if (reference == 0)
            {
                UnLoad();
            }

            return true;
        }
    }
}