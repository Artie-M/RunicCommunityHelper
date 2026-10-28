import { NavLink, Outlet } from "react-router-dom";
import { useAuth } from "../auth";

const navigation = [
  { to: "/documents", label: "Документы" },
  { to: "/users", label: "Пользователи" },
];

export function Layout() {
  const { username, logout } = useAuth();

  return (
    <div className="layout">
      <aside className="sidebar">
        <div className="brand">
          <span className="brand-mark" aria-hidden="true">
            ᚱ
          </span>
          <span>Runic Community Helper</span>
        </div>

        <nav className="nav">
          {navigation.map((item) => (
            <NavLink key={item.to} to={item.to} className={({ isActive }) => (isActive ? "nav-link active" : "nav-link")}>
              {item.label}
            </NavLink>
          ))}
        </nav>

        <div className="sidebar-footer">
          {username && <span className="muted">{username}</span>}
          <button className="button button-ghost" type="button" onClick={logout}>
            Выйти
          </button>
        </div>
      </aside>

      <main className="content">
        <Outlet />
      </main>
    </div>
  );
}
