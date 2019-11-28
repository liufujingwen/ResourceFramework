using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;

namespace ResourceFramework
{
    public class BuildSetting : ISupportInitialize
    {
        [DisplayName("项目名称")]
        [XmlAttribute("ProjectName")]
        public string projectName { get; set; }

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

                //if (buildItem.bundleType == EBundleType.All || buildItem.bundleType == EBundleType.Directory)
                //{
                //    if (!Directory.Exists(buildItem.assetPath))
                //    {
                //        throw new Exception($"不存在资源路径:{buildItem.assetPath}");
                //    }
                //}

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
            HashSet<string> files = new HashSet<string>();

            for (int i = 0; i < items.Count; i++)
            {
                BuildItem buildItem = items[i];
                List<string> tempFiles = Builder.GetFiles(buildItem.assetPath, null, buildItem.suffixes.ToArray());
                for (int ii = 0; ii < tempFiles.Count; ii++)
                {
                    files.Add(tempFiles[ii]);
                }
            }

            return files;
        }

        /// <summary>
        /// 通过资源获取打包选项
        /// </summary>
        /// <param name="assetUrl">资源路径</param>
        /// <returns>打包选项</returns>
        public BuildItem GetBuildItem(string assetUrl)
        {
            string extension = Path.GetExtension(assetUrl).ToLower();

            for (int i = 0; i < items.Count; ++i)
            {
                BuildItem item = items[i];
                //前面是否匹配
                if (assetUrl.StartsWith(item.assetPath))
                {
                    //后缀是否匹配
                    for (int ii = 0; ii < item.suffixes.Count; ii++)
                    {
                        string suffix = item.suffixes[ii];
                        if (suffix == extension)
                            return item;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 获取BundleName
        /// </summary>
        /// <param name="assetUrl">资源路径</param>
        /// <param name="referenceType">引用类型</param>
        /// <returns>BundleName</returns>
        public string GetBundleName(string assetUrl, EReferenceType referenceType)
        {
            BuildItem buildItem = GetBuildItem(assetUrl);

            if (buildItem == null)
            {
                return null;
            }

            string name;

            switch (buildItem.bundleType)
            {
                case EBundleType.All:
                    name = $"{buildItem.assetPath.Substring(0, buildItem.assetPath.LastIndexOf('/'))}{Builder.ASSET_BUNDLE}".ToLowerInvariant();
                    break;
                case EBundleType.Directory:
                    name = $"{assetUrl.Substring(0, assetUrl.LastIndexOf('/'))}{Builder.ASSET_BUNDLE}".ToLowerInvariant();
                    break;
                case EBundleType.File:
                    name = $"{assetUrl}{Builder.ASSET_BUNDLE}".ToLowerInvariant();
                    break;
                default:
                    throw new Exception($"无法获取{assetUrl}的BundleName");
            }

            buildItem.Count += 1;

            return name;
        }
    }
}
