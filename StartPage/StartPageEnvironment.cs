using System;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.StartPage
{
    public class StartPageEnvironment
    {
        public string InputDirectory;
        public string OutputDirectory;
        public string TemplatePage;
        public string RedirectorName;
        public string ManifestName;

        public int NumErrors;
        public int NumWarnings;

        public StartPageEnvironment()
        {
            InputDirectory = ".";
            OutputDirectory = ".";
            TemplatePage = "main.html";
            RedirectorName = "Start";
            ManifestName = "manifest.txt";
        }

        public void Log(Message msg)
        {
            switch (msg.Severity)
            {
                case Severity.Warning:
                    NumWarnings++;
                    break;
                case Severity.Error:
                    NumErrors++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            Console.WriteLine(msg.ToString());
        }
    }
}