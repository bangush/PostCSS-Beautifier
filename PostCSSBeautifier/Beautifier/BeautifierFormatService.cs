using DiffPlex;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using PostCSSBeautifier.Compiler;
using PostCSSBeautifier.Tagger;
using System;
using System.Collections.Generic;

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
			catch (Exception e)
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
			if (BeautifierTagger.CurrentContentType == null || !SettingsPage.IsEnabled)
				return;

			var typeName = BeautifierTagger.CurrentContentType.TypeName.ToUpper();
			if (typeName != "CSS" && typeName != "SCSS")
				return;

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
				const int flags = (int) (vsEPReplaceTextOptions.vsEPReplaceTextTabsSpaces |
				                         vsEPReplaceTextOptions.vsEPReplaceTextNormalizeNewlines);

				if (result != docText)
				{
					var differ = new Differ();
					var diffResult = differ.CreateLineDiffs(docText, result, false);

					var beginEditPoint = txtDoc.StartPoint.CreateEditPoint();
					var endEditPoint = txtDoc.StartPoint.CreateEditPoint();
					var lineCountChange = 0;

					txtDoc.DTE.UndoContext.Open("PostCSS Format");
					foreach (var block in diffResult.DiffBlocks)
					{
						var startLine = block.DeleteStartA + lineCountChange + 1;
						var endLine = block.DeleteStartA + block.DeleteCountA + lineCountChange + 1;
						lineCountChange += (block.InsertCountB - block.DeleteCountA);


						//beginEditPoint.ReplaceText(endEditPoint, "", flags);

						if (block.InsertCountB > 0)
						{
							beginEditPoint.MoveToLineAndOffset(startLine, 1);
							endEditPoint.MoveToLineAndOffset(endLine, 1);

							var newLines = new List<string>();
							for (var i = 0; i < block.InsertCountB; i++)
							{
								var lineIndex = i + block.InsertStartB;
								var line = diffResult.PiecesNew[lineIndex];
								newLines.Add(line);
							}

							var newText = string.Join("\r\n", newLines) + "\r\n";

							beginEditPoint.ReplaceText(endEditPoint, newText, flags);
						}
						else if (block.DeleteCountA > 0)
						{
							beginEditPoint.MoveToLineAndOffset(startLine, 1);
							endEditPoint.MoveToLineAndOffset(startLine + block.DeleteCountA, 1);

							beginEditPoint.ReplaceText(endEditPoint, "", flags);
						}
					}
					txtDoc.DTE.UndoContext.Close();

					//startPt.ReplaceText(txtDoc.EndPoint, result, flags);

					textView.SetCaretPos(caretLine, 0);
					textView.SetScrollPosition(1, firstVisibleUnit);
				}
			}

			currentDoc.Activate();
		}

	}
}