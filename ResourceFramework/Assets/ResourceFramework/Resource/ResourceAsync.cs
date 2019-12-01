using System;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ResourceFramework
{
    internal class ResourceAsync : AResourceAsync
    {
        public override bool keepWaiting => !done;

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
        /// 加载资源
        /// </summary>
        internal override void LoadAsset()
        {
            if (bundle == null)
                throw new Exception($"{nameof(Resource)}.{nameof(LoadAsset)}() {nameof(bundle)} is null.");

            asset = bundle.LoadAsset(url);
            done = true;
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

            bundle.ReduceReference();
            bundle = null;
            awaiter = null;
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

            LoadAsset();

            return true;
        }
    }
}