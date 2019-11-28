using System;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ResourceFramework
{
    public class EditorResource : AResource
    {
        /// <summary>
        /// 加载资源
        /// </summary>
        internal override void Load()
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException($"{nameof(EditorResource)}.{nameof(Load)}() {nameof(url)} is null.");

            loadTask = new TaskCompletionSource<AResource>();
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
            loadTask.SetResult(this);
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
        }
    }
}