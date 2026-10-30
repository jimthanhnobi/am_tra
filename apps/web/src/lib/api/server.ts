import "server-only";
import { cookies } from "next/headers";
import { apiConfig } from "@/lib/api/config";
import { AdminApiError, readProblem } from "@/lib/api/errors";

export async function getAdminApi<T>(path: string): Promise<T> {
  const cookieHeader = (await cookies()).toString();
  const response = await fetch(`${apiConfig.baseUrl}/admin/${normalizePath(path)}`, {
    cache: "no-store",
    headers: cookieHeader ? { cookie: cookieHeader } : undefined,
  });

  if (!response.ok) {
    throw new AdminApiError(response.status, await readProblem(response));
  }

  return (await response.json()) as T;
}

function normalizePath(path: string): string {
  return path.replace(/^\/+/, "");
}
