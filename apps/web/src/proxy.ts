import { NextResponse, type NextRequest } from "next/server";

const contentPath = /^\/(san-pham|danh-muc|kien-thuc)\/[a-z0-9]+(?:-[a-z0-9]+)*$/;

export async function proxy(request: NextRequest) {
  const sourcePath = request.nextUrl.pathname.replace(/\/$/, "");
  if (!contentPath.test(sourcePath)) return NextResponse.next();

  const baseUrl = process.env.API_BASE_URL?.trim() || "http://localhost:5080/api/v1";
  try {
    const endpoint = new URL(`${baseUrl.replace(/\/$/, "")}/public/seo-redirects`);
    endpoint.searchParams.set("sourcePath", sourcePath);
    const response = await fetch(endpoint, { cache: "no-store", signal: AbortSignal.timeout(3000) });
    if (response.status !== 200) return NextResponse.next();
    const value: unknown = await response.json();
    const destination = typeof value === "object" && value !== null && "destinationPath" in value
      ? (value as { destinationPath: unknown }).destinationPath : null;
    if (typeof destination !== "string" || !contentPath.test(destination) || destination === sourcePath ||
        destination.split("/")[1] !== sourcePath.split("/")[1]) return NextResponse.next();
    const target = new URL(destination, request.url);
    return NextResponse.redirect(target, 301);
  } catch {
    return NextResponse.next();
  }
}

export const config = { matcher: ["/san-pham/:slug", "/danh-muc/:slug", "/kien-thuc/:slug"] };
