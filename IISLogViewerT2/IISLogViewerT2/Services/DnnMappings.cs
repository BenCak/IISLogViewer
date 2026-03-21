namespace IISLogViewer.Services
{
    public static class DnnMappings
    {
        public static readonly Dictionary<int, string> Tabs = new()
        {
            { 16, "Test Software" },
            { 23, "SCM Requests" },
            { 30, "Viewer Tools" },
            { 35, "Third Party SW" },
            { 41, "Programmed LRUs and PWAs" },
            { 42, "SW Assignment" },
            { 44, "Media" },
            { 47, "Release Tools" },
            { 51, "PDIs" },
        };

        public static readonly Dictionary<int, string> Modules = new()
        {
            { 57,  "System Viewer" },
            { 113, "Report Generator" },
            { 162, "Find Package" },
            { 172, "SCM Requests Queue" },
            { 183, "Media Request" },
            { 219, "CSCI Viewer" },
            { 223, "Software Requests" },
            { 224, "Software Inventory" },
            { 233, "Default P" },
            { 234, "Default P" },
            { 245, "CSCI Release Tool" },
            { 250, "System Release Tool" },
            { 273, "Perishable Data Item (PDI) Catalog" },
        };

        public static readonly Dictionary<int, string> PopupControlTypes = new()
        {
            { 1,  "Add Customer" },
            { 2,  "Add Software" },
            { 3,  "Add PDI" },
            { 4,  "Add LRU/PWA" },
            { 5,  "Add CSCI" },
            { 6,  "Add System" },
            { 7,  "Add Test Package" }

        };

        // Extensions to ignore completely
        public static readonly HashSet<string> IgnoredExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".axd", ".css", ".js", ".png", ".jpg", ".jpeg", ".gif",
            ".ico", ".woff", ".woff2", ".ttf", ".eot", ".svg", ".map"
        };

        // Extensions that get their own special section
        public static readonly HashSet<string> DocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".exe", ".doc", ".docx"
        };
    }
}