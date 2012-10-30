using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
	[Import]
	public class Input : HtmlElement
	{
        public Input(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""INPUT""); }")]
        extern public Input();

		extern public string Type { get; set; }		
		extern public string Value { get; set; }		
		extern public string Name { get; set; }		
		extern public bool Status { get; set; }		
		extern public Form Form { get; }		
		extern public int Size { get; set; }		
		extern public int MaxLength { get; set; }		
		extern public string DefaultValue { get; set; }		
		extern public bool ReadOnly { get; set; }		
		extern public bool Indeterminate { get; set; }		
		extern public bool DefaultChecked { get; set; }		
		extern public bool Checked { get; set; }		
		extern public string Border { get; set; }		
		extern public int Vspace { get; set; }		
		extern public int Hspace { get; set; }		
		extern public string Alt { get; set; }		
		extern public string Src { get; set; }		
		extern public string Lowsrc { get; set; }		
		extern public string Vrml { get; set; }		
		extern public string Dynsrc { get; set; }		
		extern public bool Complete { get; }		
		extern public string Loop { get; set; }		
		extern public string Align { get; set; }		
		extern public int Width { get; set; }		
		extern public int Height { get; set; }

        [Import("Start")]
		extern public string Start { get; set; }		

		extern public string Accept { get; set; }		
		extern public string UseMap { get; set; }

		[Import("select")]
		extern public void PerformSelect();

        extern public TextRange CreateTextRange();
        extern public void SetSelectionRange(int start, int end);

        public event HtmlEventHandler Select { add { AttachEvent(this, "select", value); } remove { DetachEvent(this, "select", value); } }  
        public event HtmlEventHandler Change { add { AttachEvent(this, "change", value); } remove { DetachEvent(this, "change", value); } }  
    }
}