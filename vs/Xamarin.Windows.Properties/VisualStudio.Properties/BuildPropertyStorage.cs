// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.
using Clide;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Xamarin.Windows.Properties
{
	public class BuildPropertyStorage : IPropertyStorage
	{
		const string PlatformPropertyName = "Platform";
		const string ConfigurationPropertyName = "Configuration";

		readonly Dictionary<string, string> pendingValuesToBePersisted = new Dictionary<string, string>();
		readonly Dictionary<string, string> pendingUserValuesToBePersisted = new Dictionary<string, string>();
		readonly bool commitChangesImmediately;
		readonly string configName = null;
		readonly JoinableLazy<Project> project;
		readonly JoinableLazy<IProjectNode> projectNode;

		string configurationName;
		string platformName;

		public BuildPropertyStorage(IVsHierarchy hierarchy, string configName = null, bool commitChangesImmediately = true)
		{
			this.configName = configName;
			this.commitChangesImmediately = commitChangesImmediately;

			project = new JoinableLazy<Project>(() =>
			{
				var dteProject = hierarchy.AsDteProject();

				if (dteProject != null)
					return ProjectCollection.GlobalProjectCollection.GetLoadedProjects(dteProject.FullName).FirstOrDefault();

				return null;
			}, executeOnMainThread: true);

			projectNode = new JoinableLazy<IProjectNode>(() =>
			{
				var dteProject = hierarchy.AsDteProject();

				if (dteProject != null)
					return dteProject.AsProjectNode();

				return null;
			}, executeOnMainThread: true);

			if (!string.IsNullOrEmpty(configName))
			{
				var values = configName.Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
				if (values.Any())
					configurationName = values[0].Trim();
				if (values.Length > 1)
					platformName = values[1].Trim();
			}
		}

		Project Project => project.GetValue();

		IProjectNode ProjectNode => projectNode.GetValue();

		public string Condition { get; set; }

		public T GetPropertyValue<T>(string propertyName)
		{
			return GetPropertyValue<T>(propertyName, default(T));
		}

		public T GetPropertyValue<T>(string propertyName, T defaultValue)
		{
			object value = null;

			string valueAsString;

			// Configuration and Platform are special cases because they can be changed/switched
			// by the Configuration/Platform selector in the property page and these changes are not
			// propagated to the .csproj
			if (propertyName == ConfigurationPropertyName && !string.IsNullOrEmpty(configurationName))
			{
				valueAsString = configurationName;
			}
			else if (propertyName == PlatformPropertyName && !string.IsNullOrEmpty(platformName))
			{
				valueAsString = platformName;
			}
			else if (!pendingValuesToBePersisted.TryGetValue(propertyName, out valueAsString))
			{
				var group = GetSelectedPropertyGroup();

				if (group != null)
				{
					var property = group.Properties.FirstOrDefault(x => x.Name == propertyName);
					if (property == null)
						// Search in the global section
						property = Project.Xml.PropertyGroups.FirstOrDefault()?.Properties.FirstOrDefault(x => x.Name == propertyName);

					if (property != null)
						valueAsString = property.Value;
				}
			}

			if (valueAsString != null)
			{
				if (typeof(T) != typeof(string))
					value = TypeDescriptor
						.GetConverter(typeof(T))
						.ConvertFromString(valueAsString);
				else
					value = valueAsString;
			}

			return value != null ? (T)value : defaultValue;
		}

		public void SetPropertyValue<T>(T propertyValue, string propertyName)
		{
			var value = string.Empty;
			if (propertyValue != null)
			{
				if (propertyValue is String)
				{
					value = propertyValue.ToString();
				}
				else
				{
					value = TypeDescriptor
						.GetConverter(propertyValue.GetType())
						.ConvertToString(propertyValue);

					if (propertyValue is Boolean)
						value = value.ToLowerInvariant();
				}
			}

			pendingValuesToBePersisted[propertyName] = value;

			if (commitChangesImmediately)
				CommitChanges();
		}

		public void SetUserPropertyValue<T>(T propertyValue, string propertyName)
		{
			var value = string.Empty;
			if (propertyValue != null)
			{
				if (propertyValue is String)
				{
					value = propertyValue.ToString();
				}
				else
				{
					value = TypeDescriptor
						.GetConverter(propertyValue.GetType())
						.ConvertToString(propertyValue);

					if (propertyValue is Boolean)
						value = value.ToLowerInvariant();
				}
			}

			pendingUserValuesToBePersisted[propertyName] = value;

			if (commitChangesImmediately)
				CommitChanges();
		}

		public void CommitChanges()
		{
			foreach (var kvp in pendingValuesToBePersisted)
				CommitChange(kvp.Key, kvp.Value);

			foreach (var kvp in pendingUserValuesToBePersisted)
				CommitUserPropertyChange(kvp.Key, kvp.Value);

			pendingValuesToBePersisted.Clear();
			pendingUserValuesToBePersisted.Clear();
		}

		void CommitChange(string propertyName, string value)
		{
			if (Project != null)
			{
				var group = GetSelectedPropertyGroup();
				if (group == null && !string.IsNullOrEmpty(Condition))
				{
					group = Project.Xml.AddPropertyGroup();
					group.Condition = Condition;
				}

				if (group != null)
					group.SetProperty(propertyName, value);
			}
		}

		void CommitUserPropertyChange(string propertyName, string value)
		{
			if (ProjectNode != null)
				ProjectNode.UserProperties[propertyName] = value;
		}

		ProjectPropertyGroupElement GetSelectedPropertyGroup()
		{
			ProjectPropertyGroupElement group;
			if (!string.IsNullOrEmpty(Condition))
			{
				group = Project
					.Xml
					.PropertyGroups
					.Where(x => !string.IsNullOrEmpty(x.Condition))
					.FirstOrDefault(x =>
						string.Equals(
							x.Condition.Replace(" ", string.Empty),
							Condition.Replace(" ", string.Empty),
							StringComparison.OrdinalIgnoreCase));
			}
			else if (!string.IsNullOrEmpty(configName))
			{
				var validGroups = Project.Xml.PropertyGroups.Where(x => !string.IsNullOrEmpty(x.Condition));

				group = validGroups.FirstOrDefault(x => x.Condition.TrimEnd(new char[] { '\'', '"', ' ' }).EndsWith(configName));

				if (group == null)
					// If it is still null, try using the configName without spaces
					// For example "Any CPU" could be "AnyCPU"
					group = validGroups.FirstOrDefault(x => x.Condition.TrimEnd(new char[] { '\'', '"', ' ' }).EndsWith(configName.Replace("Any CPU", "AnyCPU")));
			}
			else
			{
				group = Project.Xml.PropertyGroups.FirstOrDefault();
			}

			return group;
		}

		public T GetUserPropertyValue<T>(string propertyName)
		{
			object value = null;

			if (!pendingUserValuesToBePersisted.TryGetValue(propertyName, out string valueAsString))
			{
				value = ProjectNode.UserProperties[propertyName];
			}

			if (valueAsString != null)
			{
				if (typeof(T) != typeof(string))
					value = TypeDescriptor
						.GetConverter(typeof(T))
						.ConvertFromString(valueAsString);
				else
					value = valueAsString;
			}

			return (T)value;
		}
	}
}