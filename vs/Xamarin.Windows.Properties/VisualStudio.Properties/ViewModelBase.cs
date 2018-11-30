// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Xamarin.Windows.Properties
{
	public abstract class ViewModelBase : INotifyPropertyChanged, IDataErrorInfo, IDisposable
	{
		bool disposed;

		public event PropertyChangedEventHandler PropertyChanged;
		public event EventHandler<string> ValidationFailed;

		readonly Dictionary<string, List<ValidationResult>> errorSummaryByColumnName = new Dictionary<string, List<ValidationResult>>();
		readonly CompositeDisposable disposables = new CompositeDisposable();

		IPropertyStorage storage;

		// Support for design data context
		public ViewModelBase()
			: this(new InMemoryPropertyStorage())
		{ }

		public ViewModelBase(IPropertyStorage storage)
		{
			// DO NOT USE the property at this stage
			// We want to avoid triggering the OnStorageChange/InvalidaProperties handlers
			// when the instance is being created
			this.storage = storage;
		}

		public IPropertyStorage Storage
		{
			get { return storage; }
			set
			{
				storage = value;

				OnStorageChanged();
				InvalidateProperties();
			}
		}

		protected virtual void OnStorageChanged()
		{
		}

		public bool IsRefreshing { get; set; }

		public virtual string Error
		{
			get
			{
				var result = new StringBuilder();
				foreach (var validationResult in errorSummaryByColumnName.SelectMany(x => x.Value))
					if (!string.IsNullOrEmpty(validationResult.ErrorMessage))
						result.AppendLine(validationResult.ErrorMessage);

				return result.Length == 0 ? null : result.ToString();
			}
		}

		public virtual bool IsValid =>
			!errorSummaryByColumnName.Any(x => x.Value.Any());

		public virtual string this[string columnName]
		{
			get
			{
				string result = null;

				var propertyInfo = GetType().GetTypeInfo().GetProperty(columnName);
				if (propertyInfo != null)
					ValidateProperty(columnName, propertyInfo.GetValue(this), out result);

				return result;
			}
		}

		bool ValidateProperty(string memberName, object value, out string result)
		{
			result = null;

			var validationContext = new ValidationContext(this)
			{
				MemberName = memberName
			};

			var validationResults = new List<ValidationResult>();
			if (!Validator.TryValidateProperty(value, validationContext, validationResults))
				result = string.Join(Environment.NewLine, validationResults.Select(x => x.ErrorMessage));

			errorSummaryByColumnName[memberName] = validationResults;

			return !validationResults.Any();
		}

		protected void SetUserPropertyValue<T>(T value, [CallerMemberName] string memberName = "") =>
			SetUserPropertyValue(value, false, memberName);

		protected void SetUserPropertyValue<T>(T value, bool ignoreValidationResult, string memberName)
		{
			SetPropertyValue(value, ignoreValidationResult, memberName, () => Storage.SetUserPropertyValue(value, memberName));
		}

		protected void SetPropertyValue<T>(T value, [CallerMemberName] string memberName = "") =>
			SetPropertyValue(value, false, memberName);

		protected void SetPropertyValue<T>(T value, bool ignoreValidationResult, string memberName)
		{
			SetPropertyValue(value, ignoreValidationResult, memberName, () => Storage.SetPropertyValue(value, memberName));
		}

		void SetPropertyValue<T>(T value, bool ignoreValidationResult, string memberName, Action setPropertyAction)
		{
			string result = null;
			if (ValidateProperty(memberName, value, out result) || ignoreValidationResult)
			{
				setPropertyAction();
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
			}
			else
			{
				InvalidateProperties();
				ValidationFailed?.Invoke(this, result);
			}
		}

		protected void ResetPropertyValue([CallerMemberName] string memberName = "")
		{
			Storage.SetPropertyValue(string.Empty, memberName);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
		}

		protected T GetPropertyValue<T>([CallerMemberName] string memberName = "") =>
			Storage.GetPropertyValue<T>(memberName);

		protected T GetPropertyValueOrDefault<T>(T defaultValue, [CallerMemberName] string memberName = "")
			=> Storage.GetPropertyValue<T>(memberName, defaultValue);

		protected T GetUserPropertyValue<T>([CallerMemberName] string memberName = "") =>
			Storage.GetUserPropertyValue<T>(memberName);

		public virtual void InvalidateProperties()
		{
			try
			{
				IsRefreshing = true;

				foreach (var provider in itemsProviders)
				{
					provider.Value.Clear();

					foreach (var value in provider.Key.GetItems())
						provider.Value.Add(value);

				}

				var targetProperties = GetType()
					.GetTypeInfo()
					.DeclaredProperties
					.Where(x => x.CanRead && x.CanWrite);

				foreach (var property in targetProperties)
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property.Name));
			}
			finally
			{
				IsRefreshing = false;
			}
		}

		readonly Dictionary<IItemsProvider, ObservableCollection<ItemViewModel>> itemsProviders =
			new Dictionary<IItemsProvider, ObservableCollection<ItemViewModel>>();

		protected void RegisterItemsProvider(IItemsProvider provider, ObservableCollection<ItemViewModel> source)
		{
			itemsProviders.Add(provider, source);
		}

		protected virtual void NotifyPropertyChanged<T>(Expression<Func<T>> expr, bool setRefresingIfNecessary = false)
		{
			// If IsRefreshing is already true we don't have to reset it at the end of this execution
			var resetRefresing = !IsRefreshing && setRefresingIfNecessary;

			try
			{
				if (resetRefresing)
					IsRefreshing = true;

				var memberExpression = expr.Body as MemberExpression;
				if (memberExpression != null)
				{
					var property = memberExpression.Member as PropertyInfo;
					if (property != null)
						PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property.Name));
				}
			}
			finally
			{
				if (resetRefresing)
					IsRefreshing = false;
			}
		}

		protected void NotifyPropertyChanged(params string[] propertyNames)
		{
			foreach (var propertyName in propertyNames)
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public void Dispose()
		{
			Dispose(true);

			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposed)
				return;

			if (disposing)
				disposables.Dispose();

			disposed = true;
		}

		protected virtual void TrackDisposable(IDisposable value) =>
			disposables.Add(value);
	}
}
