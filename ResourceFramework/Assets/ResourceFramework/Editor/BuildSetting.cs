using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;

namespace ResourceFramework
{
    public class BuildSetting : ISupportInitialize
    {
        [DisplayName("项目名称")]
        [XmlAttribute("ProjectName")]
        public string projectName { get; set; }

        [DisplayName("后缀列表")]
        [XmlAttribute("SuffixList")]
        public List<string> suffixList { get; set; } = new List<string>();

        [DisplayName("打包文件的目标文件夹")]
        [XmlAttribute("BuildRoot")]
        public string buildRoot { get; set; }

        [DisplayName("打包选项")]
        [XmlElement("BuildItem")]
        public List<BuildItem> items { get; set; } = new List<BuildItem>();

        [XmlIgnore]
        public Dictionary<string, BuildItem> itemDic = new Dictionary<string, BuildItem>();

        public void BeginInit()
        {
        }

        public void EndInit()
        {
            buildRoot = Path.GetFullPath(buildRoot).Replace("\\", "/");

            itemDic.Clear();

            for (int i = 0; i < items.Count; i++)
            {
                BuildItem buildItem = items[i];

                if (buildItem.bundleType == EBundleType.All || buildItem.bundleType == EBundleType.Directory)
                {
                    if (!Directory.Exists(buildItem.assetPath))
                    {
                        throw new Exception($"不存在资源路径:{buildItem.assetPath}");
                    }
                }

                //处理后缀
                string[] prefixes = buildItem.suffix.Split('|');
                for (int ii = 0; ii < prefixes.Length; ii++)
                {
                    string prefix = prefixes[ii].Trim();
                    if (!string.IsNullOrEmpty(prefix))
                        buildItem.suffixes.Add(prefix);
                }

                if (itemDic.ContainsKey(buildItem.assetPath))
                {
                    throw new Exception($"重复的资源路径:{buildItem.assetPath}");
                }
                itemDic.Add(buildItem.assetPath, buildItem);
            }
        }

        /// <summary>
        /// 获取所有在打包设置的文件列表
        /// </summary>
        /// <returns>文件列表</returns>
        public HashSet<string> Collect()
        {
            float min = Builder.collectRuleFileProgress.x;
            float max = Builder.collectRuleFileProgress.y;

            EditorUtility.DisplayProgressBar($"{nameof(Collect)}", "搜集打包规则资源", min);

            //处理每个规则忽略的目录,如路径A/B/C,需要忽略A/B
            for (int i = 0; i < items.Count; i++)
            {
                BuildItem buildItem_i = items[i];

                if (buildItem_i.resourceType != EResourceType.Direct)
                    continue;

                buildItem_i.ignorePaths.Clear();
                for (int j = 0; j < items.Count; j++)
                {
                    BuildItem buildItem_j = items[j];
                    if (i != j && buildItem_j.resourceType == EResourceType.Direct)
                    {
                        if (buildItem_j.assetPath.StartsWith(buildItem_i.assetPath, StringComparison.InvariantCulture))
                        {
                            buildItem_i.ignorePaths.Add(buildItem_j.assetPath);
                        }
                    }
                }
            }

            //存储被规则分析到的所有文件
            HashSet<string> files = new HashSet<string>();

            for (int i = 0; i < items.Count; i++)
            {
                BuildItem buildItem = items[i];

                EditorUtility.DisplayProgressBar($"{nameof(Collect)}", "搜集打包规则资源", min + (max - min) * ((float)i / (items.Count - 1)));

                if (buildItem.resourceType != EResourceType.Direct)
                    continue;

                List<string> tempFiles = Builder.GetFiles(buildItem.assetPath, null, buildItem.suffixes.ToArray());
                for (int j = 0; j < tempFiles.Count; j++)
                {
                    string file = tempFiles[j];

                    //过滤被忽略的
                    if (IsIgnore(buildItem.ignorePaths, file))
                        continue;

                    files.Add(file);
                }

                EditorUtility.DisplayProgressBar($"{nameof(Collect)}", "搜集打包设置资源", (float)(i + 1) / items.Count);
            }

            return files;
        }

