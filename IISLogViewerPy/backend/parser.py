import os
from datetime import datetime
from collections import defaultdict
from typing import List, Dict, Any, Optional

# Adjust paths relative to backend directory running
LOGS_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "LogFiles")


def get_tree_structure() -> Dict[str, Any]:
    """Scans the LogFiles directory and returns a tree structure of servers/years/months/days."""
    tree = {}
    
    if not os.path.exists(LOGS_DIR):
        return tree
    
    for server_name in sorted(os.listdir(LOGS_DIR)):
        server_path = os.path.join(LOGS_DIR, server_name)
        if not os.path.isdir(server_path):
            continue
        
        server_tree = {}  # year -> month -> [days]
        
        for filename in sorted(os.listdir(server_path)):
            if not filename.startswith("u_ex") or not filename.endswith(".log"):
                continue
            
            # Parse u_exYYMMDD.log
            try:
                yy = filename[4:6]
                mm = filename[6:8]
                dd = filename[8:10]
                year = f"20{yy}"
                month = mm
                day = dd
            except (IndexError, ValueError):
                continue
            
            if year not in server_tree:
                server_tree[year] = {}
            if month not in server_tree[year]:
                server_tree[year][month] = []
            server_tree[year][month].append(day)
        
        tree[server_name] = server_tree
    
    return tree


def get_log_files(server: Optional[str] = None, year: Optional[str] = None, 
                  month: Optional[str] = None, day: Optional[str] = None) -> List[str]:
    """Finds all relevant log files based on optional server, year, month, day filters."""
    if not os.path.exists(LOGS_DIR):
        return []
    
    # Determine which server dirs to scan
    if server:
        server_dirs = [os.path.join(LOGS_DIR, server)]
    else:
        server_dirs = [
            os.path.join(LOGS_DIR, d) 
            for d in os.listdir(LOGS_DIR) 
            if os.path.isdir(os.path.join(LOGS_DIR, d))
        ]
    
    matched_files = []
    
    for server_dir in server_dirs:
        if not os.path.isdir(server_dir):
            continue
        for filename in os.listdir(server_dir):
            if not filename.startswith("u_ex") or not filename.endswith(".log"):
                continue
            
            # Parse u_exYYMMDD.log
            try:
                yy = filename[4:6]
                mm = filename[6:8]
                dd = filename[8:10]
            except IndexError:
                continue
            
            file_year = f"20{yy}"
            file_month = mm
            file_day = dd
            
            # Apply filters
            if year and file_year != year:
                continue
            if month and file_month != month:
                continue
            if day and file_day != day:
                continue
            
            matched_files.append(os.path.join(server_dir, filename))
    
    return sorted(matched_files)


