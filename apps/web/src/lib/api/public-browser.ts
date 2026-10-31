"use client";

const baseUrl = (process.env.NEXT_PUBLIC_API_BASE_URL?.trim() || "http://localhost:5080/api/v1").replace(/\/$/, "");

export function publicMutationUrl(path: "leads" | "contact-events") {
  return `${baseUrl}/public/${path}`;
}