        /// <summary>
        /// 文件是否在忽略列表
        /// </summary>
        /// <param name="ignoreList">忽略路径列表</param>
        /// <param name="file">文件路径</param>
        /// <returns></returns>
        public bool IsIgnore(List<string> ignoreList, string file)
        {
            for (int i = 0; i < ignoreList.Count; i++)
            {
                string ignorePath = ignoreList[i];
                if (string.IsNullOrEmpty(ignorePath))
                    continue;
                if (file.StartsWith(ignorePath, StringComparison.InvariantCulture))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 通过资源获取打包选项
        /// </summary>
        /// <param name="assetUrl">资源路径</param>
        /// <returns>打包选项</returns>
        public BuildItem GetBuildItem(string assetUrl)
        {
            BuildItem item = null;
            for (int i = 0; i < items.Count; ++i)
            {
                BuildItem tempItem = items[i];
                //前面是否匹配
                if (assetUrl.StartsWith(tempItem.assetPath, StringComparison.InvariantCulture))
                {
                    //找到优先级最高的Rule,路径越长说明优先级越高
                    if (item == null || item.assetPath.Length < tempItem.assetPath.Length)
                    {
                        item = tempItem;
                    }
                }
            }

            return item;
        }

        /// <summary>
        /// 获取BundleName
        /// </summary>
        /// <param name="assetUrl">资源路径</param>
        /// <param name="resourceType">资源类型</param>
        /// <returns>BundleName</returns>
        public string GetBundleName(string assetUrl, EResourceType resourceType)
        {
            BuildItem buildItem = GetBuildItem(assetUrl);

            if (buildItem == null)
            {
                return null;
            }

            string name;

            //依赖类型一定要匹配后缀
            if (buildItem.resourceType == EResourceType.Dependency)
            {
                string extension = Path.GetExtension(assetUrl).ToLower();
                bool exist = false;
                for (int i = 0; i < buildItem.suffixes.Count; i++)
                {
                    if (buildItem.suffixes[i] == extension)
                    {
                        exist = true;
                    }
                }

                if (!exist)
                {
                    return null;
                }
            }

            switch (buildItem.bundleType)
            {
                case EBundleType.All:
                    name = buildItem.assetPath;
                    if (buildItem.assetPath[buildItem.assetPath.Length - 1] == '/')
                        name = buildItem.assetPath.Substring(0, buildItem.assetPath.Length - 1);
                    name = $"{name}{Builder.BUNDLE_SUFFIX}".ToLowerInvariant();
                    break;
                case EBundleType.Directory:
                    name = $"{assetUrl.Substring(0, assetUrl.LastIndexOf('/'))}{Builder.BUNDLE_SUFFIX}".ToLowerInvariant();
                    break;
                case EBundleType.File:
                    name = $"{assetUrl}{Builder.BUNDLE_SUFFIX}".ToLowerInvariant();
                    break;
                default:
                    throw new Exception($"无法获取{assetUrl}的BundleName");
            }

            buildItem.Count += 1;

            return name;
        }

        public static BuildSetting Create(string file)
        {
            BuildSetting buildSetting = new BuildSetting();
            XmlUtility.Save(file, buildSetting);
            return buildSetting;
        }

        public void Save()
        {
            XmlUtility.Save(Builder.BuildSettingPath, this);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="index"></param>
        public void RemoveRule(int index)
        {
            if (items == null || items.Count <= index)
                return;

            items.RemoveAt(index);

            Save();
        }


        /// <summary>
        /// 添加规则
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="rule">规则</param>
        /// <param name="suffix">后缀</param>
        public void AddRule(string path, EBundleType rule, SearchOption searchOption)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("添加的规则路径不能为空!!");
                return;
            }

            if (!Directory.Exists(path))
            {
                Debug.LogError($"不存在路径:{path}!!");
                return;
            }

            if (GetBuildItem(path) != null)
            {
                Debug.LogError($"该规则已经存在:{path}!!");
                return;
            }

            BuildItem ruleEntry = new BuildItem { assetPath = path, bundleType = rule };
            items.Add(ruleEntry);

            Save();
        }

        public void AddRule(BuildItem item)
        {
            if (string.IsNullOrEmpty(item.assetPath))
            {
                Debug.LogError("添加的规则路径不能为空!!");
                return;
            }

            if (!Directory.Exists(item.assetPath))
            {
                Debug.LogError($"不存在路径:{item.assetPath}!!");
                return;
            }

            if (GetBuildItem(item.assetPath) != null)
            {
                Debug.LogError($"该规则已经存在:{item.assetPath}!!");
                return;
            }

            items.Add(item);
            Save();
        }

        public void AddSuffix(string suffix)
        {
            if (suffixList.Contains(suffix))
            {
                Debug.LogError("重复后缀");
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                BuildItem item = items[i];
            }

            suffixList.Add(suffix);
            Save();
        }

        public void RemoveSuffix(int index)
        {
            if (suffixList.Count <= index)
                return;

            suffixList.RemoveAt(index);

            for (int i = 0; i < items.Count; i++)
            {
                BuildItem item = items[i];

                item.suffix = string.Empty;

                for (int idx = 0; idx < suffixList.Count; idx++)
                {
                    if (!string.IsNullOrEmpty(item.suffix))
                        item.suffix = item.suffix + "|" + suffixList[idx];
                    else
                        item.suffix = item.suffix + suffixList[idx];
                }
            }
        }
    }
}