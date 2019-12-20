using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ResourceFramework
{
    internal class EditorResource : AResource
    {
        public override bool keepWaiting => !done;

        /// <summary>
        /// 加载资源
        /// </summary>
        internal override void Load()
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException($"{nameof(EditorResource)}.{nameof(Load)}() {nameof(url)} is null.");

            LoadAsset();
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        internal override void LoadAsset()
        {
#if UNITY_EDITOR
            asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(url);
#endif
            done = true;

            if (finishedCallback != null)
            {
                Action<AResource> tempCallback = finishedCallback;
                finishedCallback = null;
                tempCallback.Invoke(this);
            }
        }

        public override T GetAsset<T>()
        {
            Object tempAsset = asset;
            Type type = typeof(T);
            if (type == typeof(Sprite))
            {
                if (asset is Sprite)
                {
                    return tempAsset as T;
                }
                else
                {
#if UNITY_EDITOR
                    if (tempAsset && !(tempAsset is GameObject))
                    {
                        Resources.UnloadAsset(tempAsset);
                    }

                    asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(url);
#endif
                    return asset as T;
                }
            }
            else
            {
                return tempAsset as T;
            }
        }

        /// <summary>
        /// 卸载资源
        /// </summary>
        internal override void UnLoad()
        {
            if (asset != null && !(asset is GameObject))
            {
                Resources.UnloadAsset(base.asset);
                asset = null;
            }

            asset = null;
            awaiter = null;
            finishedCallback = null;
        }
    }
}