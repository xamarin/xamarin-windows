// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.VisualStudio.OLE.Interop;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Merq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Windows.Forms.Integration;
using Microsoft.VisualStudio.Threading;

namespace Xamarin.Windows.Properties
{
	public abstract partial class BasePropertyPage : IPropertyPage2
	{
		IPropertyPageSite pageSite;

		protected readonly ViewModelBase viewModel;
		readonly ElementHost elementHost;
		readonly Lazy<ICommandBus> commandBus;
		readonly Lazy<IEventStream> eventStream;
		readonly JoinableTaskFactory asyncManager;

		IVsUIShell uiShell;

		bool dirty;

		protected string ConfigurationName { get; private set; }

		public BasePropertyPage()
		{
			commandBus = new Lazy<ICommandBus>(() =>
			{
				var componentModel = Package.GetGlobalService(typeof(SComponentModel)) as IComponentModel;

				return componentModel.DefaultExportProvider.GetExportedValue<ICommandBus>();
			});
			eventStream = new Lazy<IEventStream>(() =>
			{
				var componentModel = Package.GetGlobalService(typeof(SComponentModel)) as IComponentModel;

				return componentModel.DefaultExportProvider.GetExportedValue<IEventStream>();
			});

			asyncManager = Clide.ServiceLocator.Global.GetService<JoinableTaskContext>().Factory;

			uiShell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;

			viewModel = CreateViewModel();
			viewModel.PropertyChanged += OnModelChanged;
			viewModel.ValidationFailed += OnValidationFailed;

			var view = CreateView();
			view.DataContext = viewModel;

			elementHost = new ElementHost
			{
				Child = view
			};

			elementHost.Disposed += (sender, e) => viewModel.Dispose();
		}

		void OnValidationFailed(object sender, string error)
		{
			var classId = Guid.Empty;
			var result = 0;

			uiShell.ShowMessageBox(0, ref classId, string.Empty, error, string.Empty, 0, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_CRITICAL, 0, out result);
		}

		protected abstract System.Windows.FrameworkElement CreateView();

		protected abstract ViewModelBase CreateViewModel();

		protected abstract string Title { get; }

		protected ICommandBus CommandBus => commandBus.Value;

		protected IEventStream EventStream => eventStream.Value;

		protected JoinableTaskFactory AsyncManager => asyncManager;

		void OnModelChanged(object sender, PropertyChangedEventArgs e)
		{
			if (!viewModel.IsRefreshing)
			{
				dirty = true;

				if (pageSite != null)
					pageSite.OnStatusChange((uint)(PROPPAGESTATUS.PROPPAGESTATUS_DIRTY));
			}
		}

		public void Activate(IntPtr hWndParent, RECT[] pRect, int bModal)
		{
			Suspend(elementHost);

			if ((null == pRect) || (0 == pRect.Length))
			{
				throw new ArgumentNullException("pRect");
			}

			var parentControl = Control.FromHandle(hWndParent);
			var rect = Rectangle.FromLTRB(pRect[0].left, pRect[0].top, pRect[0].right, pRect[0].bottom);

			elementHost.SetBounds(rect.X, rect.Y, rect.Width, rect.Height);
			elementHost.Parent = parentControl;
		}

		public static void Suspend(Control control)
		{
			Message msgSuspendUpdate = Message.Create(control.Handle, 0, IntPtr.Zero, IntPtr.Zero);

			NativeWindow window = NativeWindow.FromHandle(control.Handle);
			window.DefWndProc(ref msgSuspendUpdate);
		}

		public static void Resume(Control control)
		{
			var wparam = new IntPtr(1);
			Message msgResumeUpdate = Message.Create(control.Handle, 0, wparam, IntPtr.Zero);

			NativeWindow window = NativeWindow.FromHandle(control.Handle);
			window.DefWndProc(ref msgResumeUpdate);

			control.Invalidate();
		}

		public void Deactivate()
		{
		}

