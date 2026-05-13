import { useState, useEffect } from "react";
import Editor from "@monaco-editor/react";
import DiffViewer from "./components/DiffViewer";
import ScoreCircle from "./components/ScoreCircle";
import HistoryPanel from "./components/HistoryPanel";
import Auth from "./components/Auth";

const API_BASE_URL = "http://localhost:5120/api";

const aiSteps = [
  "Connecting to AI...",
  "Parsing source code...",
  "Checking syntax...",
  "Analyzing complexity...",
  "Optimizing code...",
  "Security audit...",
  "Generating suggestions..."
];

// Helper function to make API calls with session
const apiCall = async (endpoint, options = {}) => {
  const token = localStorage.getItem("token");
  const sessionId = localStorage.getItem("sessionId");

  const headers = {
    "Content-Type": "application/json",
    ...(token && { "Authorization": `Bearer ${token}` }),
    ...(sessionId && { "X-Session-Id": sessionId }),
    ...options.headers
  };

  try {
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      ...options,
      headers
    });

    // Handle session expiration
    if (response.status === 401) {
      const data = await response.json().catch(() => ({}));
      localStorage.removeItem("token");
      localStorage.removeItem("sessionId");
      localStorage.removeItem("user");
      throw new Error("session_expired");
    }

    return response;
  } catch (error) {
    if (error.message === "session_expired") {
      throw error;
    }
    console.error("API call error:", error);
    throw error;
  }
};

