using UnityEditor;
using System.IO;
using System.ComponentModel;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Threading.Tasks;

namespace ResourceFramework
{
    public static class Builder
    {
        public static readonly Vector2 collectRuleFileProgress = new Vector2(0, 0.2f);
        private static readonly Vector2 ms_GetDependencyProgress = new Vector2(0.2f, 0.4f);
        private static readonly Vector2 ms_CollectBundleInfoProgress = new Vector2(0.4f, 0.5f);
        private static readonly Vector2 ms_GenerateBuildInfoProgress = new Vector2(0.5f, 0.6f);
        private static readonly Vector2 ms_BuildBundleProgress = new Vector2(0.6f, 0.7f);
        private static readonly Vector2 ms_ClearBundleProgress = new Vector2(0.7f, 0.9f);
        private static readonly Vector2 ms_BuildManifestProgress = new Vector2(0.9f, 1f);

        private static readonly Profiler ms_BuildProfiler = new Profiler(nameof(Builder));
        private static readonly Profiler ms_LoadBuildSettingProfiler = ms_BuildProfiler.CreateChild(nameof(LoadSetting));
        private static readonly Profiler ms_SwitchPlatformProfiler = ms_BuildProfiler.CreateChild(nameof(SwitchPlatform));
        private static readonly Profiler ms_CollectProfiler = ms_BuildProfiler.CreateChild(nameof(Collect));
        private static readonly Profiler ms_CollectBuildSettingFileProfiler = ms_CollectProfiler.CreateChild("CollectBuildSettingFile");
        private static readonly Profiler ms_CollectDependencyProfiler = ms_CollectProfiler.CreateChild(nameof(CollectDependency));
        private static readonly Profiler ms_CollectBundleProfiler = ms_CollectProfiler.CreateChild(nameof(CollectBundle));
        private static readonly Profiler ms_GenerateManifestProfiler = ms_CollectProfiler.CreateChild(nameof(GenerateManifest));
        private static readonly Profiler ms_BuildBundleProfiler = ms_BuildProfiler.CreateChild(nameof(BuildBundle));
        private static readonly Profiler ms_ClearBundleProfiler = ms_BuildProfiler.CreateChild(nameof(ClearAssetBundle));
        private static readonly Profiler ms_BuildManifestBundleProfiler = ms_BuildProfiler.CreateChild(nameof(BuildManifest));

#if UNITY_IOS
        private const string PLATFORM = "iOS";
#elif UNITY_ANDROID
        private const string PLATFORM = "Android";
#else
        private const string PLATFORM = "Windows";
#endif
        //bundle后缀
        public const string BUNDLE_SUFFIX = ".ab";
        public const string BUNDLE_MANIFEST_SUFFIX = ".manifest";
        //bundle描述文件名称
        public const string MANIFEST = "manifest";

        public static readonly ParallelOptions ParallelOptions = new ParallelOptions()
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount * 2
        };

        //bundle打包Options
        public readonly static BuildAssetBundleOptions BuildAssetBundleOptions = BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.DeterministicAssetBundle | BuildAssetBundleOptions.StrictMode | BuildAssetBundleOptions.DisableLoadAssetByFileName | BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension;

        /// <summary>
        /// 打包设置
        /// </summary>
        public static BuildSetting buildSetting { get; private set; }

        #region Path

        /// <summary>
        /// 打包配置
        /// </summary>
        public readonly static string BuildSettingPath = Path.GetFullPath("BuildSetting.xml").Replace("\\", "/");

        /// <summary>
        /// 临时目录,临时生成的文件都统一放在该目录
        /// </summary>
        public readonly static string TempPath = Path.GetFullPath(Path.Combine(Application.dataPath, "Temp")).Replace("\\", "/");

        /// <summary>
        /// 临时目录,临时文件的ab包都放在该文件夹，打包完成后会移除
        /// </summary>
        public readonly static string TempBuildPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../TempBuild")).Replace("\\", "/");

        /// <summary>
        /// 资源描述__文本
        /// </summary>
        public readonly static string ResourcePath_Text = $"{TempPath}/Resource.txt";

