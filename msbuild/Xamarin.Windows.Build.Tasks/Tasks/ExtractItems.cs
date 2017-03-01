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

                foreach (ITaskItem item in Items)
                {
                    XmlElement itemElement = doc.CreateElement(ItemType, MSBuildNamespace);
                    XmlAttribute a = doc.CreateAttribute("Include");
                    a.Value = item.ItemSpec;
                    itemElement.Attributes.Append(a);
                    IDictionary customMetadata = item.CloneCustomMetadata();
                    
                    foreach (string name in customMetadata.Keys)
                    {
                        XmlElement md = doc.CreateElement(name, MSBuildNamespace);
                        md.InnerText = item.GetMetadata(name);
                        itemElement.AppendChild(md);
                    }
                    group.AppendChild(itemElement);
                }

                doc.Save(Output);

                Log.LogDebugMessage("ExtractItems Task");
                Log.LogDebugMessage("  ItemType: " + ItemType);
                Log.LogDebugMessage("  Output: " + Output);
                Log.LogDebugTaskItems("  Items:", Items);

                return true;
            }
            catch (Exception e)
            {
                Log.LogError("{0}", e);
                return false;
            }
        }

        public void Cancel()
        {
            throw new NotImplementedException();
        }

    }
}