		public void GetPageInfo(PROPPAGEINFO[] pPageInfo)
		{
			PROPPAGEINFO pageInfo;

			pageInfo.cb = (uint)Marshal.SizeOf(typeof(PROPPAGEINFO));
			pageInfo.dwHelpContext = 0;
			pageInfo.pszDocString = null;
			pageInfo.pszHelpFile = null;
			pageInfo.pszTitle = Title;
			pageInfo.SIZE.cx = elementHost.Size.Width;
			pageInfo.SIZE.cy = elementHost.Size.Height;

			pPageInfo[0] = pageInfo;
		}

		public void Help(string pszHelpDir)
		{
		}

		public int IsPageDirty() =>
			dirty ? VSConstants.S_OK : VSConstants.S_FALSE;

		public void SetObjects(uint cObjects, object[] ppunk)
		{
			if (ppunk == null || cObjects == 0)
				return;

			foreach (object o in ppunk)
			{
				var cfgBrowseObject = o as IVsCfgBrowseObject;
				if (cfgBrowseObject != null)
				{
					IVsHierarchy hierarchy;
					uint item;

					IVsCfg cfg;
					cfgBrowseObject.GetCfg(out cfg);
					cfgBrowseObject.GetProjectItem(out hierarchy, out item);

					OnHierarchyInitialized(hierarchy);

					string configName;
					cfg.get_DisplayName(out configName);

					ConfigurationName = configName;

					SetPropertyStorage(viewModel, new BuildPropertyStorage(hierarchy, configName));

					dirty = false;
				}
				else
				{

					var browseObject = o as IVsBrowseObject;
					if (o is IVsBrowseObject)
					{
						IVsHierarchy hierarchy;
						uint item;

						browseObject.GetProjectItem(out hierarchy, out item);

						OnHierarchyInitialized(hierarchy);

						SetPropertyStorage(viewModel, new BuildPropertyStorage(hierarchy));

						dirty = false;
					}
				}
			}
		}

		protected virtual void SetPropertyStorage(ViewModelBase viewModel, IPropertyStorage storage)
		{
			viewModel.Storage = storage;
		}

		protected virtual void OnHierarchyInitialized(IVsHierarchy hierarchy)
		{
		}

		public void SetPageSite(IPropertyPageSite pPageSite) => pageSite = pPageSite;

		public void Show(uint nCmdShow)
		{
			switch (nCmdShow)
			{
				case Constants.SW_HIDE:
					elementHost.Hide();
					break;
				case Constants.SW_SHOW:
				case Constants.SW_SHOWNORMAL:
					elementHost.Show();
					Resume(elementHost);
					break;
			}
		}

		public int TranslateAccelerator(Microsoft.VisualStudio.OLE.Interop.MSG[] pMsg)
		{
			var keyboardMessage = Message.Create(pMsg[0].hwnd, (int)pMsg[0].message, pMsg[0].wParam, pMsg[0].lParam);
			int hr = ProcessAccelerator(ref keyboardMessage);
			pMsg[0].lParam = keyboardMessage.LParam;
			pMsg[0].wParam = keyboardMessage.WParam;
			return hr;
		}

		public int ProcessAccelerator(ref Message keyboardMessage)
		{
			Control destinationControl = Control.FromHandle(keyboardMessage.HWnd);
			bool messageProccessed = destinationControl.PreProcessMessage(ref keyboardMessage);
			if (messageProccessed)
				return VSConstants.S_OK;
			else
				return VSConstants.S_FALSE;
		}

		public void Move(Microsoft.VisualStudio.OLE.Interop.RECT[] pRect)
		{
			var rect = Rectangle.FromLTRB(pRect[0].left, pRect[0].top, pRect[0].right, pRect[0].bottom);
			elementHost.Location = new Point(rect.X, rect.Y);
			elementHost.Size = new Size(rect.Width, rect.Height);
		}

		int IPropertyPage.Apply() =>
			Apply() ? VSConstants.S_OK : VSConstants.E_FAIL;

		void IPropertyPage2.Apply() =>
			Apply();

		protected virtual bool Apply()
		{
			var isValid = viewModel.IsValid;

			if (isValid)
				dirty = false;

			return isValid;
		}

		public void EditProperty(int DISPID)
		{
		}
	}
}
