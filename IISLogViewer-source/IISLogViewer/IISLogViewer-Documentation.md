# IIS Log Viewer Application Documentation

## Overview

The IIS Log Viewer is a Blazor Server application designed to analyze and visualize IIS (Internet Information Services) web server log files. It provides an interactive dashboard and detailed reporting capabilities to help administrators understand website traffic patterns, errors, and performance metrics.

### Key Features

- **Dashboard**: Overall summary with total hits, success/error counts, and aggregated data across all log files
- **Interactive Tree Navigation**: Hierarchical selection of log folders, years, months, and individual dates
- **Detailed Reports**: Tabbed interface showing various metrics including:
  - Page hits and popular URLs
  - Top IP addresses
  - Error pages (4xx/5xx status codes)
  - Document downloads with average response times
  - Popup control usage
  - HTTP status code distribution
  - Slow-performing pages
  - Hourly traffic patterns
- **Search and Filtering**: Each report tab includes search functionality
- **Responsive Design**: Plain Blazor components with Bootstrap styling

## Architecture

### Technology Stack

- **Frontend**: Blazor Server with .NET 8
- **UI Framework**: Plain Blazor + Bootstrap
- **Backend**: C# services for log parsing and state management
- **Data Storage**: File-based (IIS log files in W3C Extended Log File Format)

### Project Structure

```
IISLogViewer/
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor      # Main app layout with drawer
│   │   └── NavMenu.razor         # Tree navigation component
│   └── Pages/
│       ├── Dashboard.razor       # Overall summary page
│       ├── Report.razor          # Detailed report tabs component
│       └── ReportSelected.razor  # Report page for selected dates
├── Services/
│   ├── LogParserService.cs       # Core log parsing logic
│   ├── SelectionState.cs         # Manages selected dates/folders
│   └── DnnMappings.cs            # DNN-specific URL mappings
├── wwwroot/                      # Static assets
└── Program.cs                    # App entry point
```

## Data Flow

1. **Log Discovery**: Application scans the configured log directory for `.log` files
2. **Tree Building**: Parses filenames to extract dates and builds hierarchical year/month/day structure
3. **User Selection**: User selects dates via the tree navigation
4. **Log Parsing**: Selected log files are parsed to extract metrics
5. **Report Generation**: Data is aggregated into `LogReport` objects
6. **UI Rendering**: Reports are displayed using MudBlazor components

## Key Components

### LogParserService

The core service responsible for:

- Discovering log files and building the date tree
- Parsing individual log entries from W3C format files
- Aggregating data into reports
- Caching parsed results for performance

**Key Methods**:

- `GetLogTree()`: Builds hierarchical date structure
- `ParseAll()`: Parses all available log files
- `ParseDates(List<DateTime>)`: Parses logs for specific dates
- `ParseFile(DateTime)`: Parses single date's log file

### SelectionState

Manages the application's state:

- Currently selected folder/directory
- Selected dates for reporting
- Change notifications for UI updates

### LogReport Data Structure

Contains aggregated metrics:

```csharp
public class LogReport
{
    public int TotalHits { get; set; }
    public Dictionary<string, int> PageHits { get; set; }
    public Dictionary<string, int> TopIPs { get; set; }
    public Dictionary<string, int> ErrorPages { get; set; }
    public Dictionary<string, int> DocumentHits { get; set; }
    public Dictionary<string, int> DocumentAvgTime { get; set; }
    public Dictionary<string, int> PopupHits { get; set; }
    public Dictionary<int, int> StatusCodes { get; set; }
    public Dictionary<string, int> SlowPages { get; set; }
    public Dictionary<int, int> HourlyHits { get; set; }
}
```

### LogEntry Data Structure

Represents individual log entries:

```csharp
public class LogEntry
{
    public DateTime DateTime { get; set; }
    public string ClientIp { get; set; }
    public string UriStem { get; set; }
    public int StatusCode { get; set; }
    public int TimeTaken { get; set; }
    // ... additional parsed fields
}
```

## UI Structure

### MainLayout

- **AppBar**: Application title and theme settings
- **Drawer**: Contains the navigation menu
- **MainContent**: Page content area

### NavMenu Component

- **Navigation Links**: Dashboard and Report pages
- **Folder Selector**: Dropdown to choose log directory
- **Tree View**: Hierarchical selection of:
  - Years (with file counts)
  - Months (with day counts)
  - Individual days
- **Checkboxes**: Multi-select functionality for date ranges

### Dashboard Page

- **Summary Cards**: Total hits, success/errors counts
- **Report Component**: Embedded detailed report for all data

### ReportSelected Page

- **Dynamic Title**: Based on selected date range
- **Summary Cards**: Filtered metrics for selected dates
- **Report Component**: Detailed tabs for selected data

### Report Component

**Tabbed Interface** implemented with plain Blazor + Bootstrap:

- Tab buttons for switching sections
- Tables for data display (with basic sorting + paging)
- Standard HTML inputs for search

## Parsing Logic

### Log File Format

The application parses IIS logs in W3C Extended Log File Format:

```
#Fields: date time s-ip cs-method cs-uri-stem cs-uri-query c-ip cs(User-Agent) sc-status sc-bytes cs-bytes time-taken
2022-01-01 12:00:00 192.168.1.1 GET /page.aspx TabID=123 192.168.1.100 Mozilla/5.0 200 1024 512 150
```

### Parsing Process

1. **File Discovery**: Scan directory for `u_exYYMMDD.log` files
2. **Field Extraction**: Parse header to identify field positions
3. **Entry Processing**: For each log line:
   - Extract timestamp, IP, URL, status, response time
   - Parse query strings for DNN-specific data (TabID, ModuleID, etc.)
   - Categorize entries (pages, documents, errors)
   - Aggregate into report dictionaries

### DNN Integration

The app includes special handling for DotNetNuke (DNN) portals:

- Maps TabID to tab names
- Maps ModuleID to module names
- Tracks popup control usage
- Identifies document downloads vs. page views

## Notes on UI

This project intentionally uses plain Blazor components and Bootstrap styling to avoid external UI framework dependencies.
