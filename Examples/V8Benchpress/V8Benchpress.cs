using Microsoft.LiveLabs.Html;
using Microsoft.LiveLabs.JavaScript.IL2JS;

namespace V8Benchpress
{
    public class V8Benchpress : Page
    {
        [EntryPoint]
        public static void Run() {
            new V8Benchpress();
        }

        public V8Benchpress()
        {
            var button = new Button();
            button.InnerText = "Start";
            button.Click += e => Benchmarks.Main();
            Browser.Document.Body.Add(button);
        }
    }
}
