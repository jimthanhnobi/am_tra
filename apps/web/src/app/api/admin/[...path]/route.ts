import type { NextRequest } from "next/server";
import { proxyAdminRequest } from "@/lib/api/admin-proxy";

type Context = Readonly<{
  params: Promise<{ path: string[] }>;
}>;

const handler = (request: NextRequest, context: Context) =>
  proxyAdminRequest(request, context);

export const GET = handler;
export const POST = handler;
export const PUT = handler;
export const PATCH = handler;
export const DELETE = handler;
export const HEAD = handler;
export const OPTIONS = handler;