        /// <summary>
        /// 资源描述__二进制
        /// </summary>
        public static string ResourcePath_Binary = $"{TempPath}/Resource.bytes";

        /// <summary>
        /// Bundle描述__文本
        /// </summary>
        public readonly static string BundlePath_Text = $"{TempPath}/Bundle.txt";

        /// <summary>
        /// Bundle描述__二进制
        /// </summary>
        public readonly static string BundlePath_Binary = $"{TempPath}/Bundle.bytes";

        /// <summary>
        /// 资源依赖描述__文本
        /// </summary>
        public readonly static string DependencyPath_Text = $"{TempPath}/Dependency.txt";

        /// <summary>
        /// 资源依赖描述__文本
        /// </summary>
        public readonly static string DependencyPath_Binary = $"{TempPath}/Dependency.bytes";

        /// <summary>
        /// 打包目录
        /// </summary>
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
        private static BuildSetting LoadSetting(string settingPath)
        {
            buildSetting = XmlUtility.Read<BuildSetting>(settingPath);
            if (buildSetting == null)
            {
                throw new Exception($"Load buildSetting failed,SettingPath:{settingPath}.");
            }
            (buildSetting as ISupportInitialize)?.EndInit();

            buildPath = Path.GetFullPath(buildSetting.buildRoot).Replace("\\", "/");
            if (buildPath.Length > 0 && buildPath[buildPath.Length - 1] != '/')
            {
                buildPath += "/";
            }
            buildPath += $"{PLATFORM}/";

            return buildSetting;
        }

        private static void Build()
        {
            ms_BuildProfiler.Start();

            ms_SwitchPlatformProfiler.Start();
            SwitchPlatform();
            ms_SwitchPlatformProfiler.Stop();

            ms_LoadBuildSettingProfiler.Start();
            buildSetting = LoadSetting(BuildSettingPath);
            ms_LoadBuildSettingProfiler.Stop();

            //搜集bundle信息
            ms_CollectProfiler.Start();
            Dictionary<string, List<string>> bundleDic = Collect();
            ms_CollectProfiler.Stop();

            //打包assetbundle
            ms_BuildBundleProfiler.Start();
            BuildBundle(bundleDic);
            ms_BuildBundleProfiler.Stop();

            //清空多余文件
            ms_ClearBundleProfiler.Start();
            ClearAssetBundle(buildPath, bundleDic);
            ms_ClearBundleProfiler.Stop();

            //把描述文件打包bundle
            ms_BuildManifestBundleProfiler.Start();
            BuildManifest();
            ms_BuildManifestBundleProfiler.Stop();

            EditorUtility.ClearProgressBar();

            ms_BuildProfiler.Stop();

            Debug.Log($"打包完成{ms_BuildProfiler}");
        }

        /// <summary>
        /// 搜集打包bundle的信息
        /// </summary>
        /// <returns></returns>

        private static Dictionary<string, List<string>> Collect()
        {
            //获取所有在打包设置的文件列表
            ms_CollectBuildSettingFileProfiler.Start();
            HashSet<string> files = buildSetting.Collect();
            ms_CollectBuildSettingFileProfiler.Stop();

            //搜集所有文件的依赖关系
            ms_CollectDependencyProfiler.Start();
            Dictionary<string, List<string>> dependencyDic = CollectDependency(files);
            ms_CollectDependencyProfiler.Stop();

            //标记所有资源的信息
            Dictionary<string, EResourceType> assetDic = new Dictionary<string, EResourceType>();

            //被打包配置分析到的直接设置为Direct
            foreach (string url in files)
            {
                assetDic.Add(url, EResourceType.Direct);
            }

            //依赖的资源标记为Dependency，已经存在的说明是Direct的资源
            foreach (string url in dependencyDic.Keys)
            {
                if (!assetDic.ContainsKey(url))
                {
                    assetDic.Add(url, EResourceType.Dependency);
                }
            }

            //该字典保存bundle对应的资源集合
            ms_CollectBundleProfiler.Start();
            Dictionary<string, List<string>> bundleDic = CollectBundle(buildSetting, assetDic, dependencyDic);
            ms_CollectBundleProfiler.Stop();

            //生成Manifest文件
            ms_GenerateManifestProfiler.Start();
            GenerateManifest(assetDic, bundleDic, dependencyDic);
            ms_GenerateManifestProfiler.Stop();

            return bundleDic;
        }

