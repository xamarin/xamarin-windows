// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using EnvDTE;
using Mono.Debugging.Soft;
using Mono.Debugging.VisualStudio;

namespace Xamarin.Windows
{
    internal class XamarinWindowsStartInfo : StartInfo
    {
        public XamarinWindowsStartInfo(SoftDebuggerStartArgs args, DebuggingOptions options, Project startupProject) : base(args, options, startupProject)
        {
        }
    }
}