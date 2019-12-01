using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ResourceFramework
{
    public class ResourceManager
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static ResourceManager instance { get; } = new ResourceManager();

        private const string MANIFEST_BUNDLE = "manifest.ab";
        private const string RESOURCE_ASSET_NAME = "Assets/Temp/Resource.bytes";
        private const string BUNDLE_ASSET_NAME = "Assets/Temp/Bundle.bytes";
        private const string DEPENDENCY_ASSET_NAME = "Assets/Temp/Dependency.bytes";

        /// <summary>
        ///   保存资源对应的bundle
        /// </summary>
        internal Dictionary<string, string> ResourceBunldeDic = new Dictionary<string, string>();

        /// <summary>
        /// 保存资源的依赖关系
        /// </summary>
        internal Dictionary<string, List<string>> ResourceDependencyDic = new Dictionary<string, List<string>>();

        /// <summary>
        /// 所有资源集合
        /// </summary>
        private Dictionary<string, AResource> m_ResourceDic = new Dictionary<string, AResource>();

        /// <summary>
        /// 异步加载集合
        /// </summary>
        private List<AResourceAsync> m_AsyncList = new List<AResourceAsync>();

        /// <summary>
        /// 需要释放的资源
        /// </summary>
        private List<AResource> m_NeedUnloadList = new List<AResource>();

        /// <summary>
        /// 使用AssetDataBase进行加载
        /// </summary>
        private bool m_Editor = false;

        /// <summary>
        /// 当前游戏运行时间，单位（毫秒）
        /// </summary>
        public uint now => (uint)Time.realtimeSinceStartup * 1000;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="getFileCallback">获取资源真实路径回调</param>
        /// <param name="offset">获取bundle的偏移</param>
        public void Initialize(Func<string, string> getFileCallback, ulong offset)
        {
            BundleManager.instance.Initialize(getFileCallback, offset);

            string manifestBunldeFile = getFileCallback.Invoke(MANIFEST_BUNDLE);
            AssetBundle manifestAssetBundle = AssetBundle.LoadFromFile(manifestBunldeFile, 0, offset);

            TextAsset resourceTextAsset = manifestAssetBundle.LoadAsset(RESOURCE_ASSET_NAME) as TextAsset;
            TextAsset bundleTextAsset = manifestAssetBundle.LoadAsset(BUNDLE_ASSET_NAME) as TextAsset;
            TextAsset dependencyTextAsset = manifestAssetBundle.LoadAsset(DEPENDENCY_ASSET_NAME) as TextAsset;

            byte[] resourceBytes = resourceTextAsset.bytes;
            byte[] bundleBytes = bundleTextAsset.bytes;
            byte[] dependencyBytes = dependencyTextAsset.bytes;

            manifestAssetBundle.Unload(true);
            manifestAssetBundle = null;

            //保存id对应的asseturl
            Dictionary<ushort, string> assetUrlDic = new Dictionary<ushort, string>();

            #region 读取资源信息
            {
                MemoryStream resourceMemoryStream = new MemoryStream(resourceBytes);
                BinaryReader resourceBinaryReader = new BinaryReader(resourceMemoryStream);
                //获取资源个数
                ushort resourceCount = resourceBinaryReader.ReadUInt16();
                for (int i = 0; i < resourceCount; i++)
                {
                    ushort assetId = resourceBinaryReader.ReadUInt16();
                    string assetUrl = resourceBinaryReader.ReadString();
                    assetUrlDic.Add(assetId, assetUrl);
                }
            }
            #endregion

            #region 读取bundle信息
            {
                ResourceBunldeDic.Clear();
                MemoryStream bundleMemoryStream = new MemoryStream(bundleBytes);
                BinaryReader bundleBinaryReader = new BinaryReader(bundleMemoryStream);
                //获取bundle个数
                ushort bundleCount = bundleBinaryReader.ReadUInt16();
                for (int i = 0; i < bundleCount; i++)
                {
                    string bundleUrl = bundleBinaryReader.ReadString();
                    //string bundleFileUrl = getFileCallback(bundleUrl);
                    string bundleFileUrl = bundleUrl;
                    //获取bundle内的资源个数
                    ushort resourceCount = bundleBinaryReader.ReadUInt16();
                    for (int ii = 0; ii < resourceCount; ii++)
                    {
                        ushort assetId = bundleBinaryReader.ReadUInt16();
                        string assetUrl = assetUrlDic[assetId];
                        ResourceBunldeDic.Add(assetUrl, bundleFileUrl);
                    }
                }
            }
            #endregion

            #region 读取资源依赖信息
            {
                ResourceDependencyDic.Clear();
                MemoryStream dependencyMemoryStream = new MemoryStream(dependencyBytes);
                BinaryReader dependencyBinaryReader = new BinaryReader(dependencyMemoryStream);
                //获取依赖链个数
                ushort dependencyCount = dependencyBinaryReader.ReadUInt16();
                for (int i = 0; i < dependencyCount; i++)
                {
                    //获取资源个数
                    ushort resourceCount = dependencyBinaryReader.ReadUInt16();
                    ushort assetId = dependencyBinaryReader.ReadUInt16();
                    string assetUrl = assetUrlDic[assetId];
                    List<string> dependencyList = new List<string>(resourceCount);
                    for (int ii = 1; ii < resourceCount; ii++)
                    {
                        ushort dependencyAssetId = dependencyBinaryReader.ReadUInt16();
                        string dependencyUrl = assetUrlDic[dependencyAssetId];
                        dependencyList.Add(dependencyUrl);
                    }

                    ResourceDependencyDic.Add(assetUrl, dependencyList);
                }
            }
            #endregion
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <param name="url">资源Url</param>
        /// <param name="async">是否异步</param>
        /// <param name="delay">延迟释放时间</param>
        /// <returns> Task<AResource> </returns>
        public AResource Load(string url, bool async, uint delay)
        {
            return LoadInternal(url, async, delay, false);
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <param name="url">资源Url</param>
        /// <param name="async">是否异步</param>
        /// <param name="delay">延迟释放时间</param>
        /// <returns></returns>
        public Task<AResource> LoadTask(string url, bool async, uint delay)
        {
            AResource resource = LoadInternal(url, async, delay, false);

            if (resource.done)
            {
                if (resource.awaiter == null)
                {
                    resource.awaiter = new ResourceAwaiter(url);
                    resource.awaiter.SetResult(resource);
                }

                return resource.awaiter.taskCompletionSource.Task;
            }

            if (resource.awaiter == null)
            {
                resource.awaiter = new ResourceAwaiter(url);
            }

            return resource.awaiter.taskCompletionSource.Task;
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <param name="url">资源Url</param>
        /// <param name="async">是否异步</param>
        /// <param name="delay">延迟释放时间</param>
        /// <param name="callback">加载完成回调</param>
        public void LoadWithCallback(string url, bool async, uint delay, Action<AResource> callback)
        {
            AResource resource = LoadInternal(url, async, delay, false);
            if (resource.done)
            {
                callback?.Invoke(resource);
            }
            else
            {
                resource.finishedCallback += callback;
            }
        }

        /// <summary>
        /// 内部加载资源
        /// </summary>
        /// <param name="url">资源url</param>
        /// <param name="async">是否异步</param>
        /// <param name="delay">延迟释放时间</param>
        /// <param name="dependency">是否依赖</param>
        /// <returns></returns>
        private AResource LoadInternal(string url, bool async, uint delay, bool dependency)
        {
            AResource resource = null;
            if (m_ResourceDic.TryGetValue(url, out resource))
            {
                //从需要释放的列表中移除
                if (resource.reference == 0)
                {
                    m_NeedUnloadList.Remove(resource);
                }

                if (delay > resource.delay)
                {
                    resource.delay = delay;
                }
                resource.AddReference();
                return resource;
            }

            //创建Resource
            if (m_Editor)
            {
                resource = new EditorResource();
            }
            else if (async)
            {
                ResourceAsync resourceAsync = new ResourceAsync();
                m_AsyncList.Add(resourceAsync);
                resource = resourceAsync;
            }
            else
            {
                resource = new Resource();
            }

            resource.url = url;
            resource.delay = delay;
            m_ResourceDic.Add(url, resource);

            //加载依赖
            List<string> dependencies = null;
            ResourceDependencyDic.TryGetValue(url, out dependencies);
            if (dependencies != null && dependencies.Count > 0)
            {
                resource.dependencies = new AResource[dependencies.Count];
                for (int i = 0; i < dependencies.Count; i++)
                {
                    string dependencyUrl = dependencies[i];
                    resource.dependencies[i] = LoadInternal(dependencyUrl, async, delay, true);
                }
            }

            resource.AddReference();
            resource.Load();

            return resource;
        }

        /// <summary>
        /// 即将要释放的资源
        /// </summary>
        /// <param name="resource"></param>
        public void WillUnload(AResource resource)
        {
            if (m_NeedUnloadList.Count == 0)
            {
                m_NeedUnloadList.Add(resource);
            }

            bool insertFlag = false;

            //插入排序,时间大的放前面
            for (int i = 0; i < m_NeedUnloadList.Count; i++)
            {
                AResource temp = m_NeedUnloadList[i];
                if (temp.destroyTime < resource.destroyTime)
                {
                    m_NeedUnloadList.Insert(i, resource);
                    return;
                }
            }

            if (insertFlag)
            {
                m_NeedUnloadList.Add(resource);
            }
        }

        public void Update()
        {
            BundleManager.instance.Update();

            for (int i = 0; i < m_AsyncList.Count; i++)
            {
                AResourceAsync resourceAsync = m_AsyncList[i];
                if (resourceAsync.Update())
                {
                    m_AsyncList.RemoveAt(i);
                    i--;

                    if (resourceAsync.awaiter != null)
                    {
                        resourceAsync.awaiter.SetResult(resourceAsync);
                    }
                }
            }
        }

        public void LateUpdate()
        {
            if (m_NeedUnloadList.Count == 0)
                return;

            int lastIndex = m_NeedUnloadList.Count - 1;
            AResource resource = m_NeedUnloadList[lastIndex];

            if (now < resource.destroyTime)
                return;

            m_NeedUnloadList.RemoveAt(lastIndex);
            resource.UnLoad();

            //依赖引用-1
            if (resource.dependencies != null)
            {
                for (int i = 0; i < resource.dependencies.Length; i++)
                {
                    AResource temp = resource.dependencies[i];
                    temp?.ReduceReference();
                }
            }
        }
    }
}