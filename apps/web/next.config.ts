import type { NextConfig } from "next";

const mediaBaseUrl = process.env.R2_PUBLIC_BASE_URL;
const mediaUrl = mediaBaseUrl ? new URL(mediaBaseUrl) : null;

const nextConfig: NextConfig = {
  output: "standalone",
  poweredByHeader: false,
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
