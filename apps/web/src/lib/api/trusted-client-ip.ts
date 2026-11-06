import { isIP } from "node:net";

// Enable only when Next is private and the ingress overwrites X-Real-IP.
// Never use the client-supplied X-Forwarded-For chain.
export function trustedClientIp(headers: Headers, trustedIngress: boolean): string | null {
  if (!trustedIngress) return null;
  const value = headers.get("x-real-ip")?.trim();
  return value && isIP(value) ? value : null;
}
