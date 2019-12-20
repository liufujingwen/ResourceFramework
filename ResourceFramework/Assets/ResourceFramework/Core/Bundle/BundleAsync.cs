using System;
using System.IO;
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

            m_AssetBundleCreateRequest = AssetBundle.LoadFromFileAsync(file, 0, BundleManager.instance.offset);
        }

        /// <summary>
        /// 卸载bundle
        /// </summary>
        internal override void UnLoad()
        {
            if (assetBundle)
            {
                assetBundle.Unload(true);
            }
            else
            {
                //正在异步加载的资源也要切到主线程进行释放
                if (m_AssetBundleCreateRequest != null)
                {
                    assetBundle = m_AssetBundleCreateRequest.assetBundle;
                }

                if (assetBundle)
                {
                    assetBundle.Unload(true);
                }
            }

            m_AssetBundleCreateRequest = null;
            done = false;
            reference = 0;
            assetBundle = null;
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="name">资源名称</param>
        /// <param name="type">资源Type</param>
        /// <returns>AssetBundleRequest</returns>
        internal override AssetBundleRequest LoadAssetAsync(string name, Type type)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException($"{nameof(BundleAsync)}.{nameof(LoadAssetAsync)}() name is null.");

            if (m_AssetBundleCreateRequest == null)
                throw new NullReferenceException($"{nameof(BundleAsync)}.{nameof(LoadAssetAsync)}() m_AssetBundleCreateRequest is null.");

            if (assetBundle == null)
                assetBundle = m_AssetBundleCreateRequest.assetBundle;

            return assetBundle.LoadAssetAsync(name, type);
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <param name="name">资源名称</param>
        /// <param name="type">资源Type</param>
        /// <returns>指定名字的资源</returns>
        internal override Object LoadAsset(string name, Type type)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException($"{nameof(BundleAsync)}.{nameof(LoadAsset)}() name is null.");

            if (m_AssetBundleCreateRequest == null)
                throw new NullReferenceException($"{nameof(BundleAsync)}.{nameof(LoadAsset)}() m_AssetBundleCreateRequest is null.");

            if (assetBundle == null)
                assetBundle = m_AssetBundleCreateRequest.assetBundle;

            return assetBundle.LoadAsset(name, type);
        }

        internal override bool Update()
        {
            if (done)
                return true;


            if (dependencies != null)
            {
                for (int i = 0; i < dependencies.Length; i++)
                {
                    if (!dependencies[i].done)
                        return false;
                }
            }

            if (!m_AssetBundleCreateRequest.isDone)
                return false;

            done = true;

            assetBundle = m_AssetBundleCreateRequest.assetBundle;

            if (reference == 0)
            {
                UnLoad();
            }

            return true;
        }
    }
}