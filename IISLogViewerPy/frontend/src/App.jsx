import React, { useState, useEffect } from 'react';
import { 
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip as RechartsTooltip, ResponsiveContainer,
  PieChart, Pie, Cell, LineChart, Line, Legend
} from 'recharts';
import { 
  Activity, Clock, FileText, AlertTriangle, Users, 
  Globe, Calendar, Filter, ServerCrash, Loader2,
  ChevronRight, ChevronDown, Server, FolderOpen, Folder
} from 'lucide-react';

const API_BASE = 'http://localhost:8000';

const COLORS = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899'];
const STATUS_COLORS = {
  '2xx': '#10b981',
  '3xx': '#3b82f6',
  '4xx': '#f59e0b',
  '5xx': '#ef4444'
};

const MONTH_NAMES = {
  '01': 'January', '02': 'February', '03': 'March', '04': 'April',
  '05': 'May', '06': 'June', '07': 'July', '08': 'August',
  '09': 'September', '10': 'October', '11': 'November', '12': 'December'
};

/* ─── Tree Sidebar ─── */
const TreeNode = ({ label, icon, level, isSelected, isOpen, hasChildren, onClick, onToggle }) => {
  const indent = level * 16;
  return (
    <div
      className={`flex items-center gap-2 px-3 py-2 cursor-pointer rounded-lg mx-2 my-0.5 transition-all duration-150 text-sm
        ${isSelected 
          ? 'bg-blue-600/20 text-blue-400 border border-blue-500/30' 
          : 'text-slate-400 hover:bg-slate-700/50 hover:text-slate-200 border border-transparent'}`}
      style={{ paddingLeft: `${indent + 12}px` }}
    >
      {hasChildren ? (
        <button onClick={(e) => { e.stopPropagation(); onToggle(); }} className="p-0 hover:text-white transition-colors">
          {isOpen ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
        </button>
      ) : (
        <span className="w-[14px]" />
      )}
      <span onClick={onClick} className="flex items-center gap-2 flex-1">
        {icon}
        <span className="truncate">{label}</span>
      </span>
    </div>
  );
};

const TreeSidebar = ({ tree, onSelect, currentSelection }) => {
  const [expanded, setExpanded] = useState({});

  const toggle = (key) => {
    setExpanded(prev => ({ ...prev, [key]: !prev[key] }));
  };

  const isSelected = (server, year, month, day) => {
    return currentSelection.server === server 
      && currentSelection.year === year 
      && currentSelection.month === month 
      && currentSelection.day === day;
  };

  return (
    <div className="w-72 min-w-[280px] bg-slate-800 border-r border-slate-700 flex flex-col h-screen overflow-hidden">
      {/* Sidebar Header */}
      <div className="p-4 border-b border-slate-700">
        <h2 className="text-white font-bold text-lg flex items-center gap-2">
          <FolderOpen className="text-blue-400" size={20} />
          Log Explorer
        </h2>
        <p className="text-slate-500 text-xs mt-1">Select scope to analyze</p>
      </div>
      
      <div className="flex-1 overflow-y-auto py-2">
        {/* All Servers */}
        <TreeNode
          label="All Servers"
          icon={<Globe size={14} className="text-emerald-400" />}
          level={0}
          isSelected={isSelected(null, null, null, null)}
          hasChildren={false}
          onClick={() => onSelect({ server: null, year: null, month: null, day: null })}
        />
        
        <div className="border-b border-slate-700/50 my-2 mx-4" />

        {Object.keys(tree).sort().map(serverName => {
          const serverKey = `srv-${serverName}`;
          const serverOpen = expanded[serverKey];
          const serverYears = tree[serverName];

          return (
            <div key={serverName}>
              <TreeNode
                label={serverName}
                icon={<Server size={14} className="text-purple-400" />}
                level={0}
                isSelected={isSelected(serverName, null, null, null)}
                isOpen={serverOpen}
                hasChildren={true}
                onClick={() => onSelect({ server: serverName, year: null, month: null, day: null })}
                onToggle={() => toggle(serverKey)}
              />

              {serverOpen && Object.keys(serverYears).sort().map(year => {
                const yearKey = `${serverKey}-${year}`;
                const yearOpen = expanded[yearKey];
                const yearMonths = serverYears[year];

                return (
                  <div key={year}>
                    <TreeNode
                      label={year}
                      icon={<Calendar size={14} className="text-blue-400" />}
                      level={1}
                      isSelected={isSelected(serverName, year, null, null)}
                      isOpen={yearOpen}
                      hasChildren={true}
                      onClick={() => onSelect({ server: serverName, year, month: null, day: null })}
                      onToggle={() => toggle(yearKey)}
                    />

                    {yearOpen && Object.keys(yearMonths).sort().map(month => {
                      const monthKey = `${yearKey}-${month}`;
                      const monthOpen = expanded[monthKey];
                      const monthDays = yearMonths[month];

                      return (
                        <div key={month}>
                          <TreeNode
                            label={MONTH_NAMES[month] || month}
                            icon={<Folder size={14} className="text-amber-400" />}
                            level={2}
                            isSelected={isSelected(serverName, year, month, null)}
                            isOpen={monthOpen}
                            hasChildren={true}
                            onClick={() => onSelect({ server: serverName, year, month, day: null })}
                            onToggle={() => toggle(monthKey)}
                          />

                          {monthOpen && monthDays.sort().map(day => (
                            <TreeNode
                              key={day}
                              label={`Day ${day}`}
                              icon={<FileText size={14} className="text-slate-500" />}
                              level={3}
                              isSelected={isSelected(serverName, year, month, day)}
                              hasChildren={false}
                              onClick={() => onSelect({ server: serverName, year, month, day })}
                            />
                          ))}
                        </div>
                      );
                    })}
                  </div>
                );
              })}
            </div>
          );
        })}
      </div>
    </div>
  );
};


/* ─── Dashboard ─── */
const MetricCard = ({ title, value, icon: Icon, colorClass }) => (
  <div className="bg-slate-800 rounded-xl p-5 border border-slate-700 shadow-lg hover:border-slate-600 transition-colors">
    <div className="flex items-center justify-between">
      <div>
        <p className="text-slate-400 text-sm font-medium mb-1">{title}</p>
        <h3 className="text-2xl font-bold text-white tracking-tight">{value}</h3>
      </div>
      <div className={`p-3 rounded-full bg-slate-700/50 ${colorClass}`}>
        <Icon size={20} />
      </div>
    </div>
  </div>
);

const Dashboard = () => {
  const [tree, setTree] = useState({});
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [treeLoading, setTreeLoading] = useState(true);
  const [error, setError] = useState(null);
  const [selection, setSelection] = useState({ server: null, year: null, month: null, day: null });

  // Fetch tree structure on mount
  useEffect(() => {
    const fetchTree = async () => {
      try {
        const res = await fetch(`${API_BASE}/api/tree`);
        const json = await res.json();
        setTree(json);
      } catch (err) {
        console.error('Failed to fetch tree:', err);
      } finally {
        setTreeLoading(false);
      }
    };
    fetchTree();
  }, []);

  // Fetch dashboard data when selection changes
  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      setError(null);
      try {
        const params = new URLSearchParams();
        if (selection.server) params.append('server', selection.server);
        if (selection.year) params.append('year', selection.year);
        if (selection.month) params.append('month', selection.month);
        if (selection.day) params.append('day', selection.day);
        
        const res = await fetch(`${API_BASE}/api/dashboard?${params}`);
        if (!res.ok) throw new Error(`API error: ${res.status}`);
        const json = await res.json();
        setData(json);
      } catch (err) {
        setError(err.message);
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, [selection]);

  // Build breadcrumb label
  const getScopeLabel = () => {
    const parts = [];
    if (selection.server) parts.push(selection.server);
    else parts.push('All Servers');
    if (selection.year) parts.push(selection.year);
    if (selection.month) parts.push(MONTH_NAMES[selection.month]);
    if (selection.day) parts.push(`Day ${selection.day}`);
    return parts.join(' → ');
  };

  return (
    <div className="flex h-screen bg-slate-900 text-slate-200 font-sans">
      {/* Tree Sidebar */}
      {treeLoading ? (
        <div className="w-72 bg-slate-800 border-r border-slate-700 flex items-center justify-center">
          <Loader2 size={24} className="animate-spin text-blue-400" />
        </div>
      ) : (
        <TreeSidebar tree={tree} onSelect={setSelection} currentSelection={selection} />
      )}

      {/* Main Content */}
      <div className="flex-1 overflow-y-auto p-6">
        {/* Header */}
        <header className="max-w-7xl mx-auto mb-6 flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <h1 className="text-2xl font-black text-white flex items-center gap-3 tracking-tight">
              <ServerCrash className="text-blue-500" />
              IIS Log Intelligence
            </h1>
            <div className="flex items-center gap-2 mt-2">
              <span className="text-slate-500 text-sm">Scope:</span>
              <span className="bg-slate-800 text-blue-400 text-sm font-medium px-3 py-1 rounded-full border border-slate-700">
                {getScopeLabel()}
              </span>
              {data && (
                <span className="text-slate-600 text-xs">
                  ({data.overview.filesProcessed} log files parsed)
                </span>
              )}
              {loading && <Loader2 size={14} className="animate-spin text-blue-400" />}
            </div>
          </div>
        </header>

        {error && !data && (
          <div className="max-w-7xl mx-auto">
            <div className="bg-slate-800 rounded-xl p-8 border border-rose-700 text-center max-w-md mx-auto">
              <AlertTriangle className="text-rose-400 mx-auto mb-4" size={48} />
              <h2 className="text-xl font-bold text-white mb-2">Error</h2>
              <p className="text-slate-400">{error}</p>
            </div>
          </div>
        )}

        {data && (
          <main className="max-w-7xl mx-auto space-y-6">
            {/* Top Metrics */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
              <MetricCard title="Total Requests" value={data.overview.totalRequests.toLocaleString()} icon={Globe} colorClass="text-blue-400" />
              <MetricCard title="Unique Visitors" value={data.overview.uniqueVisitors.toLocaleString()} icon={Users} colorClass="text-purple-400" />
              <MetricCard title="Avg Response Time" value={`${data.overview.avgResponseTime} ms`} icon={Clock} colorClass="text-emerald-400" />
              <MetricCard title="Error Rate" value={`${data.overview.errorRate}%`} icon={AlertTriangle} colorClass="text-rose-400" />
            </div>

            {/* Charts Row 1 */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
              {/* Hourly Traffic */}
              <div className="lg:col-span-2 bg-slate-800 rounded-xl p-6 border border-slate-700 shadow-lg">
                <h3 className="text-base font-semibold text-white mb-4 flex items-center gap-2">
                  <Activity className="text-blue-400" size={18} />
                  Hourly Traffic Volume
                </h3>
                <div className="h-64 w-full">
                  <ResponsiveContainer width="100%" height="100%">
                    <LineChart data={data.hourlyTraffic} margin={{ top: 5, right: 20, bottom: 5, left: 0 }}>
                      <CartesianGrid strokeDasharray="3 3" stroke="#334155" vertical={false} />
                      <XAxis dataKey="hour" stroke="#94a3b8" fontSize={11} tickLine={false} axisLine={false} />
                      <YAxis stroke="#94a3b8" fontSize={11} tickLine={false} axisLine={false} />
                      <RechartsTooltip contentStyle={{ backgroundColor: '#1e293b', border: '1px solid #334155', borderRadius: '8px', color: '#f8fafc' }} itemStyle={{ color: '#60a5fa' }} />
                      <Line type="monotone" dataKey="requests" stroke="#3b82f6" strokeWidth={2} dot={{ fill: '#1e293b', stroke: '#3b82f6', strokeWidth: 2, r: 3 }} activeDot={{ r: 5, fill: '#60a5fa' }} />
                    </LineChart>
                  </ResponsiveContainer>
                </div>
              </div>

              {/* Status Codes */}
              <div className="bg-slate-800 rounded-xl p-6 border border-slate-700 shadow-lg flex flex-col">
                <h3 className="text-base font-semibold text-white mb-2 flex items-center gap-2">
                  <Filter className="text-indigo-400" size={18} />
                  HTTP Status Codes
                </h3>
                <div className="flex-1 w-full min-h-[220px]">
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie data={data.statusCodes} innerRadius={55} outerRadius={75} paddingAngle={5} dataKey="value">
                        {data.statusCodes.map((entry, index) => (
                          <Cell key={`cell-${index}`} fill={STATUS_COLORS[entry.name.substring(0,3)] || COLORS[index % COLORS.length]} />
                        ))}
                      </Pie>
                      <RechartsTooltip contentStyle={{ backgroundColor: '#1e293b', border: '1px solid #334155', borderRadius: '8px' }} itemStyle={{ color: '#f8fafc' }} />
                      <Legend verticalAlign="bottom" height={36} iconType="circle" wrapperStyle={{ fontSize: '11px' }} />
                    </PieChart>
                  </ResponsiveContainer>
                </div>
              </div>
            </div>

            {/* Charts Row 2 */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {/* Document Types */}
              <div className="bg-slate-800 rounded-xl p-6 border border-slate-700 shadow-lg">
                <h3 className="text-base font-semibold text-white mb-4 flex items-center gap-2">
                  <FileText className="text-amber-400" size={18} />
                  Document Type Distribution
                </h3>
                <div className="h-64 w-full">
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={data.documentTypes} layout="vertical" margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
                      <CartesianGrid strokeDasharray="3 3" stroke="#334155" horizontal={true} vertical={false} />
                      <XAxis type="number" stroke="#94a3b8" fontSize={11} tickLine={false} axisLine={false} />
                      <YAxis dataKey="name" type="category" stroke="#94a3b8" fontSize={11} tickLine={false} axisLine={false} width={80} />
                      <RechartsTooltip contentStyle={{ backgroundColor: '#1e293b', border: '1px solid #334155', borderRadius: '8px' }} cursor={{ fill: '#334155', opacity: 0.4 }} />
                      <Bar dataKey="value" radius={[0, 4, 4, 0]}>
                        {data.documentTypes.map((_, index) => (
                          <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                        ))}
                      </Bar>
                    </BarChart>
                  </ResponsiveContainer>
                </div>
              </div>

              {/* Top Errors */}
              <div className="bg-slate-800 rounded-xl border border-slate-700 shadow-lg overflow-hidden flex flex-col">
                <div className="p-5 border-b border-slate-700">
                  <h3 className="text-base font-semibold text-white flex items-center gap-2">
                    <AlertTriangle className="text-rose-400" size={18} />
                    Top Error-Causing URLs
                  </h3>
                </div>
                <div className="flex-1 overflow-auto max-h-64">
                  <table className="w-full text-left border-collapse">
                    <thead>
                      <tr className="bg-slate-900/50 text-slate-400 text-xs uppercase tracking-wider sticky top-0">
                        <th className="p-3 font-medium">URL Path</th>
                        <th className="p-3 font-medium w-20">Status</th>
                        <th className="p-3 font-medium text-right w-20">Count</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-700/50">
                      {data.topErrors.map((item, i) => (
                        <tr key={i} className="hover:bg-slate-700/20 transition-colors">
                          <td className="p-3 text-xs font-mono text-slate-300 truncate max-w-[200px]" title={item.url}>{item.url}</td>
                          <td className="p-3 text-xs">
                            <span className={`px-2 py-0.5 rounded text-xs font-bold ${String(item.status).startsWith('4') ? 'bg-amber-500/10 text-amber-500' : 'bg-rose-500/10 text-rose-500'}`}>
                              {item.status}
                            </span>
                          </td>
                          <td className="p-3 text-xs text-right font-medium text-white">{item.count.toLocaleString()}</td>
                        </tr>
                      ))}
                      {data.topErrors.length === 0 && (
                        <tr><td colSpan={3} className="p-6 text-center text-slate-500 text-sm">No errors found</td></tr>
                      )}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>

            {/* Tables Row */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {/* Most Requested */}
              <div className="bg-slate-800 rounded-xl border border-slate-700 shadow-lg overflow-hidden">
                <div className="p-5 border-b border-slate-700">
                  <h3 className="text-base font-semibold text-white flex items-center gap-2">
                    <Globe className="text-emerald-400" size={18} />
                    Most Requested URLs
                  </h3>
                </div>
                <div className="overflow-auto max-h-80">
                  <table className="w-full text-left border-collapse">
                    <thead>
                      <tr className="bg-slate-900/50 text-slate-400 text-xs uppercase tracking-wider sticky top-0">
                        <th className="p-3 font-medium w-8">#</th>
                        <th className="p-3 font-medium">URL Path</th>
                        <th className="p-3 font-medium text-right w-24">Hits</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-700/50">
                      {data.mostRequested.slice(0, 20).map((item, i) => (
                        <tr key={i} className="hover:bg-slate-700/20 transition-colors">
                          <td className="p-3 text-xs text-slate-500">{i + 1}</td>
                          <td className="p-3 text-xs font-mono text-slate-300">{item.url}</td>
                          <td className="p-3 text-xs text-right font-medium text-white">{item.hits.toLocaleString()}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>

              {/* Slowest */}
              <div className="bg-slate-800 rounded-xl border border-slate-700 shadow-lg overflow-hidden">
                <div className="p-5 border-b border-slate-700">
                  <h3 className="text-base font-semibold text-white flex items-center gap-2">
                    <Clock className="text-purple-400" size={18} />
                    Slowest Requests
                  </h3>
                </div>
                <div className="overflow-auto max-h-80">
                  <table className="w-full text-left border-collapse">
                    <thead>
                      <tr className="bg-slate-900/50 text-slate-400 text-xs uppercase tracking-wider sticky top-0">
                        <th className="p-3 font-medium">URL Path</th>
                        <th className="p-3 font-medium w-28">Client IP</th>
                        <th className="p-3 font-medium text-right w-20">Time</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-700/50">
                      {data.slowestRequests.map((item, i) => (
                        <tr key={i} className="hover:bg-slate-700/20 transition-colors">
                          <td className="p-3 text-xs font-mono text-slate-300 truncate max-w-[180px]" title={item.url}>{item.url}</td>
                          <td className="p-3 text-xs text-slate-400">{item.ip}</td>
                          <td className="p-3 text-xs text-right font-medium text-rose-400">{item.time}ms</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          </main>
        )}
      </div>
    </div>
  );
};

export default Dashboard;
