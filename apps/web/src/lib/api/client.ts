"use client";

import { getAntiforgeryToken } from "@/lib/auth/client";
import { AdminApiError, readProblem } from "@/lib/api/errors";

const safeMethods = new Set(["GET", "HEAD", "OPTIONS"]);

export async function adminRequest<T>(
  path: string,
  init: RequestInit = {},
): Promise<T> {
  const method = (init.method ?? "GET").toUpperCase();
  const headers = new Headers(init.headers);
  if (init.body !== undefined && !headers.has("content-type")) {
    headers.set("content-type", "application/json");
  }

  if (!safeMethods.has(method)) {
    const csrf = await getAntiforgeryToken();
    headers.set(csrf.headerName, csrf.requestToken);
  }

  const response = await fetch(`/api/admin/${path.replace(/^\/+/, "")}`, {
    ...init,
    method,
    headers,
    cache: "no-store",
    credentials: "same-origin",
  });

  if (!response.ok) {
    throw new AdminApiError(response.status, await readProblem(response));
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export function jsonBody(value: unknown): BodyInit {
  return JSON.stringify(value);
}
