import type { AntiforgeryToken } from "@/lib/auth/types";

export async function getAntiforgeryToken(): Promise<AntiforgeryToken> {
  const response = await fetch("/api/admin/auth/csrf", {
    cache: "no-store",
    credentials: "same-origin",
  });

  if (!response.ok) {
    throw new Error("Không thể khởi tạo yêu cầu bảo mật.");
  }

  const payload: unknown = await response.json();
  if (!isAntiforgeryToken(payload)) {
    throw new Error("Phản hồi bảo mật không hợp lệ.");
  }

  return payload;
}

function isAntiforgeryToken(value: unknown): value is AntiforgeryToken {
  if (typeof value !== "object" || value === null) {
    return false;
  }

  const candidate = value as Record<string, unknown>;
  return (
    typeof candidate.headerName === "string" &&
    candidate.headerName.toLowerCase() === "x-csrf-token" &&
    typeof candidate.requestToken === "string" &&
    candidate.requestToken.length > 0
  );
}
