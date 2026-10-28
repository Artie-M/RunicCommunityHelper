import { useCallback, useEffect, useState, type ReactNode } from "react";

export interface Notice {
  kind: "success" | "error";
  text: string;
}

export function useNotice() {
  const [notice, setNotice] = useState<Notice | null>(null);

  useEffect(() => {
    if (notice?.kind !== "success") return;
    const timer = window.setTimeout(() => setNotice(null), 4000);
    return () => window.clearTimeout(timer);
  }, [notice]);

  const success = useCallback((text: string) => setNotice({ kind: "success", text }), []);
  const error = useCallback((text: string) => setNotice({ kind: "error", text }), []);
  const clear = useCallback(() => setNotice(null), []);

  return { notice, success, error, clear };
}

export function NoticeBanner({ notice, onClose }: { notice: Notice | null; onClose: () => void }) {
  if (!notice) return null;
  return (
    <div className={`alert alert-${notice.kind}`} role={notice.kind === "error" ? "alert" : "status"}>
      <span>{notice.text}</span>
      <button className="alert-close" type="button" onClick={onClose} aria-label="Закрыть">
        ×
      </button>
    </div>
  );
}

export function PageHeader({ title, description, children }: { title: string; description: string; children?: ReactNode }) {
  return (
    <header className="page-header">
      <div>
        <h1>{title}</h1>
        <p className="muted">{description}</p>
      </div>
      {children}
    </header>
  );
}

export function TableState({ colSpan, children }: { colSpan: number; children: ReactNode }) {
  return (
    <tr>
      <td className="table-state" colSpan={colSpan}>
        {children}
      </td>
    </tr>
  );
}
