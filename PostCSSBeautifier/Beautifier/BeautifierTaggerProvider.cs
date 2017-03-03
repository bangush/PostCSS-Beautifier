﻿using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace PostCSSBeautifier
{
	[Export(typeof(IViewTaggerProvider))]
    [ContentType("CSS")]
    [TagType(typeof(TextMarkerTag))]
    class BeautifierTaggerProvider : IViewTaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (textView == null)
                return null;

            //provide highlighting only on the top-level buffer
            if (textView.TextBuffer != buffer)
                return null;

            return new BeautifierTagger(textView, buffer) as ITagger<T>;
        }
    }
}
