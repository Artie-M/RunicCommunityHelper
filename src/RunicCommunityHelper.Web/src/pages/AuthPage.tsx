import { useState, type FormEvent } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { errorMessage, useAuth } from "../auth";

export function AuthPage() {
  const { token, login } = useAuth();
  const location = useLocation();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  if (token) {
    const from = (location.state as { from?: string } | null)?.from;
    return <Navigate to={from && from !== "/auth" ? from : "/"} replace />;
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setPending(true);
    try {
      await login(username.trim(), password);
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setPending(false);
    }
  }

  return (
    <main className="auth">
      <form className="auth-card" onSubmit={handleSubmit}>
        <div className="brand brand-large">
          <span className="brand-mark" aria-hidden="true">
            ᚱ
          </span>
          <span>Runic Community Helper</span>
        </div>
        <h1>Вход в панель администратора</h1>

        <label className="field">
          <span>Логин</span>
          <input
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            autoComplete="username"
            autoFocus
            required
          />
        </label>

        <label className="field">
          <span>Пароль</span>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="current-password"
            required
          />
        </label>

        {error && (
          <p className="alert alert-error" role="alert">
            {error}
          </p>
        )}

        <button className="button button-primary button-block" type="submit" disabled={pending}>
          {pending ? "Вход…" : "Войти"}
        </button>
      </form>
    </main>
  );
}
