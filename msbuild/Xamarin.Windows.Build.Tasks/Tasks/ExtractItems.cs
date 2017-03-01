using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Xml;

namespace Xamarin.Windows.Tasks
{
    /// <summary>
    /// This MSBuild task extracts Items along with any custom metadata, 
    /// from another project/msbuild file and outputs them in another msbuild 
    /// file specified as an argument to the task.
    /// 
    /// Example:
    /// This example will extract CLCompile items and AppxManifest items and 
    /// put them in the (same) file specified by the variable <code>ExtractItemsOutputFile</code>.
    /// <code>
    ///   &lt;ExtractItems Items="@(ClCompile)" ItemType="ClCompile" Output="$(ExtractItemsOutputFile)"/&gt;
    ///   &lt;ExtractItems Items = "@(AppxManifest)" ItemType="AppxManifest" Output="$(ExtractItemsOutputFile)"/&gt;
    /// </code>
    /// 
    /// If the file specified in <code>ExtractItemsOutputFile</code> already exist and is non-empty,
    /// this task will load the file and append the items.
    /// 
    /// </summary>
    public class ExtractItems : Task, ICancelableTask
    {
        static readonly string MSBuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

        [Required]
        public ITaskItem[] Items { get; set; }

        [Required]
        public string ItemType { get; set; }

        [Required]
        public string Output { get; set; }

        public string LinkMetadataName { get; set; }

        public override bool Execute()
        {
            try
            {
                
                if (!File.Exists(Output) || new FileInfo(Output).Length == 0)
                {
                    using (XmlWriter writer = XmlWriter.Create(Output))
                    {
                        writer.WriteStartDocument();
                        writer.WriteStartElement("Project", MSBuildNamespace);
                        writer.WriteEndElement();
                        writer.WriteEndDocument();
                        writer.Flush();
                    }                    
                }
                XmlDocument doc = new XmlDocument();
                doc.Load(Output);
                XmlNode root = doc.DocumentElement;

                XmlElement group = doc.CreateElement("ItemGroup", MSBuildNamespace);
                root.AppendChild(group);

                if (string.IsNullOrEmpty(LinkMetadataName))
                {
                    LinkMetadataName = "Link";
                }

                Log.LogDebugMessage("ExtractItems Task");
                Log.LogDebugMessage("  ItemType: " + ItemType);
                Log.LogDebugMessage("  Output: " + Output);
                Log.LogDebugMessage("  LinkMetadataName: " + LinkMetadataName);
                Log.LogDebugMessage("  Items:");

                foreach (ITaskItem item in Items)
                {
                    var absoluteInclude = Path.GetFullPath(item.ItemSpec);
                    Log.LogDebugMessage($"    <{ItemType} Include=\"{absoluteInclude}\">");
                    XmlElement itemElement = doc.CreateElement(ItemType, MSBuildNamespace);
                    XmlAttribute a = doc.CreateAttribute("Include");
                    a.Value = absoluteInclude;
                    itemElement.Attributes.Append(a);
                    IDictionary customMetadata = item.CloneCustomMetadata();

                    var currDir = Canonicalize(Environment.CurrentDirectory);
                    if (!Path.IsPathRooted(item.ItemSpec) 
                        && Canonicalize(Path.GetDirectoryName(Path.GetFullPath(item.ItemSpec))).ToLowerInvariant() != currDir.ToLowerInvariant() 
                        && !customMetadata.Contains(LinkMetadataName)) {

                        var md = doc.CreateElement(LinkMetadataName, MSBuildNamespace);
                        md.InnerText = item.ItemSpec;
                        itemElement.AppendChild(md);
                        Log.LogDebugMessage($"      <{LinkMetadataName}>{item.ItemSpec}</{LinkMetadataName}>");
                    }
                    foreach (string name in customMetadata.Keys)
                    {
                        var value = item.GetMetadata(name);
                        Log.LogDebugMessage($"      <{name}>{value}</{name}>");
                        XmlElement md = doc.CreateElement(name, MSBuildNamespace);
                        md.InnerText = value;
                        itemElement.AppendChild(md);
                    }
                    group.AppendChild(itemElement);
                    Log.LogDebugMessage($"    </{ItemType}>");
                }

                doc.Save(Output);

                return true;
            }
            catch (Exception e)
            {
                Log.LogError("{0}", e);
                return false;
            }
        }

        static string Canonicalize(string path)
        {
            return new Uri(path).LocalPath;
        }

        public void Cancel()
        {
            throw new NotImplementedException();
        }

    }
}
