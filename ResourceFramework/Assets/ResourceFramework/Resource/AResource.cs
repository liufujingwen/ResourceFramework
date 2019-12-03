using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ResourceFramework
{
    internal abstract class AResource : CustomYieldInstruction, IResource
    {
        /// <summary>
        /// Asset对应的Url
        /// </summary>
        internal string url { get; set; }

        /// <summary>
        /// 加载完成的资源
        /// </summary>
        public virtual Object asset { get; protected set; }

        /// <summary>
        /// 引用的Bundle
        /// </summary>
        internal ABundle bundle { get; set; }

        /// <summary>
        /// 依赖资源
        /// </summary>
        internal AResource[] dependencies { get; set; }

        /// <summary>
        /// 引用计数器
        /// </summary>
        internal int reference { get; set; }

        //是否加载完成
        internal bool done { get; set; }

        /// <summary>
        /// awaiter
        /// </summary>
        internal ResourceAwaiter awaiter { get; set; }

        /// <summary>
        /// 加载完成回调
        /// </summary>
        internal Action<AResource> finishedCallback { get; set; }

        string IResource.url
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        internal abstract void Load();

        /// <summary>
        /// 卸载资源
        /// </summary>
        internal abstract void UnLoad();

        /// <summary>
        /// 加载资源
        /// </summary>
        internal abstract void LoadAsset();

        /// <summary>
        /// 刷新异步资源（当同步资源的依赖包含异步时，需要立即刷新返回）
        /// </summary>
        internal void FreshAsyncAsset()
        {
            if (done)
                return;

            if (dependencies != null)
            {
                for (int i = 0; i < dependencies.Length; i++)
                {
                    AResource resource = dependencies[i];
                    resource.FreshAsyncAsset();
                }
            }

            if (this is AResourceAsync)
            {
                LoadAsset();
            }
        }

        /// <summary>
        /// 增加引用
        /// </summary>
        internal void AddReference()
        {
            ++reference;
        }

        /// <summary>
        /// 减少引用
        /// </summary>
        internal void ReduceReference()
        {
            --reference;

            if (reference < 0)
            {
                throw new Exception($"{GetType()}.{nameof(ReduceReference)}() less than 0,{nameof(url)}:{url}.");
            }
        }

        public Object GetAsset()
        {
            return asset;
        }

        public T GetAsset<T>() where T : Object
        {
            return asset as T;
        }

        public GameObject Instantiate()
        {
            Object obj = asset;

            if (!obj)
                return null;

            if (!(obj is GameObject))
                return null;

            return Object.Instantiate(obj) as GameObject;
        }

        public GameObject Instantiate(Vector3 position, Quaternion rotation)
        {
            Object obj = asset;

            if (!obj)
                return null;

            if (!(obj is GameObject))
                return null;

            return Object.Instantiate(obj, position, rotation) as GameObject;
        }

        public GameObject Instantiate(Transform parent, bool instantiateInWorldSpace)
        {
            Object obj = asset;

            if (!obj)
                return null;

            if (!(obj is GameObject))
                return null;

            return Object.Instantiate(obj, parent, instantiateInWorldSpace) as GameObject;
        }


    }
}