using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ResourceFramework
{
    internal class ResourceAsync : AResourceAsync
    {
        public override bool keepWaiting => !done;

        /// <summary>
        /// 异步加载资源的AssetBundleRequest
        /// </summary>
        private AssetBundleRequest m_AssetBundleRequest;

        public override Object asset
        {
            get
            {
                if (done)
                    return base.asset;

                //正在异步加载的资源要变成同步
                FreshAsyncAsset();

                return base.asset;
            }

            protected set
            {
                base.asset = value;
            }
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        internal override void Load()
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException($"{nameof(Resource)}.{nameof(Load)}() {nameof(url)} is null.");

            if (bundle != null)
                throw new Exception($"{nameof(Resource)}.{nameof(Load)}() {nameof(bundle)} not null.");

            string bundleUrl = null;
            if (!ResourceManager.instance.ResourceBunldeDic.TryGetValue(url, out bundleUrl))
                throw new Exception($"{nameof(ResourceAsync)}.{nameof(Load)}() {nameof(bundleUrl)} is null.");

            bundle = BundleManager.instance.LoadAsync(bundleUrl);
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        internal override void LoadAssetAsync()
        {
            if (bundle == null)
                throw new Exception($"{nameof(ResourceAsync)}.{nameof(LoadAssetAsync)}() {nameof(bundle)} is null.");

            m_AssetBundleRequest = bundle.LoadAssetAsync(url, typeof(Object));
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        internal override void LoadAsset()
        {
            if (bundle == null)
                throw new Exception($"{nameof(ResourceAsync)}.{nameof(LoadAsset)}() {nameof(bundle)} is null.");

            if (m_AssetBundleRequest != null)
            {
                asset = m_AssetBundleRequest.asset;
            }
            else
            {
                asset = bundle.LoadAsset(url, typeof(Object));
            }

            done = true;

            if (finishedCallback != null)
            {
                Action<AResource> tempCallback = finishedCallback;
                finishedCallback = null;
                tempCallback.Invoke(this);
            }
        }

        /// <summary>
        /// 卸载资源
        /// </summary>
        internal override void UnLoad()
        {
            if (bundle == null)
                throw new Exception($"{ nameof(Resource)}.{nameof(UnLoad)}() {nameof(bundle)} is null.");

            if (base.asset != null && !(base.asset is GameObject))
            {
                Resources.UnloadAsset(base.asset);
                asset = null;
            }

            m_AssetBundleRequest = null;
            BundleManager.instance.UnLoad(bundle);
            bundle = null;
            awaiter = null;
            finishedCallback = null;
        }

        public override bool Update()
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

            if (!bundle.done)
                return false;

            if (m_AssetBundleRequest == null)
            {
                LoadAssetAsync();
            }

            if (m_AssetBundleRequest != null && !m_AssetBundleRequest.isDone)
                return false;

            LoadAsset();

            return true;
        }
    }
}