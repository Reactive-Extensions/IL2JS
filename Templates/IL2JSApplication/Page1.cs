using System;
using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace $safeprojectname$
{
    public class Page1 : Page
    {
        // Entry point for JavaScript target (boilerplate)
        [EntryPoint]
        public static void Run()
        {
            new Page1();
        }

        public Page1()
        {
          var div = new Div { InnerText = "Welcome to IL2JS!" };
          Browser.Document.Body.Add(div);
        }
    }
}
