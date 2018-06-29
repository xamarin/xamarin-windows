// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.
using System;

namespace Xamarin.Windows.Properties
{
    static class Guids
    {
        // NOTE: changing the package GUID depending on the solution being built is 
        // the only reliable way (in addition to changing the extension assembly 
        // name itself) to guarantee that VS will be able to load them side-by-side.
        public const string PackageGuidString = "0610d5ad-36f3-4a90-a0ba-55736bf813c1";
        public const string guidPackageString = PackageGuidString;
        //        public const string guidCommandSetString = ThisAssembly.Vsix.CommandSetGuid;
        public const string WindowsBindingProjectTypeGuid = "8F3E2DF0-C35C-4265-82FC-BEA011F4A7ED";

        public const string OptionsPageGuidString = "9889e6f4-595b-4f13-8913-2c3acb3263e0";
        public static readonly Guid guidPackage = new Guid(guidPackageString);

    };
}