export default function App() {
  const [language, setLanguage] = useState("python");
  const [code, setCode] = useState("");
  const [loading, setLoading] = useState(false);
  const [step, setStep] = useState(0);
  const [result, setResult] = useState(null);
  const [history, setHistory] = useState([]);
  const [modalData, setModalData] = useState(null);
  const [error, setError] = useState(null);
  const [sessions, setSessions] = useState([]);
  const [showSessions, setShowSessions] = useState(false);
  const [authChecked, setAuthChecked] = useState(false);

  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [user, setUser] = useState(null);

  // Check authentication on mount
  useEffect(() => {
    const token = localStorage.getItem("token");
    const userData = localStorage.getItem("user");
    
    if (token && userData) {
      setIsAuthenticated(true);
      setUser(JSON.parse(userData));
    }
    setAuthChecked(true);
  }, []);

  // Load history from localStorage
  useEffect(() => {
    const saved = localStorage.getItem("codelens-history");
    if (saved) setHistory(JSON.parse(saved));
  }, []);

  // Save history to localStorage
  useEffect(() => {
    localStorage.setItem("codelens-history", JSON.stringify(history));
  }, [history]);

  const handleLogin = (userData) => {
    setIsAuthenticated(true);
    setUser(userData);
  };

  const handleLogout = () => {
    localStorage.removeItem("token");
    localStorage.removeItem("sessionId");
    localStorage.removeItem("user");
    setIsAuthenticated(false);
    setUser(null);
    setSessions([]);
  };

  const loadSessions = async () => {
    try {
      const response = await apiCall("/Auth/sessions");
      const data = await response.json();
      setSessions(data);
    } catch (error) {
      if (error.message === "session_expired") {
        handleLogout();
      }
    }
  };

  const endSession = async (sessionId) => {
    try {
      await apiCall(`/Auth/sessions/${sessionId}/end`, { method: "POST" });
      await loadSessions();
    } catch (error) {
      if (error.message === "session_expired") {
        handleLogout();
      }
    }
  };

  const endOtherSessions = async () => {
    try {
      await apiCall("/Auth/sessions/end-others", { method: "POST" });
      await loadSessions();
    } catch (error) {
      if (error.message === "session_expired") {
        handleLogout();
      }
    }
  };

  const runAnalysis = async () => {
    if (!code.trim()) {
      alert("Please enter some code to analyze");
      return;
    }

    setLoading(true);
    setError(null);
    setResult(null);
    setStep(0);

    const interval = setInterval(() => {
      setStep((prev) => (prev < aiSteps.length - 1 ? prev + 1 : prev));
    }, 800);

    try {
      const response = await apiCall("/AIAnalysis/analyze", {
        method: "POST",
        body: JSON.stringify({
          code: code,
          language: language,
          fileName: `code.${language}`,
          options: {
            fixErrors: true,
            optimizeCode: true,
            addComments: true,
            securityCheck: true,
            optimizationLevel: "balanced"
          }
        }),
      });

      const data = await response.json();
      console.log("Analysis response:", data);

      const formattedResult = {
        id: data.id || Date.now(),
        language: data.language,
        original: data.originalCode,
        optimized: data.optimizedCode,
        errors: data.errors || [],
        suggestions: data.suggestions || [],
        score: data.qualityScore || 85,
        time: data.complexity?.timeComplexity || "O(n)",
        space: data.complexity?.spaceComplexity || "O(1)",
        securityIssues: data.securityIssues || []
      };

      const historyItem = {
        id: data.id || Date.now(),
        language: data.language,
        originalCode: data.originalCode,
        optimizedCode: data.optimizedCode,
        score: data.qualityScore || 85,
        timestamp: new Date().toISOString()
      };

      setHistory(prev => [historyItem, ...prev].slice(0, 10));
      setResult(formattedResult);

    } catch (err) {
      console.error("Analysis error:", err);
      if (err.message === "session_expired") {
        handleLogout();
      } else {
        setError(err.message);
      }
    } finally {
      clearInterval(interval);
      setLoading(false);
    }
  };

  const handleView = (item) => {
    setModalData({
      original: item.originalCode || item.original,
      optimized: item.optimizedCode || item.optimized
    });
  };

  const handleDelete = (id) =>
    setHistory(history.filter((item) => item.id !== id));

  // Show nothing while checking authentication
  if (!authChecked) {
    return null;
  }

  if (!isAuthenticated) {
    return <Auth onLogin={handleLogin} />;
  }

  return (
    <div className="w-full min-h-screen bg-[#0b1220] flex justify-center">
      <div className="w-[1400px] px-8 py-8">

        <div className="flex justify-between items-center mb-8">
          <h1 className="text-3xl font-semibold text-emerald-400 flex items-center gap-3 mr-12">
            🚀 CodeLens AI Code Review Assistant
          </h1>
          <div className="flex items-center gap-4">
            {user && (
              <span className="text-gray-300 text-base">
                👋 {user.firstName} {user.lastName}
              </span>
            )}
            <button
              onClick={() => {
                setShowSessions(!showSessions);
                if (!showSessions) loadSessions();
              }}
              className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-lg text-sm transition"
            >
              {showSessions ? "Hide Sessions" : "Active Sessions"}
            </button>
            <button
              onClick={handleLogout}
              className="bg-red-600 hover:bg-red-700 text-white px-4 py-2 rounded-lg text-sm transition"
            >
              Logout
            </button>
          </div>
        </div>

        {/* Sessions Panel */}
        {showSessions && (
          <div className="mb-6 bg-[#111827] border border-gray-800 rounded-xl p-6">
            <div className="flex justify-between items-center mb-4">
              <h2 className="text-xl font-semibold text-white">Active Sessions</h2>
              <button
                onClick={endOtherSessions}
                className="bg-yellow-600 hover:bg-yellow-700 text-white px-3 py-1 rounded text-sm"
              >
                End Other Sessions
              </button>
            </div>
            <div className="grid gap-4">
              {sessions.map((session) => (
                <div key={session.sessionId} className="bg-gray-800 rounded-lg p-4 flex justify-between items-center">
                  <div>
                    <div className="text-white font-medium">{session.deviceInfo || "Unknown Device"}</div>
                    <div className="text-gray-400 text-sm">
                      IP: {session.ipAddress || "Unknown"} • 
                      Last activity: {session.lastActivityAt ? new Date(session.lastActivityAt).toLocaleString() : "Unknown"}
                    </div>
                  </div>
                  {session.sessionId !== localStorage.getItem("sessionId") && (
                    <button
                      onClick={() => endSession(session.sessionId)}
                      className="bg-red-600 hover:bg-red-700 text-white px-3 py-1 rounded text-sm"
                    >
                      End Session
                    </button>
                  )}
                  {session.sessionId === localStorage.getItem("sessionId") && (
                    <span className="text-emerald-400 text-sm">Current Session</span>
                  )}
                </div>
              ))}
              {sessions.length === 0 && (
                <div className="text-gray-400 text-center py-4">No active sessions</div>
              )}
            </div>
          </div>
        )}

        <select
          value={language}
          onChange={(e) => setLanguage(e.target.value)}
          className="mb-6 px-4 py-2 bg-[#111827] border border-gray-700 rounded-lg text-sm text-gray-200"
        >
          <option value="javascript">JavaScript</option>
          <option value="python">Python</option>
          <option value="java">Java</option>
          <option value="c">C</option>
          <option value="cpp">C++</option>
        </select>

        <div className="w-full border border-gray-800 rounded-xl overflow-hidden shadow-lg">
          <Editor
            height="380px"
            theme="vs-dark"
            language={language}
            value={code}
            onChange={(value) => setCode(value || "")}
          />
        </div>

        <button
          onClick={runAnalysis}
          disabled={loading}
          className="mt-6 bg-emerald-600 hover:bg-emerald-700 text-white px-6 py-2 rounded-lg font-medium transition disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {loading ? "Analyzing..." : "Run AI Analysis"}
        </button>

        {loading && (
          <div className="mt-6 bg-[#111827] border border-gray-800 p-4 rounded-xl">
            {aiSteps.map((s, i) => (
              <div
                key={i}
                className={i <= step ? "text-emerald-400" : "text-gray-500"}
              >
                {i <= step ? "✔" : "○"} {s}
              </div>
            ))}
          </div>
        )}

        {error && (
          <div className="mt-6 bg-red-900 border border-red-700 p-5 rounded-xl text-white">
            <h3 className="text-red-300 font-semibold text-lg mb-2">Error</h3>
            <div>{error}</div>
          </div>
        )}

        {result && (
          <div className="mt-8 space-y-6">
            
            {/* Security Issues Section */}
            {result.securityIssues?.length > 0 && (
              <div className="bg-orange-900 border border-orange-700 p-5 rounded-xl text-white">
                <h3 className="text-orange-300 font-semibold text-lg mb-2">⚠️ Security Issues</h3>
                {result.securityIssues.map((issue, i) => {
                  const cleanIssue = issue.replace(/^•\s*/, '').replace(/\*\*/g, '');
                  return <div key={i} className="mb-2 text-gray-200">• {cleanIssue}</div>;
                })}
              </div>
            )}

            {/* Syntax Errors Section - FIXED */}
            {result.errors && result.errors.length > 0 ? (
              <div className="bg-red-900 border border-red-700 p-5 rounded-xl text-white">
                <h3 className="text-red-300 font-semibold text-lg mb-2">
                  Syntax Errors ({result.errors.length})
                </h3>
                <div className="space-y-2 font-mono">
                  {result.errors.map((err, i) => {
                    // Handle each error object - combine into single line
                    let line = '';
                    let message = '';
                    
                    if (typeof err === 'object' && err !== null) {
                      line = err.line || '';
                      message = err.message || '';
                    } else if (typeof err === 'string') {
                      try {
                        const parsed = JSON.parse(err);
                        line = parsed.line || '';
                        message = parsed.message || '';
                      } catch {
                        message = err;
                      }
                    }
                    
                    // Clean the message
                    message = message.replace(/[{}"']/g, '').trim();
                    
                    // Capitalize first letter
                    if (message.length > 0) {
                      message = message.charAt(0).toUpperCase() + message.slice(1);
                    }
                    
                    // Handle specific error messages
                    if (message.includes("Can't convert") || message.includes("concatenate")) {
                      message = "Can't concatenate string and number, use str(result) or f-string";
                    }
                    
                    // Format as a single line
                    return (
                      <div key={i} className="text-gray-200 border-l-2 border-red-500 pl-3 py-1">
                        line {line}: {message}
                      </div>
                    );
                  })}
                </div>
              </div>
            ) : null}

            {/* Score and Complexity Section */}
            <div className="flex items-center gap-10 bg-[#111827] border border-gray-800 p-6 rounded-xl">
              <ScoreCircle score={result.score} />
              <div className="text-lg text-gray-300">
                <div>Time Complexity: {result.time}</div>
                <div>Space Complexity: {result.space}</div>
              </div>
            </div>

            {/* Diff Viewer */}
            <DiffViewer original={result.original} optimized={result.optimized} />

            {/* Suggestions Section */}
            <div className="bg-[#111827] border border-gray-800 p-6 rounded-xl">
              <h3 className="text-yellow-400 font-semibold text-lg mb-4">
                Suggestions
              </h3>

              {result.suggestions.map((suggestion, index) => {
                let text = suggestion.replace(/^\d+\.\s*/, '');
                const boldTitleMatch = text.match(/^\*\*(.*?)\*\*:\s*(.*)$/);
                
                if (boldTitleMatch) {
                  const title = boldTitleMatch[1];
                  const description = boldTitleMatch[2];
                  return (
                    <div key={index} className="mb-4">
                      <span className="font-bold text-white">{title}:</span>
                      <span className="text-gray-400"> {description}</span>
                    </div>
                  );
                }
                
                return <div key={index} className="mb-2 text-gray-300">• {text.replace(/\*\*/g, '')}</div>;
              })}
            </div>
          </div>
        )}

        <HistoryPanel history={history} onView={handleView} onDelete={handleDelete} />

        {modalData && (
          <div className="fixed inset-0 bg-black bg-opacity-70 flex items-center justify-center z-50">
            <div className="bg-[#111827] p-6 rounded-xl w-11/12 max-w-6xl relative">
              <button onClick={() => setModalData(null)} className="absolute top-3 right-4 text-white text-xl">✖</button>
              <DiffViewer original={modalData.original} optimized={modalData.optimized} />
            </div>
          </div>
        )}
      </div>
    </div>
  );
}