﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Xamarin.Windows.Build.Tests.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Xamarin.Windows.Build.Tests.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;?xml version=&quot;1.0&quot; encoding=&quot;utf-8&quot;?&gt;
        ///&lt;Project xmlns=&quot;http://schemas.microsoft.com/developer/msbuild/2003&quot;&gt;
        ///  &lt;ItemGroup&gt;
        ///    &lt;Items1 Include=&quot;$(TestProjects)\ConsoleApp\a.foo&quot; /&gt;
        ///    &lt;Items1 Include=&quot;$(TestProjects)\ConsoleApp\b.foo&quot; /&gt;
        ///  &lt;/ItemGroup&gt;
        ///  &lt;ItemGroup&gt;
        ///    &lt;Items2 Include=&quot;$(TestProjects)\ConsoleApp\a.foo&quot;&gt;
        ///      &lt;SomeMetadata&gt;DefaultValue&lt;/SomeMetadata&gt;
        ///    &lt;/Items2&gt;
        ///    &lt;Items2 Include=&quot;$(TestProjects)\ConsoleApp\b.foo&quot;&gt;
        ///      &lt;SomeMetadata&gt;DefaultValue&lt;/SomeMetadata&gt;
        ///    &lt;/Items2&gt;
        ///    &lt;Ite [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string ExtractItemsTestExpected1 {
            get {
                return ResourceManager.GetString("ExtractItemsTestExpected1", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;?xml version=&quot;1.0&quot; encoding=&quot;utf-8&quot;?&gt;
        ///&lt;Project xmlns=&quot;http://schemas.microsoft.com/developer/msbuild/2003&quot;&gt;
        ///  &lt;ItemGroup&gt;
        ///    &lt;Items1 Include=&quot;$(TestProjects)\ConsoleApp\a.foo&quot; /&gt;
        ///    &lt;Items1 Include=&quot;$(TestProjects)\ConsoleApp\b.foo&quot; /&gt;
        ///  &lt;/ItemGroup&gt;
        ///  &lt;ItemGroup&gt;
        ///    &lt;Items2 Include=&quot;$(TestProjects)\ConsoleApp\a.foo&quot;&gt;
        ///      &lt;SomeMetadata&gt;DefaultValue&lt;/SomeMetadata&gt;
        ///    &lt;/Items2&gt;
        ///    &lt;Items2 Include=&quot;$(TestProjects)\ConsoleApp\b.foo&quot;&gt;
        ///      &lt;SomeMetadata&gt;DefaultValue&lt;/SomeMetadata&gt;
        ///    &lt;/Items2&gt;
        ///    &lt;Ite [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string ExtractItemsTestExpected2 {
            get {
                return ResourceManager.GetString("ExtractItemsTestExpected2", resourceCulture);
            }
        }
    }
}
