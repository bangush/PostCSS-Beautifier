//------------------------------------------------------------------------------
// <copyright file="CSSBraid.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using PostCSSBeautifier.Helpers;
using PostCSSBeautifier.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace PostCSSBeautifier
{
	/// <summary>
	/// Classifier that classifies all text as an instance of the "CSSBraid" classification type.
	/// </summary>
	public class BeautifierTagger : ITagger<TextMarkerTag>
	{
		public static IContentType CurrentContentType { get; set; }
		private int OldPosition { get; set; }
		private ITextView View { get; }
		private ITextBuffer SourceBuffer { get; }
		private SnapshotPoint? CurrentChar { get; set; }
		private IContentType ContentType { get; }
		private Timer Timer { get; set; }
		private bool IsTiming { get; set; }

		private string CurrentComment { get; set; }
		public static string DefaultCombConfigPath = Path.Combine(NodePaths.Resources, @"postcss.config.js");
		public static string CombConfigPath { get; private set; } = DefaultCombConfigPath;

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		internal BeautifierTagger(ITextView view, ITextBuffer sourceBuffer)
		{
			this.View = view;
			this.SourceBuffer = sourceBuffer;
			this.CurrentChar = null;
			this.ContentType = this.SourceBuffer.ContentType;
			BeautifierTagger.CurrentContentType = this.ContentType;

			//this.View.LayoutChanged += ViewLayoutChanged;

			var settings = new Settings();
			var settingsConfigPath = settings.BeautifierJSONConfigPath;
			BeautifierTagger.RefreshConfig(settingsConfigPath);
		}

		private async void ViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
		{
			var currentPosition = View.Caret.Position.BufferPosition.Position;
			var diff = this.OldPosition - currentPosition;
			if (Math.Abs(diff) > 2)
			{
				this.OldPosition = currentPosition;
				return;
			}

			if (e.NewSnapshot.GetText() != e.OldSnapshot.GetText()) //make sure that there has really been a change
			{
				await UpdateAtCaretPosition(View.Caret.Position);
			}

			this.OldPosition = currentPosition;
		}

		private async Task UpdateAtCaretPosition(CaretPosition caretPosition)
		{
			CurrentChar = caretPosition.Point.GetPoint(SourceBuffer, caretPosition.Affinity);

			if (!CurrentChar.HasValue)
				return;

			try
			{
				var lastChar = CurrentChar.Value - 1;
				var secLastChar = CurrentChar.Value - 2;
				var lastCharVal = lastChar.GetChar();
				var secLastCharVal = secLastChar.GetChar();

				if (lastCharVal == '}' && secLastCharVal != '{' && !this.IsTiming)
				{
					SnapshotSpan fullDeclarationSpan;
					var endBracePosition = new SnapshotPoint(this.View.TextSnapshot, lastChar.Position + 1);
					FindMatchingOpeningBrace(endBracePosition, out fullDeclarationSpan);

					await ProcessSpan(fullDeclarationSpan);
				}

			}
			catch (Exception e)
			{
				Logger.Log(e);
			}

		}

		public async Task ProcessSpan(SnapshotSpan span)
		{
			var result = await Compiler.Beautifier.ProcessString(span.GetText(), this.ContentType);

			this.IsTiming = true;
			Timer = new Timer
			{
				Interval = 1000,
				Enabled = true
			};
			Timer.Elapsed += OnTimedEvent;

			result = result.Replace(@"\r\n|\n\r|\n|\r", "\r\n").TrimEnd();

			this.SourceBuffer.Replace(span, result);
			OutputWriter.Write(result);
		}

		private void OnTimedEvent(object source, ElapsedEventArgs e)
		{
			this.IsTiming = false;
			Timer.Stop();
		}

		public IEnumerable<ITagSpan<TextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			yield break;
		}


		private void FindMatchingOpeningBrace(SnapshotPoint endBracePosition, out SnapshotSpan fullDeclarationSpan)
		{
			fullDeclarationSpan = new SnapshotSpan(endBracePosition, endBracePosition);

			var snapshot = endBracePosition.Snapshot;
			var snapshotText = snapshot.GetText();
			var initialOffset = (endBracePosition - 1).Position;
			var offset = initialOffset; //move the offset to the character before this one

			var openCount = 0;
			while (true)
			{
				var currentChar = snapshotText[offset];
				if (currentChar != '{')
				{
					if (offset < 0)
					{
						return;
					}

					if (currentChar == '}' && offset != initialOffset)
					{
						openCount++;
					}
				}
				else
				{
					if (openCount <= 0)
					{
						var braceToBraceSpan = new SnapshotSpan(new SnapshotPoint(snapshot, offset), endBracePosition);
						fullDeclarationSpan = FindBeginningOfSelector(braceToBraceSpan);
						return;
					}

					openCount--;
				}

				offset--;
			}
		}

		private SnapshotSpan FindBeginningOfSelector(SnapshotSpan braceToBraceSpan)
		{
			var snapshot = braceToBraceSpan.Snapshot;
			var snapshotText = snapshot.GetText();
			var offset = braceToBraceSpan.Start.Position;
			var initialOffset = offset;
			var end = braceToBraceSpan.End;

			while (true)
			{
				var currentChar = snapshotText[offset];
				var currentLine = snapshot.GetLineFromPosition(offset);
				var positionInLine = offset - currentLine.Start.Position;
				var isPartOfComment = this.DetermineCommentSituation(currentChar, currentLine, positionInLine);
				if (isPartOfComment)
				{
					offset--;
					continue;
				}

				var foundVarWrapper = currentChar == '{' && snapshotText[offset - 1] == '#';
				if (foundVarWrapper)
				{
					throw new Exception("Variable wrapper found, aborting");
				}

				var foundBrace = currentChar == '{' || currentChar == '}' || currentChar == ';';
				if ((foundBrace && offset != initialOffset) || offset <= 0)
				{
					var startPos = foundBrace ? offset + 1 : 0;
					return new SnapshotSpan(new SnapshotPoint(snapshot, startPos), end);
				}

				offset--;
			}
		}

		private bool DetermineCommentSituation(char currentChar, ITextSnapshotLine line, int currentCharPositionInLine)
		{
			// Matched a / character but have not detected that a comment is made yet
			var couldBeCommentStart = currentChar == '/' && string.IsNullOrEmpty(this.CurrentComment);

			// We had matched a / character last time, but this character was not a *
			var isntCommentAfterAll = this.CurrentComment == "/" && currentChar != '*';

			// We matched a */ 
			var isDefinitelyCommentStart = currentChar == '*' && this.CurrentComment == "/";

			// This char is /, and the existing CurrentComment string is *SOME_TEXT*/
			var isEndOfComment = currentChar == '/' && !string.IsNullOrEmpty(this.CurrentComment) && this.CurrentComment.Substring(0, 1) == "*";

			// this.CurrentComment is a full comment (meaning it is /*SOME_TEXT*/
			var commentIsOver = this.CurrentComment != null && this.CurrentComment.Length >= 2 && this.CurrentComment.Substring(0, 2) == "/*";

			// Matched any text as long as we know it could be a comment
			var commentStillGoing = !commentIsOver && !isntCommentAfterAll && !string.IsNullOrEmpty(this.CurrentComment);

			if (!commentStillGoing)
			{
				var doubleSlashPosition = line.GetText().IndexOf("//", StringComparison.Ordinal);
				var hasSingleLineComment = doubleSlashPosition >= 0;
				if (hasSingleLineComment)
				{
					return doubleSlashPosition <= currentCharPositionInLine;
				}
			}

			if (couldBeCommentStart)
			{
				this.CurrentComment = "/";
			}
			else if (isDefinitelyCommentStart || isEndOfComment || commentStillGoing)
			{
				this.CurrentComment = currentChar + this.CurrentComment;
			}
			else if (commentIsOver || isntCommentAfterAll)
			{
				this.CurrentComment = null;
			}

			return !string.IsNullOrEmpty(this.CurrentComment);
		}

		public static void RefreshConfig(string path)
		{
			var pathIsSet = path != @"C:\some_path\your_file.json" && path != "";

			var config = "";
			var hadError = false;
			if (pathIsSet)
			{
				try
				{
					TextReader tr = new StreamReader(path);
					config = tr.ReadToEnd();
				}
				catch (Exception)
				{
					hadError = true;

					MessageBox.Show(string.Format("VS CSS Process attempted to load a configuration file at '{0}', " +
					                              "but an error occured. Please ensure that a .json file exists at " +
					                              "that path.\r\n\r\nWe will use the default configuration until you " +
					                              "fix the path. Visit http://csscomb.com/config to learn more about " +
					                              "how to create CSSComb configuration.", path), "VS CSS Process - Error");
				}
			}

			BeautifierTagger.CombConfigPath = pathIsSet ? path : DefaultCombConfigPath;
		}
	}
}