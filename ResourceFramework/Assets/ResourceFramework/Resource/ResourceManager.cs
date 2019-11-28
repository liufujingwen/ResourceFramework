using System;
using System.Collections.Generic;
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

        /// <summary>
        /// 所有资源集合
        /// </summary>
        private Dictionary<string, AResource> m_ResourceDic = new Dictionary<string, AResource>();

        /// <summary>
        /// 依赖数据
        /// </summary>
        private Dictionary<string, string[]> m_DependencyDic = new Dictionary<string, string[]>();

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
        /// 加载资源
        /// </summary>
        /// <param name="url">资源Url</param>
        /// <param name="async">是否异步</param>
        /// <param name="delay">延迟释放时间</param>
        /// <returns> Task<AResource> </returns>
        public Task<AResource> Load(string url, bool async, uint delay)
        {
            AResource resource = LoadInternal(url, async, delay, false);
            return resource.loadTask.Task;
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <param name="url">资源Url</param>
        /// <param name="async">是否异步</param>
        /// <param name="delay">延迟释放时间</param>
        /// <param name="callback">加载完成回调</param>
        public async void Load(string url, bool async, uint delay, Action<AResource> callback)
        {
            AResource resource = LoadInternal(url, async, delay, false);
            Task<AResource> task = resource.loadTask.Task;
            await task;
            callback?.Invoke(resource);
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
            string[] dependencies = null;
            m_DependencyDic.TryGetValue(url, out dependencies);
            if (dependencies != null && dependencies.Length > 0)
            {
                resource.dependencies = new AResource[dependencies.Length];
                for (int i = 0; i < dependencies.Length; i++)
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
                }
            }
        }

        public void LateUpdate()
        {
            if (m_NeedUnloadList.Count == 0)
                return;

            int lastIndex = m_NeedUnloadList.Count - 1;
            AResource resource = m_NeedUnloadList[lastIndex];
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