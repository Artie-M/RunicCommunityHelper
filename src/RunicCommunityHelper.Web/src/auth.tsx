import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";

const TOKEN_KEY = "rch.token";

const NAME_CLAIM = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";

interface TokenPayload {
  exp?: number;
  [NAME_CLAIM]?: string;
}

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message);
  }
}

interface RequestOptions {
  method?: "GET" | "POST" | "PUT" | "DELETE";
  body?: unknown;
}

interface AuthContextValue {
  token: string | null;
  username: string | null;
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
  request: <T>(path: string, options?: RequestOptions) => Promise<T>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function decodePayload(token: string): TokenPayload | null {
  try {
    const base64 = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
    const bytes = Uint8Array.from(atob(base64), (c) => c.charCodeAt(0));
    return JSON.parse(new TextDecoder().decode(bytes)) as TokenPayload;
  } catch {
    return null;
  }
}

function isExpired(token: string): boolean {
  const exp = decodePayload(token)?.exp;
  return exp === undefined || exp * 1000 <= Date.now();
}

function readStoredToken(): string | null {
  try {
    const token = localStorage.getItem(TOKEN_KEY);
    return token && !isExpired(token) ? token : null;
  } catch {
    return null;
  }
}

function storeToken(token: string | null) {
  try {
    if (token) localStorage.setItem(TOKEN_KEY, token);
    else localStorage.removeItem(TOKEN_KEY);
  } catch {
    // хранилище недоступно 
  }
}

function statusMessage(status: number): string {
  switch (status) {
    case 400:
      return "Некорректные данные";
    case 403:
      return "Недостаточно прав";
    case 404:
      return "Запись не найдена";
    default:
      return status >= 500 ? "Ошибка сервера" : `Ошибка запроса (${status})`;
  }
}

async function sendRequest<T>(path: string, token: string | null, options: RequestOptions): Promise<T> {
  const headers: Record<string, string> = {};
  if (token) headers.Authorization = `Bearer ${token}`;
  if (options.body !== undefined) headers["Content-Type"] = "application/json";

  let response: Response;
  try {
    response = await fetch(path, {
      method: options.method ?? "GET",
      headers,
      body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
    });
  } catch {
    throw new ApiError(0, "Сервер недоступен");
  }

  if (!response.ok) {
    let message = statusMessage(response.status);
    try {
      const data = (await response.json()) as { error?: string };
      if (data.error) message = data.error;
    } catch {
      // тело ответа пустое или не JSON 
    }
    throw new ApiError(response.status, message);
  }

  const text = await response.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(readStoredToken);

  const logout = useCallback(() => {
    storeToken(null);
    setToken(null);
  }, []);

  const login = useCallback(async (username: string, password: string) => {
    try {
      const { token: newToken } = await sendRequest<{ token: string }>("/api/auth/login", null, {
        method: "POST",
        body: { username, password },
      });
      storeToken(newToken);
      setToken(newToken);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        throw new ApiError(401, "Неверный логин или пароль");
      }
      throw error;
    }
  }, []);

  const request = useCallback(
    async <T,>(path: string, options: RequestOptions = {}): Promise<T> => {
      try {
        return await sendRequest<T>(path, token, options);
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) {
          logout();
          throw new ApiError(401, "Сессия истекла, войдите снова");
        }
        throw error;
      }
    },
    [token, logout],
  );

  useEffect(() => {
    const exp = token ? decodePayload(token)?.exp : undefined;
    if (!exp) return;
    const timer = window.setTimeout(logout, Math.max(exp * 1000 - Date.now(), 0));
    return () => window.clearTimeout(timer);
  }, [token, logout]);

  const value = useMemo<AuthContextValue>(() => {
    const payload = token ? decodePayload(token) : null;
    return {
      token,
      username: payload?.[NAME_CLAIM] ?? null,
      login,
      logout,
      request,
    };
  }, [token, login, logout, request]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used inside AuthProvider");
  return context;
}

export function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Неизвестная ошибка";
}
