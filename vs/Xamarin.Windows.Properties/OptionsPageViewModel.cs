// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Windows.Properties
{
    public class OptionsPageViewModel : ViewModelBase
    {
        public OptionsPageViewModel() {
            EnableJitMode = true;
        }

        public string AdditionalAotOptions {
            get { return GetPropertyValue<string>(); }
            set { SetPropertyValue<string>(value); }
        }

        public bool GenerateDebuggableAotModules {
            get { return GetPropertyValue<bool>(); }
            set { SetPropertyValue<bool>(value); }
        }

        public bool BundleAssemblies {
            get { return GetPropertyValue<bool>(); }
            set { SetPropertyValue<bool>(value); }
        }

        public bool EnableJitMode {
            get { return GetPropertyValue<bool>(); }
            set { SetPropertyValue<bool>(value); }
        }

        public bool EnableAotMode {
            get { return !EnableJitMode; }
            set { EnableJitMode = !value; }
        }
 
    }
}
