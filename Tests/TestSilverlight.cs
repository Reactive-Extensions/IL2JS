using Microsoft.LiveLabs.Html;

namespace Microsoft.LiveLabs.JavaScript.Tests
{
    public partial class TestSilverlight : Page
    {
        public static void Run() {
            new TestSilverlight();
        }

        public TestSilverlight()
        {
            InitializeComponent();
            Test%NAME%.Main();
        }
    }
}
