using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace ResourceFramework
{
    public class BuildItem
    {
        [DisplayName("资源路径")]
        [XmlAttribute("AssetPath")]
        public string assetPath { get; set; }

        [DisplayName("ab粒度类型")]
        [XmlAttribute("BundleType")]
        public EBundleType bundleType { get; set; } = EBundleType.File;

        [DisplayName("资源后缀")]
        [XmlAttribute("Suffix")]
        public string suffix { get; set; } = ".prefab";

        [XmlIgnore]
        public List<string> suffixes { get; set; }

        //匹配该打包设置的个数
        [XmlIgnore]
        public int Count { get; set; }
    }
}
