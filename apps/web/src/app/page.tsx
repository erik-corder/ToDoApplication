"use client";

import { useEffect, useState } from "react";

import { api, type TodoItem } from "@/lib/api";

export default function Home() {
  const [items, setItems] = useState<TodoItem[]>([]);
  const [title, setTitle] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  async function refresh() {
    try {
      setItems(await api.list());
      setError(null);
    } catch {
      setError("Could not reach the API — is it running? See the root README for setup.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    refresh();
  }, []);

  async function handleAdd(e: React.FormEvent) {
    e.preventDefault();
    if (!title.trim()) return;
    await api.create(title.trim());
    setTitle("");
    await refresh();
  }

  async function handleToggle(item: TodoItem) {
    await api.update(item.id, { isCompleted: !item.isCompleted });
    await refresh();
  }

  async function handleDelete(id: string) {
    await api.remove(id);
    await refresh();
  }

  return (
    <div className="mx-auto flex min-h-screen max-w-lg flex-col gap-6 px-6 py-16">
      <h1 className="text-2xl font-semibold">To Do</h1>

      {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <form onSubmit={handleAdd} className="flex gap-2">
        <input
          className="flex-1 rounded-md border border-black/10 px-3 py-2 text-sm"
          placeholder="Add a task…"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
        />
        <button type="submit" className="rounded-md bg-black px-4 py-2 text-sm font-medium text-white">
          Add
        </button>
      </form>

      {loading ? (
        <p className="text-sm text-black/50">Loading…</p>
      ) : items.length === 0 ? (
        <p className="text-sm text-black/50">No tasks yet.</p>
      ) : (
        <ul className="flex flex-col gap-2">
          {items.map((item) => (
            <li key={item.id} className="flex items-center gap-3 rounded-md border border-black/10 px-3 py-2">
              <input type="checkbox" checked={item.isCompleted} onChange={() => handleToggle(item)} />
              <span className={`flex-1 text-sm ${item.isCompleted ? "text-black/40 line-through" : ""}`}>{item.title}</span>
              <button onClick={() => handleDelete(item.id)} className="text-xs text-black/40 hover:text-red-600">
                Delete
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
