namespace IISLogViewer.Services
{
    public class SelectionState
    {
        public HashSet<DateTime> SelectedDays { get; set; } = new();
        public string CurrentFolder { get; set; } = "";
        private readonly Dictionary<string, int> _reportGridPages = new(StringComparer.Ordinal);
        public event Action? OnChange;
        public event Action? OnFolderChanged;

        public int GetReportGridPage(string reportKey)
        {
            if (string.IsNullOrWhiteSpace(reportKey))
                return 1;

            return _reportGridPages.TryGetValue(reportKey, out var page) && page > 0 ? page : 1;
        }

        public void SetReportGridPage(string reportKey, int page)
        {
            if (string.IsNullOrWhiteSpace(reportKey))
                return;

            _reportGridPages[reportKey] = page < 1 ? 1 : page;
        }

        public void NotifyChanged() => OnChange?.Invoke();
        public void NotifyFolderChanged() => OnFolderChanged?.Invoke();
    }
}