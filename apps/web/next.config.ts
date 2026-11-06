import type { NextConfig } from "next";

const mediaBaseUrl = process.env.R2_PUBLIC_BASE_URL;
const mediaUrl = mediaBaseUrl ? new URL(mediaBaseUrl) : null;

const nextConfig: NextConfig = {
  output: "standalone",
  poweredByHeader: false,
  headers: async () => [{ source: "/:path*", headers: [
    { key: "X-Content-Type-Options", value: "nosniff" },
    { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
    { key: "X-Frame-Options", value: "DENY" },
    { key: "Permissions-Policy", value: "camera=(), microphone=(), geolocation=()" },
    { key: "Content-Security-Policy", value: "frame-ancestors 'none'; object-src 'none'; base-uri 'self'" },
  ] }],
  reactStrictMode: true,
  // Blocking SSR preserves HTTP 404 and avoids stale Published content after Archive.
  cacheComponents: false,
  partialPrefetching: false,
  htmlLimitedBots: /.*/,
  images: {
    remotePatterns: mediaUrl ? [{ protocol: mediaUrl.protocol.slice(0, -1) as "https" | "http", hostname: mediaUrl.hostname, port: mediaUrl.port, pathname: `${mediaUrl.pathname.replace(/\/$/, "")}/**` }] : [],
  },
  turbopack: {
    rules: {
      "*.css": {
        loaders: ["@tailwindcss/turbopack"],
        as: "*.css",
      },
    },
  },
};

export default nextConfig;
