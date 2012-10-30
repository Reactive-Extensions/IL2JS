namespace Microsoft.LiveLabs.Html
{
    public delegate bool ErrorEventHandler(string message, string url, int errorCode);

    public delegate void HtmlEventHandler(HtmlEvent theEvent);
}