        /// <summary>
        /// 收集指定文件集合所有的依赖信息
        /// </summary>
        /// <param name="files">文件集合</param>
        /// <returns>依赖信息</returns>
        private static Dictionary<string, List<string>> CollectDependency(ICollection<string> files)
        {
            float min = ms_GetDependencyProgress.x;
            float max = ms_GetDependencyProgress.y;

            Dictionary<string, List<string>> dependencyDic = new Dictionary<string, List<string>>();

            //声明fileList后，就不需要递归了
            List<string> fileList = new List<string>(files);

            for (int i = 0; i < fileList.Count; i++)
            {
                string assetUrl = fileList[i];

                if (dependencyDic.ContainsKey(assetUrl))
                    continue;

                if (i % 10 == 0)
                {
                    //只能大概模拟进度
                    float progress = min + (max - min) * ((float)i / (files.Count * 3));
                    EditorUtility.DisplayProgressBar($"{nameof(CollectDependency)}", "搜集依赖信息", progress);
                }

                string[] dependencies = AssetDatabase.GetDependencies(assetUrl, false);
                List<string> dependencyList = new List<string>(dependencies.Length);

                //过滤掉不符合要求的asset
                for (int ii = 0; ii < dependencies.Length; ii++)
                {
                    string tempAssetUrl = dependencies[ii];
                    string extension = Path.GetExtension(tempAssetUrl).ToLower();
                    if (string.IsNullOrEmpty(extension) || extension == ".cs" || extension == ".dll")
                        continue;
                    dependencyList.Add(tempAssetUrl);
                    if (!fileList.Contains(tempAssetUrl))
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
        private static Dictionary<string, List<string>> CollectBundle(BuildSetting buildSetting, Dictionary<string, EResourceType> assetDic, Dictionary<string, List<string>> dependencyDic)
        {
            float min = ms_CollectBundleInfoProgress.x;
            float max = ms_CollectBundleInfoProgress.y;

            EditorUtility.DisplayProgressBar($"{nameof(CollectBundle)}", "搜集bundle信息", min);

            Dictionary<string, List<string>> bundleDic = new Dictionary<string, List<string>>();
            //外部资源
            List<string> notInRuleList = new List<string>();

            int index = 0;
            foreach (KeyValuePair<string, EResourceType> pair in assetDic)
            {
                index++;
                string assetUrl = pair.Key;
                string bundleName = buildSetting.GetBundleName(assetUrl, pair.Value);

                //没有bundleName的资源为外部资源
                if (bundleName == null)
                {
                    notInRuleList.Add(assetUrl);
                    continue;
                }

                List<string> list;
                if (!bundleDic.TryGetValue(bundleName, out list))
                {
                    list = new List<string>();
                    bundleDic.Add(bundleName, list);
                }

                list.Add(assetUrl);

                EditorUtility.DisplayProgressBar($"{nameof(CollectBundle)}", "搜集bundle信息", min + (max - min) * ((float)index / assetDic.Count));
            }

            //todo...  外部资源
            if (notInRuleList.Count > 0)
            {
                string massage = string.Empty;
                for (int i = 0; i < notInRuleList.Count; i++)
                {
                    massage += "\n" + notInRuleList[i];
                }
                EditorUtility.ClearProgressBar();
                throw new Exception($"资源不在打包规则,或者后缀不匹配！！！{massage}");
            }

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
        private static void GenerateManifest(Dictionary<string, EResourceType> assetDic, Dictionary<string, List<string>> bundleDic, Dictionary<string, List<string>> dependencyDic)
        {
            float min = ms_GenerateBuildInfoProgress.x;
            float max = ms_GenerateBuildInfoProgress.y;

            EditorUtility.DisplayProgressBar($"{nameof(GenerateManifest)}", "生成打包信息", min);

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
                {
                    EditorUtility.ClearProgressBar();
                    throw new Exception($"资源个数超出{ushort.MaxValue}");
                }

                //写入个数
                resourceBw.Write((ushort)assetDic.Count);
                List<string> keys = new List<string>(assetDic.Keys);
                keys.Sort();

                for (ushort i = 0; i < keys.Count; i++)
                {
                    string assetUrl = keys[i];
                    assetIdDic.Add(assetUrl, i);
                    resourceSb.AppendLine($"{i}\t{assetUrl}");
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

            EditorUtility.DisplayProgressBar($"{nameof(GenerateManifest)}", "生成打包信息", min + (max - min) * 0.3f);

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

            EditorUtility.DisplayProgressBar($"{nameof(GenerateManifest)}", "生成打包信息", min + (max - min) * 0.8f);

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

                //用于保存资源依赖链
                List<List<ushort>> dependencyList = new List<List<ushort>>();
                foreach (var kv in dependencyDic)
                {
                    List<string> dependencyAssets = kv.Value;

                    //依赖为0的不需要写入
                    if (dependencyAssets.Count == 0)
                        continue;

                    string assetUrl = kv.Key;

                    List<ushort> ids = new List<ushort>();
                    ids.Add(assetIdDic[assetUrl]);

                    string content = assetUrl;
                    for (int i = 0; i < dependencyAssets.Count; i++)
                    {
                        string dependencyAssetUrl = dependencyAssets[i];
                        content += $"\t{dependencyAssetUrl}";
                        ids.Add(assetIdDic[dependencyAssetUrl]);
                    }

                    dependencySb.AppendLine(content);

                    if (ids.Count > byte.MaxValue)
                    {
                        EditorUtility.ClearProgressBar();
                        throw new Exception($"资源{assetUrl}的依赖超出一个字节上限:{byte.MaxValue}");
                    }

                    dependencyList.Add(ids);
                }

                //写入依赖链个数
                dependencyBw.Write((ushort)dependencyList.Count);
                for (int i = 0; i < dependencyList.Count; i++)
                {
                    //写入资源数
                    List<ushort> ids = dependencyList[i];
                    dependencyBw.Write((ushort)ids.Count);
                    for (int ii = 0; ii < ids.Count; ii++)
                        dependencyBw.Write(ids[ii]);
                }

                dependencyMs.Flush();
                byte[] buffer = dependencyMs.GetBuffer();
                dependencyBw.Close();
                //写入资源依赖描述文本文件
                File.WriteAllText(DependencyPath_Text, dependencySb.ToString(), Encoding.UTF8);
                File.WriteAllBytes(DependencyPath_Binary, buffer);
            }
            #endregion


            AssetDatabase.Refresh();

            EditorUtility.DisplayProgressBar($"{nameof(GenerateManifest)}", "生成打包信息", max);

            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// 打包AssetBundle
        /// <param name="assetDic">资源列表</param>
        /// <param name="bundleDic">bundle包信息</param>
        /// <param name="dependencyDic">资源依赖信息</param>
        /// </summary>
        private static AssetBundleManifest BuildBundle(Dictionary<string, List<string>> bundleDic)
        {
            float min = ms_BuildBundleProgress.x;
            float max = ms_BuildBundleProgress.y;

            EditorUtility.DisplayProgressBar($"{nameof(BuildBundle)}", "打包AssetBundle", min);

            if (!Directory.Exists(buildPath))
                Directory.CreateDirectory(buildPath);

            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(buildPath, GetBuilds(bundleDic), BuildAssetBundleOptions, EditorUserBuildSettings.activeBuildTarget);

            EditorUtility.DisplayProgressBar($"{nameof(BuildBundle)}", "打包AssetBundle", max);

            return manifest;
        }

        /// <summary>
        /// 把Resource.bytes、bundle.bytes、Dependency.bytes 打包assetbundle
        /// </summary>
        private static void BuildManifest()
        {
            float min = ms_BuildManifestProgress.x;
            float max = ms_BuildManifestProgress.y;

            EditorUtility.DisplayProgressBar($"{nameof(BuildManifest)}", "将Manifest打包成AssetBundle", min);

            if (!Directory.Exists(TempBuildPath))
                Directory.CreateDirectory(TempBuildPath);

            string prefix = Application.dataPath.Replace("/Assets", "/").Replace("\\", "/");

            AssetBundleBuild manifest = new AssetBundleBuild();
            manifest.assetBundleName = $"{MANIFEST}{BUNDLE_SUFFIX}";
            manifest.assetNames = new string[3]
            {
                ResourcePath_Binary.Replace(prefix,""),
                BundlePath_Binary.Replace(prefix,""),
                DependencyPath_Binary.Replace(prefix,""),
            };

            EditorUtility.DisplayProgressBar($"{nameof(BuildManifest)}", "将Manifest打包成AssetBundle", min + (max - min) * 0.5f);

            AssetBundleManifest assetBundleManifest = BuildPipeline.BuildAssetBundles(TempBuildPath, new AssetBundleBuild[] { manifest }, BuildAssetBundleOptions, EditorUserBuildSettings.activeBuildTarget);

            //把文件copy到build目录
            if (assetBundleManifest)
            {
                string manifestFile = $"{TempBuildPath}/{MANIFEST}{BUNDLE_SUFFIX}";
                string target = $"{buildPath}/{MANIFEST}{BUNDLE_SUFFIX}";
                if (File.Exists(manifestFile))
                {
                    File.Copy(manifestFile, target);
                }
            }

            //删除临时目录
            if (Directory.Exists(TempBuildPath))
                Directory.Delete(TempBuildPath, true);

            EditorUtility.DisplayProgressBar($"{nameof(BuildManifest)}", "将Manifest打包成AssetBundle", max);
        }

        /// <summary>
        /// 获取所有需要打包的AssetBundleBuild
        /// </summary>
        /// <param name="bundleTable">bunlde信息</param>
        /// <returns></returns>
        private static AssetBundleBuild[] GetBuilds(Dictionary<string, List<string>> bundleTable)
        {
            int index = 0;
            AssetBundleBuild[] assetBundleBuilds = new AssetBundleBuild[bundleTable.Count];
            foreach (KeyValuePair<string, List<string>> pair in bundleTable)
            {
                assetBundleBuilds[index++] = new AssetBundleBuild()
                {
                    assetBundleName = pair.Key,
                    assetNames = pair.Value.ToArray(),
                };
            }

            return assetBundleBuilds;
        }

        /// <summary>
        /// 清空多余的assetbundle
        /// </summary>
        /// <param name="path">打包路径</param>
        /// <param name="bundleDic"></param>
        private static void ClearAssetBundle(string path, Dictionary<string, List<string>> bundleDic)
        {
            float min = ms_ClearBundleProgress.x;
            float max = ms_ClearBundleProgress.y;

            EditorUtility.DisplayProgressBar($"{nameof(ClearAssetBundle)}", "清除多余的AssetBundle文件", min);

            List<string> fileList = GetFiles(path, null, null);
            HashSet<string> fileSet = new HashSet<string>(fileList);

            foreach (string bundle in bundleDic.Keys)
            {
                fileSet.Remove($"{path}{bundle}");
                fileSet.Remove($"{path}{bundle}{BUNDLE_MANIFEST_SUFFIX}");
            }

            fileSet.Remove($"{path}{PLATFORM}");
            fileSet.Remove($"{path}{PLATFORM}{BUNDLE_MANIFEST_SUFFIX}");

            Parallel.ForEach(fileSet, ParallelOptions, File.Delete);

            EditorUtility.DisplayProgressBar($"{nameof(ClearAssetBundle)}", "清除多余的AssetBundle文件", max);
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
            string[] files = Directory.GetFiles(path, $"*.*", SearchOption.AllDirectories);
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

                    for (int ii = 0; ii < suffixes.Length; ii++)
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
