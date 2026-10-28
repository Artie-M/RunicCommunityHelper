import { useCallback, useEffect, useMemo, useState } from "react";
import { errorMessage, useAuth } from "../auth";
import { NoticeBanner, PageHeader, TableState, useNotice } from "../components/ui";
import type { DiscordUser } from "../types";

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function UsersPage() {
  const { request } = useAuth();
  const { notice, success, error, clear } = useNotice();
  const [users, setUsers] = useState<DiscordUser[] | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [editingId, setEditingId] = useState<number | null>(null);
  const [email, setEmail] = useState("");
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    setLoadError(null);
    try {
      setUsers(await request<DiscordUser[]>("/api/discordusers"));
    } catch (e) {
      setLoadError(errorMessage(e));
    }
  }, [request]);

  useEffect(() => {
    void load();
  }, [load]);

  const visibleUsers = useMemo(() => {
    const query = search.trim().toLowerCase();
    if (!users || !query) return users;
    return users.filter((user) => user.email.toLowerCase().includes(query) || user.discordId.includes(query));
  }, [users, search]);

  function startEdit(user: DiscordUser) {
    clear();
    setEditingId(user.id);
    setEmail(user.email);
  }

  async function handleSave(user: DiscordUser) {
    const value = email.trim();
    if (!emailPattern.test(value) || value.length > 128) {
      return error("Введите корректный адрес почты (не длиннее 128 символов)");
    }

    setBusy(true);
    try {
      await request(`/api/discordusers/${user.id}`, {
        method: "PUT",
        body: { id: user.id, discordId: user.discordId, email: value },
      });
      setUsers((current) => current?.map((u) => (u.id === user.id ? { ...u, email: value } : u)) ?? null);
      setEditingId(null);
      success("Почта обновлена");
    } catch (e) {
      error(errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="page">
      <PageHeader title="Пользователи" description="Discord-пользователи, привязавшие почту Google через бота.">
        <input
          className="search"
          type="search"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Поиск по почте или Discord ID"
          aria-label="Поиск"
        />
      </PageHeader>

      <NoticeBanner notice={notice} onClose={clear} />

      <p className="hint">
        Смена почты здесь меняет только запись в базе: доступ старой почты к документам не отзывается, а новая получит
        доступ, когда пользователь снова нажмёт кнопку в Discord.
      </p>

      <div className="card table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th className="col-id">ID</th>
              <th>Discord ID</th>
              <th>Почта</th>
              <th className="col-actions" aria-label="Действия" />
            </tr>
          </thead>
          <tbody>
            {loadError && (
              <TableState colSpan={4}>
                {loadError}{" "}
                <button className="link-button" type="button" onClick={load}>
                  Повторить
                </button>
              </TableState>
            )}
            {!loadError && visibleUsers === null && <TableState colSpan={4}>Загрузка…</TableState>}
            {visibleUsers?.length === 0 && (
              <TableState colSpan={4}>{search ? "Ничего не найдено" : "Пользователей пока нет"}</TableState>
            )}
            {visibleUsers?.map((user) => (
              <tr key={user.id} className={editingId === user.id ? "editing" : undefined}>
                <td className="col-id">{user.id}</td>
                <td>
                  <a className="mono nowrap" href={`https://discord.com/users/${user.discordId}`} target="_blank" rel="noreferrer">
                    {user.discordId}
                  </a>
                </td>
                <td>
                  {editingId === user.id ? (
                    <input
                      type="email"
                      aria-label="Почта"
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      onKeyDown={(e) => {
                        if (e.key === "Enter") void handleSave(user);
                        if (e.key === "Escape") setEditingId(null);
                      }}
                      autoFocus
                    />
                  ) : (
                    user.email
                  )}
                </td>
                <td className="col-actions">
                  {editingId === user.id ? (
                    <>
                      <button className="button button-primary" type="button" disabled={busy} onClick={() => handleSave(user)}>
                        Сохранить
                      </button>
                      <button className="button button-ghost" type="button" disabled={busy} onClick={() => setEditingId(null)}>
                        Отмена
                      </button>
                    </>
                  ) : (
                    <button className="button button-ghost" type="button" disabled={busy} onClick={() => startEdit(user)}>
                      Изменить
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
