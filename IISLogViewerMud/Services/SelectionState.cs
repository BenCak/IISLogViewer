namespace IISLogViewer.Services
{
    public class SelectionState
    {
        public HashSet<DateTime> SelectedDays { get; set; } = new();
        public string CurrentFolder { get; set; } = "";
        public event Action? OnChange;
        public event Action? OnFolderChanged;

        public void NotifyChanged() => OnChange?.Invoke();
        public void NotifyFolderChanged() => OnFolderChanged?.Invoke();
    }
}