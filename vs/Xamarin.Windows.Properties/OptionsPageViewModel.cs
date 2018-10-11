// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Windows.Properties
{
    public class OptionsPageViewModel : ViewModelBase
    {
        public string AdditionalAotArguments {
            get { return GetPropertyValue<string>(); }
            set { SetPropertyValue<string>(value); }
        }

        public string AdditionalMonoOptions {
            get { return GetPropertyValue<string>(); }
            set { SetPropertyValue<string>(value); }
        }
        public string MonoLogLevel {
            get { return GetPropertyValue<string>(); }
            set { SetPropertyValue<string>(value); }
        }
        public string MonoLogMask {
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

        public bool EnableAotMode {
            get { return GetPropertyValue<bool>(); }
            set { SetPropertyValue<bool>(value); }
        }
 
    }
}
