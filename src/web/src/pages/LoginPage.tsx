import { Link } from "react-router-dom";

export function LoginPage() {
  return (
    <div className="login-page">
      <div className="login-card">
        <h1>Sign in</h1>
        <p className="muted">
          Microsoft Entra External ID integration is wired on the API. Connect your SPA registration and MSAL here.
        </p>
        <button type="button" className="btn primary" disabled>
          Continue with Microsoft (placeholder)
        </button>
        <p className="hint">
          For local development, the API can run with <code>Auth:DevelopmentBypass:Enabled</code> so you do not need a
          token from the browser.
        </p>
        <Link to="/dashboard" className="link">
          Skip to dashboard (dev)
        </Link>
      </div>
    </div>
  );
}
