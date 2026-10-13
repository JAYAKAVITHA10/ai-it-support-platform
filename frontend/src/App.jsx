
import { useCallback, useEffect, useState } from "react";
import { Activity, AlertCircle, ClipboardList, LogOut, Plus, RefreshCw, ShieldCheck, Ticket, X } from "lucide-react";
import "./App.css";

const API_URL = (import.meta.env.VITE_API_URL || "http://localhost:5286").replace(/\/$/, "");

async function apiRequest(path, options = {}, token = "") {
  const response = await fetch(`${API_URL}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers,
    },
  });

  if (!response.ok) {
    let message = `Request failed (${response.status})`;
    try {
      const data = await response.json();
      message = data.message || data.title || message;
    } catch {
      // Keep the status message if the response has no JSON body.
    }
    throw new Error(message);
  }

  if (response.status === 204) return null;
  return response.json();
}

export default function App() {
  const [token, setToken] = useState("");
  const [user, setUser] = useState(null);
  const [mode, setMode] = useState("login");
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [tickets, setTickets] = useState([]);
  const [loading, setLoading] = useState(false);
  const [authLoading, setAuthLoading] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [priority, setPriority] = useState("Medium");
  const [category, setCategory] = useState("Software");

  const loadTickets = useCallback(async (activeToken) => {
    setLoading(true);
    setError("");
    try {
      const data = await apiRequest("/api/tickets", {}, activeToken);
      setTickets(Array.isArray(data) ? data : []);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (token) loadTickets(token);
  }, [token, loadTickets]);

  async function handleAuth(event) {
    event.preventDefault();
    setError("");
    setSuccess("");
    setAuthLoading(true);

    try {
      if (mode === "register") {
        await apiRequest("/api/auth/register", {
          method: "POST",
          body: JSON.stringify({ fullName, email, password }),
        });
        setSuccess("Account created. Please sign in.");
        setMode("login");
        setPassword("");
      } else {
        const result = await apiRequest("/api/auth/login", {
          method: "POST",
          body: JSON.stringify({ email, password }),
        });
        setToken(result.accessToken);
        setUser(result.user);
        setPassword("");
      }
    } catch (err) {
      setError(err.message);
    } finally {
      setAuthLoading(false);
    }
  }

  async function handleCreateTicket(event) {
    event.preventDefault();
    setError("");
    setSuccess("");

    try {
      await apiRequest("/api/tickets", {
        method: "POST",
        body: JSON.stringify({
          title,
          description,
          priority,
          category,
          // The backend must derive the creator from the JWT, not this field.
          createdByUserId: user?.userId ?? 0,
        }),
      }, token);

      setTitle("");
      setDescription("");
      setPriority("Medium");
      setCategory("Software");
      setShowForm(false);
      setSuccess("Ticket created successfully.");
      await loadTickets(token);
    } catch (err) {
      setError(err.message);
    }
  }

  function logout() {
    setToken("");
    setUser(null);
    setTickets([]);
    setError("");
    setSuccess("");
    setShowForm(false);
  }

  const openCount = tickets.filter((ticket) =>
    ["Open", "In Progress", "Pending"].includes(ticket.status)
  ).length;

  const criticalCount = tickets.filter((ticket) =>
    ticket.priority === "Critical"
  ).length;

  if (!token) {
    return (
      <main className="auth-page">
        <section className="auth-visual">
          <div className="brand"><span className="brand-icon"><ShieldCheck /></span> ResolveIT</div>
          <div className="auth-message">
            <span className="eyebrow">IT SUPPORT & INCIDENT MANAGEMENT</span>
            <h1>Support that keeps your work moving.</h1>
            <p>Report technical issues, track resolutions, and keep every request organized in one place.</p>
            <div className="auth-feature"><Activity size={19} /> A clearer view of every incident</div>
            <div className="auth-feature"><ShieldCheck size={19} /> Secure, role-based access</div>
          </div>
          <p className="auth-footer">Your workplace support hub</p>
        </section>

        <section className="auth-panel">
          <form className="auth-card" onSubmit={handleAuth}>
            <div className="mobile-brand"><ShieldCheck /> ResolveIT</div>
            <span className="eyebrow">{mode === "login" ? "WELCOME BACK" : "GET STARTED"}</span>
            <h2>{mode === "login" ? "Sign in to your account" : "Create your account"}</h2>
            <p className="muted">
              {mode === "login" ? "Enter your details to access your workspace." : "Register as a requester to start raising tickets."}
            </p>

            {mode === "register" && (
              <label>Full name
                <input value={fullName} onChange={(e) => setFullName(e.target.value)} required minLength={2} maxLength={100} placeholder="Your full name" />
              </label>
            )}
            <label>Email address
              <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required placeholder="you@example.com" />
            </label>
            <label>Password
              <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required minLength={8} placeholder="At least 8 characters" />
            </label>

            {error && <div className="alert error"><AlertCircle size={18} /> {error}</div>}
            {success && <div className="alert success">{success}</div>}

            <button className="primary-button full-button" disabled={authLoading}>
              {authLoading ? "Please wait..." : mode === "login" ? "Sign in" : "Create account"}
            </button>
            <p className="switch-auth">
              {mode === "login" ? "New to ResolveIT?" : "Already have an account?"}{" "}
              <button type="button" className="text-button" onClick={() => { setMode(mode === "login" ? "register" : "login"); setError(""); setSuccess(""); }}>
                {mode === "login" ? "Create an account" : "Sign in"}
              </button>
            </p>
          </form>
        </section>
      </main>
    );
  }

  return (
    <main className="app-shell">
      <aside className="sidebar">
        <div className="brand"><span className="brand-icon"><ShieldCheck /></span> ResolveIT</div>
        <div className="side-label">WORKSPACE</div>
        <div className="nav-item active"><ClipboardList size={19} /> My tickets</div>
        <div className="sidebar-bottom">
          <div className="user-avatar">{user?.fullName?.charAt(0)?.toUpperCase() || "U"}</div>
          <div className="user-details"><strong>{user?.fullName}</strong><span>{user?.role}</span></div>
          <button className="icon-button" title="Sign out" onClick={logout}><LogOut size={18} /></button>
        </div>
      </aside>

      <section className="main-content">
        <header className="topbar">
          <div><span className="eyebrow">WORKSPACE / OVERVIEW</span><h1>My tickets</h1></div>
          <div className="top-actions">
            <button className="secondary-button" onClick={() => loadTickets(token)} disabled={loading}><RefreshCw size={16} /> Refresh</button>
            {user?.role === "Requester" && <button className="primary-button" onClick={() => { setShowForm(true); setError(""); setSuccess(""); }}><Plus size={18} /> New ticket</button>}
          </div>
        </header>

        {error && <div className="alert error page-alert"><AlertCircle size={18} /> {error}</div>}
        {success && <div className="alert success page-alert">{success}</div>}

        <div className="welcome-row">
          <div><h2>Hello, {user?.fullName?.split(" ")[0] || "there"} 👋</h2><p className="muted">Here's the latest overview of your support requests.</p></div>
          <div className="secure-tag"><ShieldCheck size={16} /> Signed in securely</div>
        </div>

        <div className="stats-grid">
          <article className="stat-card"><div className="stat-icon blue"><ClipboardList /></div><span>Total tickets</span><strong>{tickets.length}</strong><small>Tickets visible to you</small></article>
          <article className="stat-card"><div className="stat-icon orange"><Activity /></div><span>Active tickets</span><strong>{openCount}</strong><small>Open, pending or in progress</small></article>
          <article className="stat-card"><div className="stat-icon red"><AlertCircle /></div><span>Critical priority</span><strong>{criticalCount}</strong><small>Requires urgent attention</small></article>
        </div>

        <section className="tickets-panel">
          <div className="panel-heading"><div><h2>Recent tickets</h2><p className="muted">Track your reported issues and their current status.</p></div><Ticket size={22} /></div>
          {loading ? <div className="empty-state">Loading your tickets...</div> :
            tickets.length === 0 ? <div className="empty-state"><ClipboardList size={34} /><h3>No tickets yet</h3><p>When you report an issue, it will appear here.</p>{user?.role === "Requester" && <button className="primary-button" onClick={() => setShowForm(true)}><Plus size={17} /> Create your first ticket</button>}</div> :
            <div className="table-wrap"><table><thead><tr><th>Ticket</th><th>Category</th><th>Priority</th><th>Status</th><th>Created</th></tr></thead><tbody>
              {tickets.map((ticket) => <tr key={ticket.ticketId}>
                <td><strong>#{ticket.ticketId} {ticket.title}</strong><small className="ticket-description">{ticket.description}</small></td>
                <td>{ticket.category}</td>
                <td><span className={`priority priority-${ticket.priority?.toLowerCase()}`}>{ticket.priority}</span></td>
                <td><span className={`status status-${ticket.status?.toLowerCase().replace(/\s+/g, "-")}`}>{ticket.status}</span></td>
                <td>{ticket.createdAt ? new Date(ticket.createdAt).toLocaleDateString() : "—"}</td>
              </tr>)}
            </tbody></table></div>
          }
        </section>
        <footer className="page-footer">ResolveIT · IT Support & Incident Management</footer>
      </section>

      {showForm && <div className="modal-backdrop" onMouseDown={(e) => { if (e.target === e.currentTarget) setShowForm(false); }}>
        <form className="ticket-modal" onSubmit={handleCreateTicket}>
          <div className="modal-heading"><div><span className="eyebrow">SUPPORT REQUEST</span><h2>Create a ticket</h2></div><button type="button" className="icon-button" onClick={() => setShowForm(false)}><X /></button></div>
          <p className="muted">Describe the issue so the support team can help you.</p>
          <label>Ticket title<input value={title} onChange={(e) => setTitle(e.target.value)} required minLength={3} maxLength={200} placeholder="e.g. Unable to access company email" /></label>
          <label>Description<textarea value={description} onChange={(e) => setDescription(e.target.value)} required minLength={5} maxLength={5000} rows={5} placeholder="Explain what happened and what you were trying to do..." /></label>
          <div className="form-row">
            <label>Category<select value={category} onChange={(e) => setCategory(e.target.value)}><option>Software</option><option>Hardware</option><option>Network</option><option>Account Access</option><option>Email</option><option>Other</option></select></label>
            <label>Priority<select value={priority} onChange={(e) => setPriority(e.target.value)}><option>Low</option><option>Medium</option><option>High</option><option>Critical</option></select></label>
          </div>
          <div className="modal-actions"><button type="button" className="secondary-button" onClick={() => setShowForm(false)}>Cancel</button><button className="primary-button"><Plus size={17} /> Submit ticket</button></div>
        </form>
      </div>}
    </main>
  );
}
