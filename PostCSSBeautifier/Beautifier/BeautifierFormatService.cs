using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using PostCSSBeautifier.Compiler;
using PostCSSBeautifier.Tagger;
using System;

namespace PostCSSBeautifier
{
	public interface IDocumentFormatter
	{
		void Format(Document document);
	}

	public interface IDocumentFilter
	{
		bool IsAllowed(Document document);
	}

	internal class BeautifierFormatService
	{
		private readonly DTE dte;
		private readonly IDocumentFormatter formatter;

		public BeautifierFormatService(DTE dte)
		{
			this.dte = dte;

			formatter = new VisualStudioCommandFormatter(dte);
		}

		public void FormatDocument(Document doc)
		{
			try
			{
				formatter.Format(doc);
			}
			catch (Exception)
			{
			} // Do not do anything here on purpose.
		}
	}

	public class VisualStudioCommandFormatter : IDocumentFormatter
	{
		private readonly DTE dte;

		public VisualStudioCommandFormatter(DTE dte)
		{
			if (ReferenceEquals(null, dte)) throw new ArgumentNullException(nameof(dte));

			this.dte = dte;
		}

		public void Format(Document document)
		{
			if (BeautifierTagger.CurrentContentType == null)
			{
				return;
			}

			var typeName = BeautifierTagger.CurrentContentType.TypeName.ToUpper();
			if (typeName != "CSS" && typeName != "SCSS")
			{
				return;
			}

			var currentDoc = dte.ActiveDocument;

			document.Activate();

			var txtDoc = document.Object() as TextDocument;
			if (txtDoc != null)
			{
				var textManager = (IVsTextManager) Package.GetGlobalService(typeof(SVsTextManager));
				IVsTextView textView;
				textManager.GetActiveView(1, null, out textView);

				int caretLine;
				int caretCol;
				textView.GetCaretPos(out caretLine, out caretCol);

				int firstVisibleUnit;
				int ignore;
				textView.GetScrollInfo(1, out ignore, out ignore, out ignore, out firstVisibleUnit);

				var startPt = txtDoc.StartPoint.CreateEditPoint();
				var docText = startPt.GetText(txtDoc.EndPoint);

				var result = BeautifierCompiler.ProcessString(docText, BeautifierTagger.CurrentContentType);

				// ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
				const vsEPReplaceTextOptions flags = vsEPReplaceTextOptions.vsEPReplaceTextTabsSpaces |
				                                     vsEPReplaceTextOptions.vsEPReplaceTextNormalizeNewlines;

				if (result != docText)
				{
					startPt.ReplaceText(txtDoc.EndPoint, result, (int) flags);

					textView.SetCaretPos(caretLine, 0);
					textView.SetScrollPosition(1, firstVisibleUnit);
				}
			}

			currentDoc.Activate();
		}
	}
}
