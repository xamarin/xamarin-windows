// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.
using System;
using System.Runtime.InteropServices;
using System.Windows;
using Xamarin.Windows.Properties.Properties;

namespace Xamarin.Windows.Properties
{
    [Guid(Guids.OptionsPageGuidString)]
    public class OptionsPage : BasePropertyPage
    {
        protected OptionsPageViewModel model;

        protected override string Title => Resources.OptionsTitle;

        protected override FrameworkElement CreateView() => new OptionsPageView();

        protected override ViewModelBase CreateViewModel() { return model = new OptionsPageViewModel// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.
(); }


    }
}
