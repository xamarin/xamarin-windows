//
// ProjectInterfaceConverters.cs
//
// Authors:
//       Jonathan Pobst <jpobst@novell.com>
//
// Copyright 2010 Novell Inc. All rights reserved.
//

using System;

namespace Xamarin.Windows
{
    public class ProjectPageGuids
    {
        // Config Independent
        public static Guid CSharpBuildPage = new Guid("a54ad834-9219-4aa6-b589-607af21c3e26");
        public static Guid DebugPage = new Guid("6185191f-1008-4fb2-a715-3a4e4f27e610");
        public static Guid CodeAnalysisPage = new Guid("984ae51a-4b21-44e7-822c-dd5e046893ef");

        // Config Dependent
        public static Guid BuildEventsPage = new Guid("1e78f8db-6c07-4d61-a18f-7514010abd56");
        public static Guid CSharpApplicationPage = new Guid("5e9a8ac2-4f34-4521-858f-4c248ba31532");
        public static Guid VBApplicationPage = new Guid("8998e48e-b89a-4034-b66e-353d8c1fdc2e");
        public static Guid ServicesPage = new Guid("43e38d2e-43b8-4204-8225-9357316137a4");
        public static Guid ReferencePathsPage = new Guid("031911c8-6148-4e25-b1b1-44bca9a0c45c"); // C# Only
        public static Guid SigningPage = new Guid("f8d6553f-f752-4dbf-acb6-f291b744a792");
        public static Guid SecurityPage = new Guid("df8f7042-0bb1-47d1-8e6d-deb3d07698bd");
        public static Guid PublishPage = new Guid("cc4014f5-b18d-439c-9352-f99d984cca85");
        public static Guid MyExtensionsPage = new Guid("f24459fc-e883-4a8e-9da2-aef684f0e1f4");

        // I don't know
        public static Guid VBWPFApplicationPage = new Guid("00aa1f44-2ba3-4eaa-b54a-ce18000e6c5d");
        public static Guid ReferencesPage = new Guid("4e43f4ab-9f03-4129-95bf-b8ff870af6ab"); // VB Only
        public static Guid WPFSecurityPage = new Guid("00a2c8fe-3844-41be-9637-167454a7f1a7");
        public static Guid VBCompilePage = new Guid("eda661ea-dc61-4750-b3a5-f6e9c74060f5");
        public static Guid SqlDatabasePage = new Guid("87f6adce-9161-489f-907e-3930a6429609"); // SQL Server Projects
        public static Guid SqlDeployPage = new Guid("29ab1d1b-10e8-4511-a362-ef1571b8443c"); // SQL Server Projects
        public static Guid SmartDeviceDevicesPage = new Guid("7b74aadf-aca4-410e-8d4b-afe119835b99");
        public static Guid SmartDeviceDebugPage = new Guid("ac5faec7-d452-4ac1-bc44-2d7ece6df06c");
        public static Guid ResourceEditorPage = new Guid("ff4d6aca-9352-4a5f-821e-f4d6ebdcab11");
        public static Guid SettingsDesignerPage = new Guid("6d2695f9-5365-4a78-89ed-f205c045bfe6");
    }
}
