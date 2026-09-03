const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";

export interface TodoItem {
  id: string;
  title: string;
  isCompleted: boolean;
  createdAt: string;
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_URL}${path}`, {
    ...init,
    headers: { "Content-Type": "application/json", ...init?.headers },
    cache: "no-store",
  });
  if (!res.ok) {
    throw new Error(`API request to ${path} failed: ${res.status}`);
  }
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

export const api = {
  list: () => request<TodoItem[]>("/api/todoitems"),
  create: (title: string) => request<TodoItem>("/api/todoitems", { method: "POST", body: JSON.stringify({ title }) }),
  update: (id: string, patch: { title?: string; isCompleted?: boolean }) =>
    request<void>(`/api/todoitems/${id}`, { method: "PUT", body: JSON.stringify(patch) }),
  remove: (id: string) => request<void>(`/api/todoitems/${id}`, { method: "DELETE" }),
};
