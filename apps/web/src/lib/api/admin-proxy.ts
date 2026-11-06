import type { NextRequest } from "next/server";
import { apiConfig } from "@/lib/api/config";
import { trustedClientIp } from "@/lib/api/trusted-client-ip";

const safeMethods = new Set(["GET", "HEAD", "OPTIONS"]);
const forwardedRequestHeaders = [
  "accept",
  "content-type",
  "cookie",
  "x-correlation-id",
  "x-csrf-token",
] as const;
const forwardedResponseHeaders = [
  "cache-control",
  "content-type",
  "pragma",
  "retry-after",
  "x-correlation-id",
] as const;

type ProxyContext = Readonly<{
  params: Promise<{ path: string[] }>;
}>;

export async function proxyAdminRequest(
  request: NextRequest,
  context: ProxyContext,
): Promise<Response> {
  if (!safeMethods.has(request.method) && !hasSameOrigin(request)) {
    return problemResponse(403, "Yêu cầu khác nguồn bị từ chối.");
  }

  const { path } = await context.params;
  if (path.length === 0 || path.some((segment) => !segment || segment === "." || segment === "..")) {
    return problemResponse(400, "Đường dẫn API không hợp lệ.");
  }

  const upstreamUrl = new URL(
    `${apiConfig.baseUrl}/admin/${path.map(encodeURIComponent).join("/")}`,
  );
  upstreamUrl.search = request.nextUrl.search;

  const headers = new Headers();
  for (const headerName of forwardedRequestHeaders) {
    const value = request.headers.get(headerName);
    if (value !== null) {
      headers.set(headerName, value);
    }
  }

  const hasBody = request.method !== "GET" && request.method !== "HEAD";
  const clientIp = trustedClientIp(request.headers, process.env.BFF_TRUST_PROXY_HEADERS === "true");
  if (clientIp) {
    headers.set("x-forwarded-for", clientIp);
    headers.set("x-forwarded-proto", new URL(process.env.NEXT_PUBLIC_SITE_URL || request.nextUrl.origin).protocol.slice(0, -1));
  }

  try {
    const upstreamResponse = await fetch(upstreamUrl, {
      method: request.method,
      headers,
      body: hasBody ? await request.arrayBuffer() : undefined,
      cache: "no-store",
      redirect: "manual",
      signal: AbortSignal.timeout(15_000),
    });

    return createProxyResponse(request.method, upstreamResponse);
  } catch {
    return problemResponse(502, "Không thể kết nối dịch vụ quản trị.");
  }
}

function hasSameOrigin(request: NextRequest): boolean {
  const origin = request.headers.get("origin");
  // Standalone behind a host-port mapping can report its internal port in
  // nextUrl.origin. Compare with the configured public origin instead.
  const publicUrl = process.env.NEXT_PUBLIC_SITE_URL?.trim();
  const expectedOrigin = publicUrl ? new URL(publicUrl).origin : request.nextUrl.origin;
  return origin !== null && origin === expectedOrigin;
}

async function createProxyResponse(method: string, upstream: Response): Promise<Response> {
  const headers = new Headers();

  for (const headerName of forwardedResponseHeaders) {
    const value = upstream.headers.get(headerName);
    if (value !== null) {
      headers.set(headerName, value);
    }
  }

  const headersWithCookies = upstream.headers as Headers & {
    getSetCookie?: () => string[];
  };
  for (const cookie of headersWithCookies.getSetCookie?.() ?? []) {
    headers.append("set-cookie", cookie);
  }
  headers.set("cache-control", "no-store");
  headers.set("pragma", "no-cache");

  const hasNoBody =
    method === "HEAD" ||
    upstream.status === 204 ||
    upstream.status === 205 ||
    upstream.status === 304;

  return new Response(hasNoBody ? null : await upstream.arrayBuffer(), {
    status: upstream.status,
    headers,
  });
}

function problemResponse(status: number, detail: string): Response {
  return Response.json(
    {
      type: "about:blank",
      title: status === 403 ? "Forbidden" : status === 400 ? "Bad Request" : "Bad Gateway",
      status,
      detail,
    },
    {
      status,
      headers: { "cache-control": "no-store", "content-type": "application/problem+json" },
    },
  );
}
