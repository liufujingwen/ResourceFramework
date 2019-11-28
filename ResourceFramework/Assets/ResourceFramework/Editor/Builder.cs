using UnityEditor;
using System.IO;
using System.ComponentModel;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

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

        #region Path

        /// <summary>
        /// 打包配置
        /// </summary>
        public static string BuildSettingPath = Path.GetFullPath("../BuildSetting.xml").Replace("\\", "/");

        /// <summary>
        /// 临时目录,临时生成的文件都统一放在该目录
        /// </summary>
        public static string TempPath = Path.Combine(Application.dataPath, "Temp").Replace("\\", "/");

        /// <summary>
        /// 资源描述__文本
        /// </summary>
        public static string ResourcePath_Text = $"{TempPath}/Resource.txt";

        /// <summary>
        /// 资源描述__二进制
        /// </summary>
        public static string ResourcePath_Binary = $"{TempPath}/Resource.bytes";

        /// <summary>
        /// Bundle描述__文本
        /// </summary>
        public static string BundlePath_Text = $"{TempPath}/Bundle.txt";

        /// <summary>
        /// Bundle描述__二进制
        /// </summary>
        public static string BundlePath_Binary = $"{TempPath}/Bundle.bytes";

        /// <summary>
        /// 资源依赖描述__文本
        /// </summary>
        public static string DependencyPath_Text = $"{TempPath}/Dependency.txt";

        /// <summary>
        /// 资源依赖描述__文本
        /// </summary>
        public static string DependencyPath_Binary = $"{TempPath}/Dependency.txt";

        public static string buildPath { get; set; }

        #endregion

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
            LoadSetting(BuildSettingPath);
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

            //该字典保存bundle对应的资源集合
            Dictionary<string, List<string>> bundleDic = CollectBundle(buildSetting, assetDic, dependencyDic);

            //生成Manifest文件
            GenerateManifest(assetDic, bundleDic, dependencyDic);

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

        /// <summary>
        /// 搜集bundle对应的ab名字
        /// </summary>
        /// <param name="buildSetting"></param>
        /// <param name="assetDic">资源列表</param>
        /// <param name="dependencyDic">资源依赖信息</param>
        /// <returns>bundle包信息</returns>
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

            //todo...  外部资源

            //排序
            foreach (List<string> list in bundleDic.Values)
            {
                list.Sort();
            }

            return bundleDic;
        }

        /// <summary>
        /// 生成资源描述文件
        /// <param name="assetDic">资源列表</param>
        /// <param name="bundleDic">bundle包信息</param>
        /// <param name="dependencyDic">资源依赖信息</param>
        /// </summary>
        private static void GenerateManifest(Dictionary<string, EReferenceType> assetDic, Dictionary<string, List<string>> bundleDic, Dictionary<string, List<string>> dependencyDic)
        {
            //生成临时存放文件的目录
            if (!Directory.Exists(TempPath))
                Directory.CreateDirectory(TempPath);

            //资源映射id
            Dictionary<string, ushort> assetIdDic = new Dictionary<string, ushort>();

            #region 生成资源描述信息
            {
                //删除资源描述文本文件
                if (File.Exists(ResourcePath_Text))
                    File.Delete(ResourcePath_Text);

                //删除资源描述二进制文件
                if (File.Exists(ResourcePath_Binary))
                    File.Delete(ResourcePath_Binary);

                //写入资源列表
                StringBuilder resourceSb = new StringBuilder();
                MemoryStream resourceMs = new MemoryStream();
                BinaryWriter resourceBw = new BinaryWriter(resourceMs);
                if (assetDic.Count > ushort.MaxValue)
                    throw new Exception($"资源个数超出{ushort.MaxValue}");
                //写入个数
                resourceBw.Write((ushort)assetDic.Count);
                ushort resourceId = 0;
                foreach (string assetUrl in assetDic.Keys)
                {
                    assetIdDic.Add(assetUrl, ++resourceId);
                    resourceSb.AppendLine($"{resourceId}\t{assetUrl}");
                    resourceBw.Write(resourceId);
                    resourceBw.Write(assetUrl);
                }

                resourceMs.Flush();
                byte[] buffer = resourceMs.GetBuffer();
                resourceBw.Close();
                //写入资源描述文本文件
                File.WriteAllText(ResourcePath_Text, resourceSb.ToString(), Encoding.UTF8);
                File.WriteAllBytes(ResourcePath_Binary, buffer);
            }
            #endregion

            #region 生成bundle描述信息
            {
                //删除bundle描述文本文件
                if (File.Exists(BundlePath_Text))
                    File.Delete(BundlePath_Text);

                //删除bundle描述二进制文件
                if (File.Exists(BundlePath_Binary))
                    File.Delete(BundlePath_Binary);

                //写入bundle信息
                StringBuilder bundleSb = new StringBuilder();
                MemoryStream bundleMs = new MemoryStream();
                BinaryWriter bundleBw = new BinaryWriter(bundleMs);

                //写入bundle个数
                bundleBw.Write((ushort)bundleDic.Count);
                foreach (var kv in bundleDic)
                {
                    string bundleName = kv.Key;
                    List<string> assets = kv.Value;

                    //写入bundle
                    bundleSb.AppendLine(bundleName);
                    bundleBw.Write(bundleName);

                    //写入资源个数
                    bundleBw.Write((ushort)assets.Count);

                    for (int i = 0; i < assets.Count; i++)
                    {
                        string assetUrl = assets[i];
                        ushort assetId = assetIdDic[assetUrl];
                        bundleSb.AppendLine($"\t{assetUrl}");
                        //写入资源id,用id替换字符串可以节省内存
                        bundleBw.Write(assetId);
                    }
                }

                bundleMs.Flush();
                byte[] buffer = bundleMs.GetBuffer();
                bundleBw.Close();
                //写入资源描述文本文件
                File.WriteAllText(BundlePath_Text, bundleSb.ToString(), Encoding.UTF8);
                File.WriteAllBytes(BundlePath_Binary, buffer);
            }
            #endregion

            #region 生成资源依赖描述信息
            {
                //删除资源依赖描述文本文件
                if (File.Exists(DependencyPath_Text))
                    File.Delete(DependencyPath_Text);

                //删除资源依赖描述二进制文件
                if (File.Exists(DependencyPath_Binary))
                    File.Delete(DependencyPath_Binary);

                //写入资源依赖信息
                StringBuilder dependencySb = new StringBuilder();
                MemoryStream dependencyMs = new MemoryStream();
                BinaryWriter dependencyBw = new BinaryWriter(dependencyMs);

                //用于保存资源依赖关系
                List<ushort> ids = new List<ushort>();
                //写入资源个数
                dependencyBw.Write((ushort)dependencyDic.Count);
                foreach (var kv in dependencyDic)
                {
                    string assetUrl = kv.Key;
                    List<string> dependencyAssets = kv.Value;

                    ids.Clear();
                    ids.Add(assetIdDic[assetUrl]);

                    string content = assetUrl;
                    for (int i = 0; i < dependencyAssets.Count; i++)
                    {
                        string dependencyAssetUrl = dependencyAssets[i];
                        content += $"\tdependencyAssetUrl";
                        ids.Add(assetIdDic[dependencyAssetUrl]);
                    }

                    dependencySb.AppendLine(content);

                    if (ids.Count > byte.MaxValue)
                        throw new Exception($"资源{assetUrl}的依赖超出一个字节上限:{byte.MaxValue}");
                    for (int i = 0; i < ids.Count; i++)
                        dependencyBw.Write(ids[i]);
                }

                dependencyMs.Flush();
                byte[] buffer = dependencyMs.GetBuffer();
                dependencyBw.Close();
                //写入资源依赖描述文本文件
                File.WriteAllText(DependencyPath_Text, dependencySb.ToString(), Encoding.UTF8);
                File.WriteAllBytes(DependencyPath_Binary, buffer);
            }
            #endregion
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