def parse_logs_detailed(server: Optional[str] = None, year: Optional[str] = None, 
                        month: Optional[str] = None, day: Optional[str] = None) -> Dict[str, Any]:
    """Streams relevant log files line by line and computes all metrics in one pass."""
    
    files = get_log_files(server, year, month, day)
    
    # Initialize Aggregation Buckets
    total_requests = 0
    total_response_time = 0
    unique_visitors = set()
    
    status_counts = {"2xx": 0, "3xx": 0, "4xx": 0, "5xx": 0}
    doc_type_counts = defaultdict(int)
    hourly_traffic = {f"{str(i).zfill(2)}:00": 0 for i in range(24)}
    
    url_hits = defaultdict(int)
    error_urls = defaultdict(lambda: {"count": 0, "status": {}})
    slowest_reqs = []  # Will keep top N
    
    files_processed = 0
    
    for file_path in files:
        try:
            with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                col_map = {}
                
                for line in f:
                    line = line.strip()
                    if not line:
                        continue
                    
                    # Parse Headers
                    if line.startswith("#Fields:"):
                        fields = line.split(" ")[1:]
                        col_map = {field: idx for idx, field in enumerate(fields)}
                        continue
                    
                    if line.startswith("#"):
                        continue
                    
                    parts = line.split(" ")
                    if len(parts) < len(col_map):
                        continue
                    
                    def get_val(col_name):
                        idx = col_map.get(col_name)
                        return parts[idx] if idx is not None and idx < len(parts) else None
                    
                    time_str = get_val("time")
                    client_ip = get_val("c-ip")
                    uri_stem = get_val("cs-uri-stem")
                    status = get_val("sc-status")
                    time_taken_str = get_val("time-taken")
                    
                    total_requests += 1
                    
                    # Hourly Traffic
                    if time_str:
                        hour = time_str.split(":")[0] + ":00"
                        if hour in hourly_traffic:
                            hourly_traffic[hour] += 1
                    
                    # Unique Visitors
                    if client_ip:
                        unique_visitors.add(client_ip)
                    
                    # Status Codes
                    if status:
                        s_start = status[0]
                        if s_start in ['2', '3', '4', '5']:
                            status_counts[f"{s_start}xx"] += 1
                    
                    # Most Requested URLs
                    if uri_stem:
                        url_hits[uri_stem] += 1
                        
                        # Doc types grouping
                        ext = os.path.splitext(uri_stem)[1].lower()
                        if ext:
                            doc_type_counts[ext] += 1
                        else:
                            doc_type_counts['.html/none'] += 1
                        
                        # Top Error URLs
                        if status and status.startswith(('4', '5')):
                            err_rec = error_urls[uri_stem]
                            err_rec["count"] += 1
                            err_rec["status"][status] = err_rec["status"].get(status, 0) + 1
                    
                    # Slow Requests & Total Response Time
                    if time_taken_str and time_taken_str.isdigit():
                        tt = int(time_taken_str)
                        total_response_time += tt
                        
                        slowest_reqs.append({
                            "url": uri_stem or "unknown",
                            "ip": client_ip or "unknown",
                            "time": tt
                        })
                        slowest_reqs.sort(key=lambda x: x["time"], reverse=True)
                        slowest_reqs = slowest_reqs[:20]
            
            files_processed += 1
                        
        except Exception as e:
            print(f"Failed to parse {file_path}: {e}")
    
    # Post-processing
    avg_response_time = round(total_response_time / total_requests) if total_requests > 0 else 0
    err_count = status_counts["4xx"] + status_counts["5xx"]
    error_rate = round((err_count / total_requests) * 100, 2) if total_requests > 0 else 0
    
    sorted_urls = sorted(url_hits.items(), key=lambda x: x[1], reverse=True)[:50]
    most_req_list = [{"url": k, "hits": v} for k, v in sorted_urls]
    
    sorted_docs = sorted(doc_type_counts.items(), key=lambda x: x[1], reverse=True)[:10]
    docs_list = [{"name": k, "value": v} for k, v in sorted_docs]
    
    status_list = [
        {"name": "2xx Success", "value": status_counts["2xx"]},
        {"name": "3xx Redirect", "value": status_counts["3xx"]},
        {"name": "4xx Client Error", "value": status_counts["4xx"]},
        {"name": "5xx Server Error", "value": status_counts["5xx"]},
    ]
    
    hourly_list = [{"hour": k, "requests": v} for k, v in hourly_traffic.items()]
    
    err_urls_list = []
    for url, data in error_urls.items():
        primary_status = max(data["status"].items(), key=lambda x: x[1])[0] if data["status"] else "Unknown"
        err_urls_list.append({
            "url": url,
            "count": data["count"],
            "status": primary_status
        })
    err_urls_list.sort(key=lambda x: x["count"], reverse=True)
    err_urls_list = err_urls_list[:20]

    return {
        "overview": {
            "totalRequests": total_requests,
            "uniqueVisitors": len(unique_visitors),
            "avgResponseTime": avg_response_time,
            "errorRate": error_rate,
            "filesProcessed": files_processed
        },
        "documentTypes": docs_list,
        "hourlyTraffic": hourly_list,
        "statusCodes": status_list,
        "mostRequested": most_req_list,
        "slowestRequests": slowest_reqs,
        "topErrors": err_urls_list
    }
