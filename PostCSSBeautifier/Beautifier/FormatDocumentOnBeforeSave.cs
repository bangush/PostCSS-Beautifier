﻿using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Linq;

namespace PostCSSBeautifier
{

	internal class FormatDocumentOnBeforeSave : IVsRunningDocTableEvents3
	{
		private readonly DTE dte;
		private readonly RunningDocumentTable runningDocumentTable;
		private readonly BeautifierFormatService documentFormatter;

		public FormatDocumentOnBeforeSave(DTE dte, RunningDocumentTable runningDocumentTable, BeautifierFormatService documentFormatter)
		{
			this.runningDocumentTable = runningDocumentTable;
			this.documentFormatter = documentFormatter;
			this.dte = dte;
		}

		public int OnBeforeSave(uint docCookie)
		{
			var document = FindDocument(docCookie);

			if (document == null)
				return VSConstants.S_OK;

			documentFormatter.FormatDocument(document);

			return VSConstants.S_OK;
		}

		private Document FindDocument(uint docCookie)
		{
			var documentInfo = runningDocumentTable.GetDocumentInfo(docCookie);
			var documentPath = documentInfo.Moniker;

			return dte.Documents.Cast<Document>().FirstOrDefault(doc => doc.FullName == documentPath);
		}

		public int OnAfterFirstDocumentLock(uint docCookie, uint dwRdtLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
		{
			return VSConstants.S_OK;
		}

		public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRdtLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
		{
			return VSConstants.S_OK;
		}

		public int OnAfterSave(uint docCookie)
		{
			return VSConstants.S_OK;
		}

		public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
		{
			return VSConstants.S_OK;
		}

		public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
		{
			return VSConstants.S_OK;
		}

		public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
		{
			return VSConstants.S_OK;
		}

		int IVsRunningDocTableEvents3.OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld,
			string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
		{
			return VSConstants.S_OK;
		}


		int IVsRunningDocTableEvents2.OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld,
			string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
		{
			return VSConstants.S_OK;
		}
	}
}
