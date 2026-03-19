namespace IISLogViewer.Services
{
    public class LogEntry
    {
        public DateTime DateTime { get; set; }
        public string ServerIp { get; set; } = "";
        public string Method { get; set; } = "";
        public string UriStem { get; set; } = "";
        public string UriQuery { get; set; } = "";
        public string ClientIp { get; set; } = "";
        public string UserAgent { get; set; } = "";
        public int StatusCode { get; set; }
        public int TimeTaken { get; set; }

        // Parsed extras
        public int? TabId { get; set; }
        public int? ModuleId { get; set; }
        public int? PopupControlType { get; set; }
        public string? TabName { get; set; }
        public string? ModuleName { get; set; }
        public string EntryType { get; set; } = "Page"; // Page, Document, Ignored
        public string FileExtension { get; set; } = "";
    }

    public class LogReport
    {
        public int TotalHits { get; set; }
        public Dictionary<string, int> PageHits { get; set; } = new();
        public Dictionary<string, int> TabHits { get; set; } = new();
        public Dictionary<string, int> ModuleHits { get; set; } = new();
        public Dictionary<string, int> PopupHits { get; set; } = new();
        public Dictionary<string, int> DocumentHits { get; set; } = new();
        public Dictionary<string, int> DocumentAvgTime { get; set; } = new();
        public Dictionary<string, int> TopIPs { get; set; } = new();
        public Dictionary<string, int> ErrorPages { get; set; } = new();
        public Dictionary<int, int> StatusCodes { get; set; } = new();
        public Dictionary<string, int> SlowPages { get; set; } = new();
        public Dictionary<int, int> HourlyHits { get; set; } = new();
        public Dictionary<int, Dictionary<string, int>> HourlyPageHits { get; set; } = new();
        public Dictionary<int, Dictionary<string, int>> HourlyDocumentHits { get; set; } = new();
        public Dictionary<int, Dictionary<string, int>> HourlyPopupHits { get; set; } = new();
        public Dictionary<int, int> WeekdayHits { get; set; } = new();
        public Dictionary<int, Dictionary<string, int>> WeekdayPageHits { get; set; } = new();
        public Dictionary<int, Dictionary<string, int>> WeekdayUserHits { get; set; } = new();
        public Dictionary<int, Dictionary<string, int>> WeekdayPopupHits { get; set; } = new();
        public Dictionary<int, Dictionary<string, int>> WeekdayDocumentHits { get; set; } = new();
        public Dictionary<int, Dictionary<int, int>> WeekdayHourHits { get; set; } = new();
        public Dictionary<string, int> UserHits { get; set; } = new();
        public Dictionary<string, Dictionary<string, int>> UserPageHits { get; set; } = new();
        public Dictionary<string, Dictionary<string, int>> UserDocumentHits { get; set; } = new();
        public Dictionary<string, Dictionary<string, int>> UserPopupHits { get; set; } = new();
        public Dictionary<string, Dictionary<int, int>> UserStatusHits { get; set; } = new();
        public Dictionary<string, Dictionary<int, int>> UserHourlyHits { get; set; } = new();
        public Dictionary<string, Dictionary<int, int>> UserWeekdayHits { get; set; } = new();
    }

    public class UserProfile
    {
        public string LoginName { get; set; } = "";
        public string Name { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Department { get; set; } = "";
    }

    public class LogParserService
    {
        private string _logDirectory;
        private readonly string _rootDirectory;
        private readonly TimeZoneInfo _targetTimeZone;
        private readonly Dictionary<string, UserProfile> _userProfiles;

        public string TargetTimeZoneId => _targetTimeZone.Id;
        
        // Caches for expensive operations
        private Dictionary<int, Dictionary<int, List<DateTime>>>? _cachedTree;
        private string _cachedTreeFolder = "";
        private LogReport? _cachedAllReport;
        private string _cachedAllFolder = "";

        public LogParserService(string rootDirectory, string? timeZoneId = null, string? userCsvPath = null)
        {
            _rootDirectory = rootDirectory;
            _logDirectory = rootDirectory;
            _targetTimeZone = ResolveTimeZone(timeZoneId);
            _userProfiles = LoadUserProfiles(userCsvPath);
        }

    public string CurrentFolder => _logDirectory;

    public List<string> GetAvailableFolders()
    {
        var folders = new List<string>();
        folders.Add(_rootDirectory); // root itself

        foreach (var dir in Directory.GetDirectories(_rootDirectory, "*", SearchOption.AllDirectories))
            folders.Add(dir);

        return folders;
    }

    public string GetFolderDisplayName(string path)
    {
        return path == _rootDirectory ? "All Logs (Root)" : Path.GetFileName(path);
    }

    public void SetFolder(string path)
    {
        _logDirectory = path;
    }
    
    public void ClearCache()
    {
        _cachedTree = null;
        _cachedAllReport = null;
    }

    public UserProfile? GetUserProfile(string loginName)
    {
        if (string.IsNullOrWhiteSpace(loginName))
            return null;

        _userProfiles.TryGetValue(loginName, out var profile);
        return profile;
    }

    public string GetUserDisplayName(string loginName)
    {
        var profile = GetUserProfile(loginName);
        if (profile == null)
            return loginName;

        return string.IsNullOrWhiteSpace(profile.Name) ? loginName : profile.Name;
    }
    
        public Dictionary<int, Dictionary<int, List<DateTime>>> GetLogTree()
        {
            if (_cachedTree != null && _cachedTreeFolder == _logDirectory)
                return _cachedTree;

            var tree = new Dictionary<int, Dictionary<int, List<DateTime>>>();
            var files = Directory.GetFiles(_logDirectory, "*.log", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var date = ExtractDateFromFilename(Path.GetFileName(file));
                if (date == null) continue;

                var year = date.Value.Year;
                var month = date.Value.Month;

                if (!tree.ContainsKey(year))
                    tree[year] = new Dictionary<int, List<DateTime>>();
                if (!tree[year].ContainsKey(month))
                    tree[year][month] = new List<DateTime>();

                if (!tree[year][month].Contains(date.Value))
                    tree[year][month].Add(date.Value);
            }
            
            _cachedTreeFolder = _logDirectory;
            _cachedTree = tree;
            return tree;
        }

        public async Task<LogReport> ParseFile(DateTime date)
        {
            var filename = $"u_ex{date:yyMMdd}.log";
            var files = Directory.GetFiles(_logDirectory, filename, SearchOption.AllDirectories);
            return await ParseFiles(files);
        }

        public async Task<LogReport> ParseMonth(int year, int month)
        {
            var allFiles = Directory.GetFiles(_logDirectory, "*.log", SearchOption.AllDirectories);
            var files = allFiles.Where(f =>
            {
                var date = ExtractDateFromFilename(Path.GetFileName(f));
                return date.HasValue && date.Value.Year == year && date.Value.Month == month;
            });
            return await ParseFiles(files);
        }

        public async Task<LogReport> ParseYear(int year)
        {
            var allFiles = Directory.GetFiles(_logDirectory, "*.log", SearchOption.AllDirectories);
            var files = allFiles.Where(f =>
            {
                var date = ExtractDateFromFilename(Path.GetFileName(f));
                return date.HasValue && date.Value.Year == year;
            });
            return await ParseFiles(files);
        }

        public async Task<LogReport> ParseAll()
        {
            if (_cachedAllReport != null && _cachedAllFolder == _logDirectory)
                return _cachedAllReport;

            var files = Directory.GetFiles(_logDirectory, "u_ex*.log", SearchOption.AllDirectories);
            var report = await ParseFiles(files);
            
            _cachedAllFolder = _logDirectory;
            _cachedAllReport = report;
            return report;
        }

        public async Task<LogReport> ParseDates(List<DateTime> dates)
        {
            var allFiles = Directory.GetFiles(_logDirectory, "*.log", SearchOption.AllDirectories);
            var files = allFiles.Where(f =>
            {
                var date = ExtractDateFromFilename(Path.GetFileName(f));
                return date.HasValue && dates.Any(d => d.Date == date.Value.Date);
            });
            return await ParseFiles(files);
        }

        private class AggregateTracker
        {
            public int Hits { get; set; }
            public long TotalTime { get; set; }
        }

        private sealed class FileParseResult
        {
            public LogReport Report { get; init; } = new();
            public Dictionary<string, AggregateTracker> DocumentTracking { get; init; } = new();
            public Dictionary<string, AggregateTracker> SlowTracking { get; init; } = new();
        }

        private async Task<LogReport> ParseFiles(IEnumerable<string> filePaths)
        {
            var paths = filePaths.Where(File.Exists).ToArray();
            if (paths.Length == 0)
                return new LogReport();

            // Parse each file on a background thread, then merge.
            var tasks = paths.Select(p => Task.Run(() => ParseSingleFile(p)));
            var results = await Task.WhenAll(tasks);

            var report = new LogReport();
            var docTracking = new Dictionary<string, AggregateTracker>(StringComparer.OrdinalIgnoreCase);
            var slowTracking = new Dictionary<string, AggregateTracker>(StringComparer.OrdinalIgnoreCase);

            foreach (var res in results)
            {
                var r = res.Report;

                report.TotalHits += r.TotalHits;

                MergeDictionary(report.PageHits, r.PageHits);
                MergeDictionary(report.TabHits, r.TabHits);
                MergeDictionary(report.ModuleHits, r.ModuleHits);
                MergeDictionary(report.PopupHits, r.PopupHits);
                MergeDictionary(report.DocumentHits, r.DocumentHits);
                MergeDictionary(report.TopIPs, r.TopIPs);
                MergeDictionary(report.ErrorPages, r.ErrorPages);
                MergeDictionary(report.StatusCodes, r.StatusCodes);
                MergeDictionary(report.HourlyHits, r.HourlyHits);
                MergeNestedDictionary(report.HourlyPageHits, r.HourlyPageHits);
                MergeNestedDictionary(report.HourlyDocumentHits, r.HourlyDocumentHits);
                MergeNestedDictionary(report.HourlyPopupHits, r.HourlyPopupHits);
                MergeDictionary(report.WeekdayHits, r.WeekdayHits);
                MergeNestedDictionary(report.WeekdayPageHits, r.WeekdayPageHits);
                MergeNestedDictionary(report.WeekdayUserHits, r.WeekdayUserHits);
                MergeNestedDictionary(report.WeekdayPopupHits, r.WeekdayPopupHits);
                MergeNestedDictionary(report.WeekdayDocumentHits, r.WeekdayDocumentHits);
                MergeNestedDictionary(report.WeekdayHourHits, r.WeekdayHourHits);
                MergeDictionary(report.UserHits, r.UserHits);
                MergeNestedDictionary(report.UserPageHits, r.UserPageHits);
                MergeNestedDictionary(report.UserDocumentHits, r.UserDocumentHits);
                MergeNestedDictionary(report.UserPopupHits, r.UserPopupHits);
                MergeNestedDictionary(report.UserStatusHits, r.UserStatusHits);
                MergeNestedDictionary(report.UserHourlyHits, r.UserHourlyHits);
                MergeNestedDictionary(report.UserWeekdayHits, r.UserWeekdayHits);

                MergeTracking(docTracking, res.DocumentTracking);
                MergeTracking(slowTracking, res.SlowTracking);
            }

            // Post-process Sorting and Averages once, after merge
            report.PageHits = report.PageHits.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            report.TabHits = report.TabHits.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            report.ModuleHits = report.ModuleHits.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            report.PopupHits = report.PopupHits.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            report.TopIPs = report.TopIPs.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            report.ErrorPages = report.ErrorPages.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            report.HourlyHits = report.HourlyHits.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
            report.HourlyPageHits = SortNestedHourMap(report.HourlyPageHits);
            report.HourlyDocumentHits = SortNestedHourMap(report.HourlyDocumentHits);
            report.HourlyPopupHits = SortNestedHourMap(report.HourlyPopupHits);
            report.WeekdayHits = report.WeekdayHits.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
            report.WeekdayPageHits = SortNestedHourMap(report.WeekdayPageHits);
            report.WeekdayUserHits = SortNestedHourMap(report.WeekdayUserHits);
            report.WeekdayPopupHits = SortNestedHourMap(report.WeekdayPopupHits);
            report.WeekdayDocumentHits = SortNestedHourMap(report.WeekdayDocumentHits);
            report.WeekdayHourHits = SortNestedIntOuterMap(report.WeekdayHourHits);
            report.UserHits = report.UserHits.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            report.UserPageHits = SortNestedMap(report.UserPageHits);
            report.UserDocumentHits = SortNestedMap(report.UserDocumentHits);
            report.UserPopupHits = SortNestedMap(report.UserPopupHits);
            report.UserStatusHits = SortNestedMap(report.UserStatusHits);
            report.UserHourlyHits = SortNestedMap(report.UserHourlyHits);
            report.UserWeekdayHits = SortNestedMap(report.UserWeekdayHits);

            report.DocumentHits = report.DocumentHits.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            foreach (var kvp in docTracking)
            {
                report.DocumentAvgTime[kvp.Key] = (int)(kvp.Value.TotalTime / kvp.Value.Hits);
            }

            var sortedSlowPages = slowTracking
                .ToDictionary(kvp => kvp.Key, kvp => (int)(kvp.Value.TotalTime / kvp.Value.Hits))
                .OrderByDescending(x => x.Value)
                .ToDictionary(x => x.Key, x => x.Value);
            report.SlowPages = sortedSlowPages;

            return report;
        }

        private static void MergeDictionary<TKey>(Dictionary<TKey, int> target, Dictionary<TKey, int> source) where TKey : notnull
        {
            foreach (var kvp in source)
            {
                if (target.TryGetValue(kvp.Key, out var existing))
                    target[kvp.Key] = existing + kvp.Value;
                else
                    target[kvp.Key] = kvp.Value;
            }
        }

        private static void MergeTracking(Dictionary<string, AggregateTracker> target, Dictionary<string, AggregateTracker> source)
        {
            foreach (var (key, tracker) in source)
            {
                if (!target.TryGetValue(key, out var existing))
                {
                    target[key] = new AggregateTracker { Hits = tracker.Hits, TotalTime = tracker.TotalTime };
                }
                else
                {
                    existing.Hits += tracker.Hits;
                    existing.TotalTime += tracker.TotalTime;
                }
            }
        }

        private static void MergeNestedDictionary<TOuterKey, TInnerKey>(
            Dictionary<TOuterKey, Dictionary<TInnerKey, int>> target,
            Dictionary<TOuterKey, Dictionary<TInnerKey, int>> source)
            where TOuterKey : notnull
            where TInnerKey : notnull
        {
            foreach (var (outerKey, bucket) in source)
            {
                if (!target.TryGetValue(outerKey, out var targetBucket))
                {
                    targetBucket = new Dictionary<TInnerKey, int>();
                    target[outerKey] = targetBucket;
                }

                foreach (var (key, count) in bucket)
                {
                    if (targetBucket.TryGetValue(key, out var existing))
                        targetBucket[key] = existing + count;
                    else
                        targetBucket[key] = count;
                }
            }
        }

        private static Dictionary<int, Dictionary<string, int>> SortNestedHourMap(
            Dictionary<int, Dictionary<string, int>> source)
        {
            return source
                .OrderBy(x => x.Key)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value
                        .OrderByDescending(i => i.Value)
                        .ThenBy(i => i.Key, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(i => i.Key, i => i.Value, StringComparer.OrdinalIgnoreCase));
        }

        private static Dictionary<string, Dictionary<TInnerKey, int>> SortNestedMap<TInnerKey>(
            Dictionary<string, Dictionary<TInnerKey, int>> source)
            where TInnerKey : notnull
        {
            return source
                .OrderByDescending(x => x.Value.Values.Sum())
                .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value
                        .OrderByDescending(i => i.Value)
                        .ToDictionary(i => i.Key, i => i.Value),
                    StringComparer.OrdinalIgnoreCase);
        }

        private static Dictionary<int, Dictionary<TInnerKey, int>> SortNestedIntOuterMap<TInnerKey>(
            Dictionary<int, Dictionary<TInnerKey, int>> source)
            where TInnerKey : notnull
        {
            return source
                .OrderBy(x => x.Key)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value
                        .OrderByDescending(i => i.Value)
                        .ToDictionary(i => i.Key, i => i.Value));
        }

        private FileParseResult ParseSingleFile(string path)
        {
            var report = new LogReport();
            var docTracking = new Dictionary<string, AggregateTracker>(StringComparer.OrdinalIgnoreCase);
            var slowTracking = new Dictionary<string, AggregateTracker>(StringComparer.OrdinalIgnoreCase);
            var fallbackDate = ExtractDateFromFilename(Path.GetFileName(path));

            try
            {
                if (!File.Exists(path))
                    return new FileParseResult { Report = report, DocumentTracking = docTracking, SlowTracking = slowTracking };

                string[] fields = Array.Empty<string>();

                using var reader = new StreamReader(path);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("#Fields:"))
                    {
                        fields = line.Substring(9).Trim().Split(' ');
                        continue;
                    }
                    if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line)) continue;
                    if (fields.Length == 0) continue;

                    var parts = line.Split(' ');
                    if (parts.Length != fields.Length) continue;

                    try
                    {
                        var fieldMap = fields.Select((f, i) => new { f, i })
                                            .ToDictionary(x => x.f, x => parts[x.i]);

                        

                        string uriStem = "";
                        string uriQuery = "";
                        string clientIp = "";
                        int statusCode = 0;
                        int timeTaken = 0;

                        if (fieldMap.TryGetValue("cs-uri-stem", out var uri))
                            uriStem = uri == "-" ? "" : uri;

                        if (fieldMap.TryGetValue("cs-uri-query", out var query))
                            uriQuery = query == "-" ? "" : query;

                        if (fieldMap.TryGetValue("c-ip", out var ip))
                            clientIp = ip == "-" ? "" : ip;

                        string userName = "Anonymous";
                        if (fieldMap.TryGetValue("cs-username", out var user))
                            userName = NormalizeUserName(user);

                        if (fieldMap.TryGetValue("sc-status", out var statusStr) &&
                            int.TryParse(statusStr, out var sc))
                            statusCode = sc;

                        if (fieldMap.TryGetValue("time-taken", out var ttStr) &&
                            int.TryParse(ttStr, out var tt))
                            timeTaken = tt;

                        
                        report.TotalHits++;

                        // Track hourly traffic in configured local timezone (IIS timestamp is UTC)
                        int? hourForBreakdown = null;
                        int? weekdayForBreakdown = null;
                        if (TryGetLocalDateTime(fieldMap, fallbackDate, out var localDateTime))
                        {
                            int hour = localDateTime.Hour;
                            int weekday = (int)localDateTime.DayOfWeek;
                            if (!report.HourlyHits.ContainsKey(hour))
                                report.HourlyHits[hour] = 0;
                            report.HourlyHits[hour]++;

                            if (!report.WeekdayHits.ContainsKey(weekday))
                                report.WeekdayHits[weekday] = 0;
                            report.WeekdayHits[weekday]++;

                            hourForBreakdown = hour;
                            weekdayForBreakdown = weekday;

                            IncrementNestedBucket(report.WeekdayHourHits, weekday, hour);
                            IncrementNestedBucket(report.WeekdayUserHits, weekday, userName);

                            IncrementNestedBucket(report.UserHourlyHits, userName, hour);
                            IncrementNestedBucket(report.UserWeekdayHits, userName, weekday);
                        }
                        // Increment Status Code
                        if (statusCode > 0)
                        {
                            if (!report.StatusCodes.ContainsKey(statusCode))
                                report.StatusCodes[statusCode] = 0;
                            report.StatusCodes[statusCode]++;
                        }

                        // Increment Client IP
                        if (!string.IsNullOrEmpty(clientIp))
                        {
                            if (!report.TopIPs.ContainsKey(clientIp))
                                report.TopIPs[clientIp] = 0;
                            report.TopIPs[clientIp]++;
                        }

                        if (!report.UserHits.ContainsKey(userName))
                            report.UserHits[userName] = 0;
                        report.UserHits[userName]++;

                        // Determine file extension
                        string fileExtension = Path.GetExtension(uriStem).ToLowerInvariant();

                        // Skip ignored extensions and empty/dash stems
                        if (string.IsNullOrEmpty(uriStem) ||
                            DnnMappings.IgnoredExtensions.Contains(fileExtension))
                        {
                            continue;
                        }

                        // Determine Entry Type
                        bool isDocument = DnnMappings.DocumentExtensions.Contains(fileExtension);

                        // Parse query string for DNN specific things
                        string? tabName = null;
                        string? moduleName = null;
                        int? popupType = null;

                        if (!string.IsNullOrEmpty(uriQuery))
                        {
                            var queryParams = uriQuery.Split('&')
                                .Select(p => p.Split('='))
                                .Where(p => p.Length == 2)
                                .ToDictionary(p => p[0], p => p[1], StringComparer.OrdinalIgnoreCase);

                            if (queryParams.TryGetValue("TabID", out var tabIdStr) && int.TryParse(tabIdStr, out var tabId))
                                tabName = DnnMappings.Tabs.TryGetValue(tabId, out var tn) ? tn : $"Tab {tabId}";

                            if (queryParams.TryGetValue("ModuleID", out var modIdStr) && int.TryParse(modIdStr, out var modId))
                                moduleName = DnnMappings.Modules.TryGetValue(modId, out var mn) ? mn : $"Module {modId}";

                            if (queryParams.TryGetValue("PopupControlType", out var pctStr) && int.TryParse(pctStr, out var pct))
                                popupType = pct;
                        }

                        // Track Error Pages (400+)
                        if (statusCode >= 400)
                        {
                            string errLabel = $"{uriStem} ({statusCode})";
                            if (!report.ErrorPages.ContainsKey(errLabel))
                                report.ErrorPages[errLabel] = 0;
                            report.ErrorPages[errLabel]++;
                        }

                        if (statusCode > 0)
                            IncrementNestedBucket(report.UserStatusHits, userName, statusCode);

                        if (isDocument)
                        {
                            // Track Documents
                            if (!report.DocumentHits.ContainsKey(uriStem))
                                report.DocumentHits[uriStem] = 0;
                            report.DocumentHits[uriStem]++;

                            if (!docTracking.ContainsKey(uriStem))
                                docTracking[uriStem] = new AggregateTracker();
                            docTracking[uriStem].Hits++;
                            docTracking[uriStem].TotalTime += timeTaken;

                            if (hourForBreakdown.HasValue)
                                IncrementHourlyBucket(report.HourlyDocumentHits, hourForBreakdown.Value, uriStem);

                            if (weekdayForBreakdown.HasValue)
                                IncrementNestedBucket(report.WeekdayDocumentHits, weekdayForBreakdown.Value, uriStem);

                            IncrementNestedBucket(report.UserDocumentHits, userName, uriStem);
                        }
                        else
                        {
                            // It's a Page
                            string pageLabel = BuildPageLabel(uriStem, uriQuery, tabName, moduleName);

                            if (!report.PageHits.ContainsKey(pageLabel))
                                report.PageHits[pageLabel] = 0;
                            report.PageHits[pageLabel]++;

                            if (hourForBreakdown.HasValue)
                                IncrementHourlyBucket(report.HourlyPageHits, hourForBreakdown.Value, pageLabel);

                            if (weekdayForBreakdown.HasValue)
                                IncrementNestedBucket(report.WeekdayPageHits, weekdayForBreakdown.Value, pageLabel);

                            IncrementNestedBucket(report.UserPageHits, userName, pageLabel);

                            // Track Slow Pages Time
                            if (timeTaken > 0)
                            {
                                if (!slowTracking.ContainsKey(pageLabel))
                                    slowTracking[pageLabel] = new AggregateTracker();
                                slowTracking[pageLabel].Hits++;
                                slowTracking[pageLabel].TotalTime += timeTaken;
                            }

                            // Track Tabs
                            if (tabName != null)
                            {
                                if (!report.TabHits.ContainsKey(tabName))
                                    report.TabHits[tabName] = 0;
                                report.TabHits[tabName]++;
                            }

                            // Track Modules
                            if (moduleName != null)
                            {
                                if (!report.ModuleHits.ContainsKey(moduleName))
                                    report.ModuleHits[moduleName] = 0;
                                report.ModuleHits[moduleName]++;
                            }

                            // Track Popups
                            if (popupType.HasValue)
                            {
                                string popLabel = DnnMappings.PopupControlTypes.TryGetValue(popupType.Value, out var pn) ? pn : $"Type {popupType}";
                                if (!report.PopupHits.ContainsKey(popLabel))
                                    report.PopupHits[popLabel] = 0;
                                report.PopupHits[popLabel]++;

                                IncrementNestedBucket(report.UserPopupHits, userName, popLabel);

                                if (hourForBreakdown.HasValue)
                                    IncrementHourlyBucket(report.HourlyPopupHits, hourForBreakdown.Value, popLabel);

                                if (weekdayForBreakdown.HasValue)
                                    IncrementNestedBucket(report.WeekdayPopupHits, weekdayForBreakdown.Value, popLabel);
                            }
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            catch
            {
                // Ignore file-level parse errors; partial data is fine.
            }

            return new FileParseResult
            {
                Report = report,
                DocumentTracking = docTracking,
                SlowTracking = slowTracking
            };
        }

        private string BuildPageLabel(string stem, string query, string? tabName, string? moduleName)
        {
            var formattedQuery = string.IsNullOrEmpty(query) ? "" : $"?{query}";

            if (tabName != null && moduleName != null)
                return $"{stem}{formattedQuery} (Tab={tabName}, Module={moduleName})";

            if (tabName != null)
                return $"{stem}{formattedQuery} (Tab={tabName})";

            return stem;
        }

        private static void IncrementHourlyBucket(Dictionary<int, Dictionary<string, int>> map, int hour, string key)
        {
            if (!map.TryGetValue(hour, out var bucket))
            {
                bucket = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                map[hour] = bucket;
            }

            if (bucket.TryGetValue(key, out var existing))
                bucket[key] = existing + 1;
            else
                bucket[key] = 1;
        }

        private static void IncrementNestedBucket<TOuterKey, TInnerKey>(
            Dictionary<TOuterKey, Dictionary<TInnerKey, int>> map,
            TOuterKey outerKey,
            TInnerKey innerKey)
            where TOuterKey : notnull
            where TInnerKey : notnull
        {
            if (!map.TryGetValue(outerKey, out var bucket))
            {
                bucket = new Dictionary<TInnerKey, int>();
                map[outerKey] = bucket;
            }

            if (bucket.TryGetValue(innerKey, out var existing))
                bucket[innerKey] = existing + 1;
            else
                bucket[innerKey] = 1;
        }

        private static string NormalizeUserName(string rawUser)
        {
            if (string.IsNullOrWhiteSpace(rawUser) || rawUser == "-")
                return "Anonymous";

            var trimmed = rawUser.Trim();
            if (trimmed.Contains('\\'))
            {
                var pieces = trimmed.Split('\\', StringSplitOptions.RemoveEmptyEntries);
                if (pieces.Length > 0)
                    return pieces[^1];
            }

            return trimmed;
        }

        private static Dictionary<string, UserProfile> LoadUserProfiles(string? userCsvPath)
        {
            var profiles = new Dictionary<string, UserProfile>(StringComparer.OrdinalIgnoreCase);

            var resolvedPath = ResolveUserCsvPath(userCsvPath);
            if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
                return profiles;

            try
            {
                using var reader = new StreamReader(resolvedPath);
                var headerLine = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(headerLine))
                    return profiles;

                var headers = headerLine.Split(',').Select(h => h.Trim()).ToArray();
                var index = headers
                    .Select((name, idx) => new { name, idx })
                    .ToDictionary(x => x.name, x => x.idx, StringComparer.OrdinalIgnoreCase);

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var cols = line.Split(',');
                    var loginName = ReadCsvValue(cols, index, "loginname");
                    if (string.IsNullOrWhiteSpace(loginName))
                        continue;

                    profiles[loginName] = new UserProfile
                    {
                        LoginName = loginName,
                        Name = ReadCsvValue(cols, index, "name"),
                        FirstName = ReadCsvValue(cols, index, "firstname"),
                        LastName = ReadCsvValue(cols, index, "lastname"),
                        Email = ReadCsvValue(cols, index, "email"),
                        Department = ReadCsvValue(cols, index, "department")
                    };
                }
            }
            catch
            {
                // Ignore CSV load errors; report should continue without enrichment.
            }

            return profiles;
        }

        private static string ResolveUserCsvPath(string? configuredPath)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
                return Path.Combine(Directory.GetCurrentDirectory(), "Data", "user-profiles.csv");

            if (Path.IsPathRooted(configuredPath))
                return configuredPath;

            return Path.Combine(Directory.GetCurrentDirectory(), configuredPath);
        }

        private static string ReadCsvValue(string[] cols, Dictionary<string, int> index, string key)
        {
            if (!index.TryGetValue(key, out var colIndex))
                return "";

            if (colIndex < 0 || colIndex >= cols.Length)
                return "";

            return cols[colIndex].Trim();
        }

        private bool TryGetLocalDateTime(Dictionary<string, string> fieldMap, DateTime? fallbackDate, out DateTime localDateTime)
        {
            localDateTime = default;

            if (!fieldMap.TryGetValue("time", out var timeStr) ||
                !TimeSpan.TryParse(timeStr, out var timeVal))
            {
                return false;
            }

            DateTime datePart;
            if (fieldMap.TryGetValue("date", out var dateStr) &&
                DateTime.TryParseExact(
                    dateStr,
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var parsedDate))
            {
                datePart = parsedDate.Date;
            }
            else if (fallbackDate.HasValue)
            {
                datePart = fallbackDate.Value.Date;
            }
            else
            {
                localDateTime = DateTime.Today.Add(timeVal);
                return true;
            }

            var utcDateTime = DateTime.SpecifyKind(datePart.Add(timeVal), DateTimeKind.Utc);
            localDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, _targetTimeZone);
            return true;
        }

        private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
        {
            if (string.IsNullOrWhiteSpace(timeZoneId))
                return TimeZoneInfo.Local;

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
            }
            catch
            {
                return TimeZoneInfo.Local;
            }
        }

        private DateTime? ExtractDateFromFilename(string filename)
        {
            try
            {
                if (filename.Length < 10) return null;
                var datePart = filename.Substring(4, 6);
                if (DateTime.TryParseExact(datePart, "yyMMdd",
                    null, System.Globalization.DateTimeStyles.None, out var date))
                    return date;
            }
            catch { }
            return null;
        }
    }
}