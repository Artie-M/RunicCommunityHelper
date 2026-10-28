import { useCallback, useEffect, useState, type FormEvent } from "react";
import { errorMessage, useAuth } from "../auth";
import { NoticeBanner, PageHeader, TableState, useNotice } from "../components/ui";
import type { GoogleDocument } from "../types";

interface DocumentDraft {
  fileId: string;
  requiredRole: string;
}

const emptyDraft: DocumentDraft = { fileId: "", requiredRole: "" };
function parseFileId(input: string): string {
  const value = input.trim();
  return value.match(/\/d\/([\w-]+)/)?.[1] ?? value.match(/[?&]id=([\w-]+)/)?.[1] ?? value;
}

function validate(draft: DocumentDraft): string | null {
  if (!/^[\w-]{10,128}$/.test(draft.fileId)) {
    return "ID документа должен состоять из латинских букв, цифр, «-» и «_» (от 10 до 128 символов)";
  }
  if (!/^\d{17,20}$/.test(draft.requiredRole)) {
    return "ID роли — число из 17–20 цифр (ПКМ по роли в Discord → «Копировать ID роли»)";
  }
  return null;
}

function normalize(draft: DocumentDraft): DocumentDraft {
  return { fileId: parseFileId(draft.fileId), requiredRole: draft.requiredRole.trim() };
}

export function DocumentsPage() {
  const { request } = useAuth();
  const { notice, success, error, clear } = useNotice();
  const [documents, setDocuments] = useState<GoogleDocument[] | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [newDocument, setNewDocument] = useState<DocumentDraft>(emptyDraft);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [draft, setDraft] = useState<DocumentDraft>(emptyDraft);
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    setLoadError(null);
    try {
      setDocuments(await request<GoogleDocument[]>("/api/googledocuments"));
    } catch (e) {
      setLoadError(errorMessage(e));
    }
  }, [request]);

  useEffect(() => {
    void load();
  }, [load]);

  async function handleAdd(event: FormEvent) {
    event.preventDefault();
    const value = normalize(newDocument);
    const validationError = validate(value);
    if (validationError) return error(validationError);

    setBusy(true);
    try {
      const created = await request<GoogleDocument>("/api/googledocuments", { method: "POST", body: value });
      setDocuments((current) => [...(current ?? []), created]);
      setNewDocument(emptyDraft);
      success("Документ добавлен");
    } catch (e) {
      error(errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  function startEdit(document: GoogleDocument) {
    clear();
    setEditingId(document.id);
    setDraft({ fileId: document.fileId, requiredRole: document.requiredRole });
  }

  async function handleSave(id: number) {
    const value = normalize(draft);
    const validationError = validate(value);
    if (validationError) return error(validationError);

    setBusy(true);
    try {
      await request(`/api/googledocuments/${id}`, { method: "PUT", body: { id, ...value } });
      setDocuments((current) => current?.map((doc) => (doc.id === id ? { ...doc, ...value } : doc)) ?? null);
      setEditingId(null);
      success("Изменения сохранены");
    } catch (e) {
      error(errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  async function handleDelete(document: GoogleDocument) {
    const confirmed = window.confirm(
      `Удалить запись о документе ${document.fileId}?\n\nДоступ, уже выданный пользователям, автоматически не отзывается.`,
    );
    if (!confirmed) return;

    setBusy(true);
    try {
      await request(`/api/googledocuments/${document.id}`, { method: "DELETE" });
      setDocuments((current) => current?.filter((doc) => doc.id !== document.id) ?? null);
      success("Запись удалена");
    } catch (e) {
      error(errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="page">
      <PageHeader
        title="Документы"
        description="Какая роль Discord даёт доступ к какому документу Google. Один документ можно привязать к нескольким ролям."
      />

      <NoticeBanner notice={notice} onClose={clear} />

      <form className="card add-form" onSubmit={handleAdd}>
        <label className="field">
          <span>ID документа или ссылка</span>
          <input
            value={newDocument.fileId}
            onChange={(e) => setNewDocument({ ...newDocument, fileId: e.target.value })}
            placeholder="https://docs.google.com/spreadsheets/d/…"
          />
        </label>
        <label className="field">
          <span>ID роли</span>
          <input
            value={newDocument.requiredRole}
            onChange={(e) => setNewDocument({ ...newDocument, requiredRole: e.target.value })}
            placeholder="936422104788791337"
            inputMode="numeric"
          />
        </label>
        <button className="button button-primary" type="submit" disabled={busy}>
          Добавить
        </button>
      </form>
      <p className="hint">
        Новая запись не выдаёт доступ сразу: пользователи получат его, когда снова нажмут кнопку «Обновить доступ» в
        Discord.
      </p>

      <div className="card table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th className="col-id">ID</th>
              <th>ID документа</th>
              <th>ID роли</th>
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
            {!loadError && documents === null && <TableState colSpan={4}>Загрузка…</TableState>}
            {documents?.length === 0 && <TableState colSpan={4}>Документов пока нет</TableState>}
            {documents?.map((document) =>
              editingId === document.id ? (
                <tr key={document.id} className="editing">
                  <td className="col-id">{document.id}</td>
                  <td>
                    <input
                      aria-label="ID документа"
                      value={draft.fileId}
                      onChange={(e) => setDraft({ ...draft, fileId: e.target.value })}
                      autoFocus
                    />
                  </td>
                  <td>
                    <input
                      aria-label="ID роли"
                      value={draft.requiredRole}
                      onChange={(e) => setDraft({ ...draft, requiredRole: e.target.value })}
                      inputMode="numeric"
                    />
                  </td>
                  <td className="col-actions">
                    <button className="button button-primary" type="button" disabled={busy} onClick={() => handleSave(document.id)}>
                      Сохранить
                    </button>
                    <button className="button button-ghost" type="button" disabled={busy} onClick={() => setEditingId(null)}>
                      Отмена
                    </button>
                  </td>
                </tr>
              ) : (
                <tr key={document.id}>
                  <td className="col-id">{document.id}</td>
                  <td>
                    <a
                      className="mono"
                      href={`https://drive.google.com/open?id=${encodeURIComponent(document.fileId)}`}
                      target="_blank"
                      rel="noreferrer"
                    >
                      {document.fileId}
                    </a>
                  </td>
                  <td className="mono nowrap">{document.requiredRole}</td>
                  <td className="col-actions">
                    <button className="button button-ghost" type="button" disabled={busy} onClick={() => startEdit(document)}>
                      Изменить
                    </button>
                    <button className="button button-danger" type="button" disabled={busy} onClick={() => handleDelete(document)}>
                      Удалить
                    </button>
                  </td>
                </tr>
              ),
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}
