// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.
using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Xamarin.Windows.Properties
{
    [Guid(Guids.guidPackageString)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideMenuResource("2000", 13)]
    [ProvideObject(typeof(OptionsPage), RegisterUsing = RegistrationMethod.CodeBase)]
    public sealed class VsPackage : AsyncPackage
    {
    }
}
