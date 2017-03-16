//------------------------------------------------------------------------------
// <copyright file="CSSBraid.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;

namespace PostCSSBeautifier.Tagger
{
	/// <summary>
	/// Classifier that classifies all text as an instance of the "CSSBraid" classification type.
	/// </summary>
	public class BeautifierTagger : ITagger<TextMarkerTag>
	{
		private ITextView View { get; }
		public static IContentType CurrentContentType { get; set; }

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		internal BeautifierTagger(ITextView view, ITextBuffer sourceBuffer)
		{
			this.View = view;

			view.GotAggregateFocus += OnViewGotFocus;
			BeautifierTagger.CurrentContentType = sourceBuffer.ContentType;
		}

		private void OnViewGotFocus(object obj, EventArgs e)
		{
			BeautifierTagger.CurrentContentType = this.View.TextBuffer.ContentType;
		}

		public IEnumerable<ITagSpan<TextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			yield break;
		}

	}
}