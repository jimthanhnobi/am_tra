import { cookies } from "next/headers";
import { apiConfig } from "@/lib/api/config";
import type { CurrentAdminUser } from "@/lib/auth/types";

export async function getCurrentAdminUser(): Promise<CurrentAdminUser | null> {
  const cookieHeader = (await cookies()).toString();
  const response = await fetch(`${apiConfig.baseUrl}/admin/auth/me`, {
    cache: "no-store",
    headers: cookieHeader ? { cookie: cookieHeader } : undefined,
  });

  if (response.status === 401) {
    return null;
  }

  if (!response.ok) {
    throw new Error(`Không thể kiểm tra phiên quản trị (HTTP ${response.status}).`);
  }

  const payload: unknown = await response.json();
  if (!isCurrentAdminUser(payload)) {
    throw new Error("API trả về dữ liệu phiên quản trị không hợp lệ.");
  }

  return payload;
}

function isCurrentAdminUser(value: unknown): value is CurrentAdminUser {
  if (typeof value !== "object" || value === null) {
    return false;
  }

  const candidate = value as Record<string, unknown>;
  return (
    typeof candidate.id === "string" &&
    typeof candidate.email === "string" &&
    Array.isArray(candidate.roles) &&
    candidate.roles.every((role) => typeof role === "string") &&
    Array.isArray(candidate.permissions) &&
    candidate.permissions.every((permission) => typeof permission === "string")
  );
}
