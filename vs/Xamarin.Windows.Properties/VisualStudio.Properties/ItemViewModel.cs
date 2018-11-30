// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xamarin.Windows.Properties
{
	public class ItemViewModel
	{
		public ItemViewModel()
		{ }

		public ItemViewModel(string displayName, string value, string group = "")
		{
			DisplayName = displayName;
			Value = value;
			Group = group;
		}

		public string DisplayName { get; set; }
		public string Value { get; set; }
		public string Group { get; set; }
		public bool IsDefault { get; set; }
		public object Data { get; set; }
	}
}
