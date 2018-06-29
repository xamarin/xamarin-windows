// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Xamarin.Windows.Properties
{
	public interface IPropertyStorage
	{
		T GetPropertyValue<T>(string propertyName);
		T GetPropertyValue<T>(string propertyName, T defaultValue);
		T GetUserPropertyValue<T>(string propertyName);
		void SetPropertyValue<T>(T value, string propertyName);
		void SetUserPropertyValue<T>(T propertyValue, string propertyName);

	}
}
