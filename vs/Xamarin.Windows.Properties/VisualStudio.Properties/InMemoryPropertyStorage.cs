// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Xamarin.Windows.Properties
{
	public class InMemoryPropertyStorage : IPropertyStorage
	{
		Dictionary<string, object> values = new Dictionary<string, object>();

		public T GetPropertyValue<T>(string propertyName)
		{
			object value;
			if (values.TryGetValue(propertyName, out value))
				return (T)value;

			return default(T);
		}

		public T GetPropertyValue<T>(string propertyName, T defaultValue)
		{
			object value;
			if (values.TryGetValue(propertyName, out value))
				return (T)value;

			return defaultValue;
		}

		public void SetPropertyValue<T>(T value, string propertyName) =>
			values[propertyName] = value;

		public T GetUserPropertyValue<T>(string propertyName) => throw new NotImplementedException();

		public void SetUserPropertyValue<T>(T propertyValue, string propertyName) => throw new NotImplementedException();
	}
}
