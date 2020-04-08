using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

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
        private LinkedList<AResource> m_NeedUnloadList = new LinkedList<AResource>();

        /// <summary>
        /// 是否使用AssetDataBase进行加载
        /// </summary>
        private bool m_Editor;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="platform">平台</param>
        /// <param name="getFileCallback">获取资源真实路径回调</param>
        /// <param name="editor">是否使用AssetDataBase加载</param>
        /// <param name="offset">获取bundle的偏移</param>
        public void Initialize(string platform, Func<string, string> getFileCallback, bool editor, ulong offset)
        {
            m_Editor = editor;

            if (m_Editor)
                return;

            BundleManager.instance.Initialize(platform, getFileCallback, offset);

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
                for (ushort i = 0; i < resourceCount; i++)
                {
                    string assetUrl = resourceBinaryReader.ReadString();
                    assetUrlDic.Add(i, assetUrl);
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
        /// <returns> Task<AResource> </returns>
        public IResource Load(string url, bool async)
        {
            return LoadInternal(url, async, false);
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="url">资源Url</param>
        /// <returns></returns>
        public ResourceAwaiter LoadWithAwaiter(string url)
        {
            AResource resource = LoadInternal(url, true, false);

            if (resource.done)
            {
                if (resource.awaiter == null)
                {
                    resource.awaiter = new ResourceAwaiter();
                    resource.awaiter.SetResult(resource as IResource);
                }

                return resource.awaiter;
            }

            if (resource.awaiter == null)
                resource.awaiter = new ResourceAwaiter();

            return resource.awaiter;
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <param name="url">资源Url</param>
        /// <param name="async">是否异步</param>
        /// <param name="callback">加载完成回调</param>
        public void LoadWithCallback(string url, bool async, Action<IResource> callback)
        {
            AResource resource = LoadInternal(url, async, false);
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
        /// <param name="dependency">是否依赖</param>
        /// <returns></returns>
        private AResource LoadInternal(string url, bool async, bool dependency)
        {
            AResource resource = null;
            if (m_ResourceDic.TryGetValue(url, out resource))
            {
                //从需要释放的列表中移除
                if (resource.reference == 0)
                {
                    m_NeedUnloadList.Remove(resource);
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
                    AResource dependencyResource = LoadInternal(dependencyUrl, async, true);
                    resource.dependencies[i] = dependencyResource;
                }
            }

            resource.AddReference();
            resource.Load();

            return resource;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="resource"></param>
        public void Unload(IResource resource)
        {
            if (resource == null)
            {
                throw new ArgumentException($"{nameof(ResourceManager)}.{nameof(Unload)}() {nameof(resource)} is null.");
            }

            AResource aResource = resource as AResource;
            aResource.ReduceReference();

            if (aResource.reference == 0)
            {
                WillUnload(aResource);
            }
        }

        /// <summary>
        /// 通过路径释放资源
        /// </summary>
        /// <param name="assetUrl"></param>
        public void Unload(string assetUrl)
        {
            if (string.IsNullOrEmpty(assetUrl))
                throw new ArgumentException($"{nameof(ResourceManager)}.{nameof(Unload)}() {nameof(assetUrl)} is null.");

            AResource resource;
            if(!m_ResourceDic.TryGetValue(assetUrl,out resource))
                throw new Exception($"{nameof(ResourceManager)}.{nameof(Unload)}(),Unload [{assetUrl}] failed.");

            Unload(resource);
        }

        /// <summary>
        /// 即将要释放的资源
        /// </summary>
        /// <param name="resource">资源路径</param>
        private void WillUnload(AResource resource)
        {
            m_NeedUnloadList.AddLast(resource);
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
                        resourceAsync.awaiter.SetResult(resourceAsync as IResource);
                    }
                }
            }
        }

        public void LateUpdate()
        {
            if (m_NeedUnloadList.Count != 0)
            {
                while (m_NeedUnloadList.Count > 0)
                {
                    AResource resource = m_NeedUnloadList.First.Value;
                    m_NeedUnloadList.RemoveFirst();
                    if (resource == null)
                        continue;

                    m_ResourceDic.Remove(resource.url);

                    resource.UnLoad();

                    //依赖引用-1
                    if (resource.dependencies != null)
                    {
                        for (int i = 0; i < resource.dependencies.Length; i++)
                        {
                            AResource temp = resource.dependencies[i];
                            Unload(temp);
                        }
                    }
                }
            }

            BundleManager.instance.LateUpdate();
        }
    }
}