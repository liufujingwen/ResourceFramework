using UnityEditor;
using System.IO;
using System.ComponentModel;
using System;
using System.Collections.Generic;

namespace ResourceFramework
{
    public static class Builder
    {
#if UNITY_IOS
        private const string PLATFORM = "ios";
#elif UNITY_ANDROID
        private const string PLATFORM = "android";
#else
        private const string PLATFORM = "windows";
#endif

        public const string ASSET_BUNDLE = ".ab";

        /// <summary>
        /// 打包设置
        /// </summary>
        public static BuildSetting buildSetting { get; private set; }

        /// <summary>
        /// 打包配置
        /// </summary>
        public static string BUILD_SETTING_PATH = Path.GetFullPath("../BuildSetting.xml").Replace("\\", "/");

        public static string buildPath { get; set; }

        #region Build MenuItem

        [MenuItem("Tools/Build/Windows")]
        public static void BuildWindows()
        {
            Build();
        }

        [MenuItem("Tools/Build/Android")]
        public static void BuildAndroid()
        {
            Build();
        }

        [MenuItem("Tools/Build/iOS")]
        public static void BuildIos()
        {
            Build();
        }

        #endregion

        /// <summary>
        /// 切换打包平台
        /// </summary>
        public static void SwitchPlatform()
        {
            string platform = PLATFORM;

            switch (platform)
            {
                case "windows":
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
                    break;
                case "android":
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                    break;
                case "ios":
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
                    break;
            }
        }

        /// <summary>
        /// 加载打包配置文件
        /// </summary>
        /// <param name="settingPath">打包配置路径</param>
        private static void LoadSetting(string settingPath)
        {
            buildSetting = new BuildSetting();

            buildSetting.projectName = "haru";
            buildSetting.buildRoot = "../build";

            for (int i = 0; i < 3; i++)
            {
                BuildItem item = new BuildItem();
                item.assetPath = "Assets/" + i + "/";
                item.bundleType = EBundleType.File;
                item.suffix = ".prefab";

                buildSetting.items.Add(item);
            }

            XmlUtility.Save(settingPath, buildSetting);

            buildSetting = XmlUtility.Read<BuildSetting>(settingPath);
            (buildSetting as ISupportInitialize).EndInit();

            buildPath = Path.GetFullPath(buildSetting.buildRoot).Replace("\\", "/");
            if (buildPath.Length > 0 && buildPath[buildPath.Length - 1] != '/')
            {
                buildPath += "/";
            }
            buildPath += PLATFORM + "/";
        }

        private static void Build()
        {
            SwitchPlatform();
            LoadSetting(BUILD_SETTING_PATH);
            Collect();
        }

        /// <summary>
        /// 搜集打包bundle的信息
        /// </summary>
        /// <returns></returns>

        private static Dictionary<string, List<string>> Collect()
        {
            //获取所有在打包设置的文件列表
            HashSet<string> files = buildSetting.Collect();

            //搜集所有文件的依赖关系
            Dictionary<string, List<string>> dependencyDic = CollectDependency(files);

            //标记所有资源的信息
            Dictionary<string, EReferenceType> assetDic = new Dictionary<string, EReferenceType>();

            //被打包配置分析到的直接设置为Direct
            foreach (string url in files)
            {
                assetDic.Add(url, EReferenceType.Direct);
            }

            //依赖的资源标记为Dependency，已经存在的说明是Direct的资源
            foreach (string url in dependencyDic.Keys)
            {
                if (!dependencyDic.ContainsKey(url))
                {
                    assetDic.Add(url, EReferenceType.Dependency);
                }
            }

            //最终打包的设置
            CollectBundle(buildSetting, assetDic, dependencyDic);

            Dictionary<string, List<string>> bundleDic = new Dictionary<string, List<string>>();
            return bundleDic;
        }

        /// <summary>
        /// 收集指定文件集合所有的依赖信息
        /// </summary>
        /// <param name="files">文件集合</param>
        /// <returns>依赖信息</returns>
        private static Dictionary<string, List<string>> CollectDependency(ICollection<string> files)
        {
            Dictionary<string, List<string>> dependencyDic = new Dictionary<string, List<string>>();

            //声明fileList后，就不需要递归了
            List<string> fileList = new List<string>(files);

            for (int i = 0; i < fileList.Count; i++)
            {
                string assetUrl = fileList[i];

                if (dependencyDic.ContainsKey(assetUrl))
                    continue;

                string[] dependencies = AssetDatabase.GetDependencies(assetUrl, false);
                List<string> dependencyList = new List<string>(dependencies.Length);

                //过滤掉不符合要求的asset
                for (int ii = 0; ii < dependencies.Length; ii++)
                {
                    string tempAssetUrl = dependencies[ii];
                    string extension = Path.GetExtension(tempAssetUrl).ToLower();
                    if (extension == ".cs" || extension == ".dll")
                        continue;
                    dependencyList.Add(tempAssetUrl);
                    fileList.Add(tempAssetUrl);
                }

                dependencyDic.Add(assetUrl, dependencyList);
            }

            return dependencyDic;
        }

        private static Dictionary<string, List<string>> CollectBundle(BuildSetting buildSetting, Dictionary<string, EReferenceType> assetDic, Dictionary<string, List<string>> dependencyDic)
        {
            Dictionary<string, List<string>> bundleDic = new Dictionary<string, List<string>>();
            //外部资源
            List<string> externalList = new List<string>();

            foreach (KeyValuePair<string, EReferenceType> pair in assetDic)
            {
                string assetUrl = pair.Key;
                string bundleName = buildSetting.GetBundleName(assetUrl, pair.Value);

                //没有bundleName的资源为外部资源
                if (bundleName == null)
                {
                    externalList.Add(assetUrl);
                    continue;
                }

                List<string> list;
                if (!bundleDic.TryGetValue(bundleName, out list))
                {
                    list = new List<string>();
                    bundleDic.Add(bundleName, list);
                }

                list.Add(assetUrl);
            }

            //排序
            foreach (List<string> list in bundleDic.Values)
            {
                list.Sort();
            }

            return bundleDic;
        }

        /// <summary>
        /// 获取指定路径的文件
        /// </summary>
        /// <param name="path">指定路径</param>
        /// <param name="prefix">前缀</param>
        /// <param name="suffixes">后缀集合</param>
        /// <returns>文件列表</returns>
        public static List<string> GetFiles(string path, string prefix, params string[] suffixes)
        {
            string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            List<string> result = new List<string>(files.Length);

            for (int i = 0; i < files.Length; ++i)
            {
                string file = files[i].Replace('\\', '/');

                if (prefix != null && !file.StartsWith(prefix, StringComparison.InvariantCulture))
                {
                    continue;
                }

                if (suffixes != null && suffixes.Length > 0)
                {
                    bool exist = false;

                    for (int ii = 0; ii < suffixes.Length; i++)
                    {
                        string suffix = suffixes[ii];
                        if (file.EndsWith(suffix, StringComparison.InvariantCulture))
                        {
                            exist = true;
                            break;
                        }
                    }

                    if (!exist)
                        continue;
                }

                result.Add(file);
            }

            return result;
        }
    }
}
