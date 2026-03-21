from fastapi import FastAPI, Query, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from parser import parse_logs_detailed, get_tree_structure
import uvicorn

app = FastAPI(title="IIS Log Intelligence API")

# Enable CORS for the React frontend
app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:5173", "http://127.0.0.1:5173", "*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

@app.get("/api/health")
def health_check():
    return {"status": "ok", "message": "Log parser online"}

@app.get("/api/tree")
def get_tree():
    """Returns the hierarchical tree structure of all servers/years/months/days."""
    try:
        tree = get_tree_structure()
        return tree
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Tree scan error: {str(e)}")

@app.get("/api/dashboard")
def get_dashboard_data(
    server: str = Query(None, description="Server folder name (e.g. SVC1). Omit for all servers."),
    year: str = Query(None, description="4 digit year (e.g. 2022). Omit for all years."),
    month: str = Query(None, description="2 digit month (e.g. 01). Omit for all months."),
    day: str = Query(None, description="2 digit day (e.g. 15). Omit for all days.")
):
    try:
        data = parse_logs_detailed(server=server, year=year, month=month, day=day)
        return data
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Log parsing error: {str(e)}")

if __name__ == "__main__":
    uvicorn.run("main:app", host="0.0.0.0", port=8000, reload=True)
