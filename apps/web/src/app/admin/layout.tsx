import type { Metadata } from "next";
import type { ReactNode } from "react";

export const metadata: Metadata = {
  title: "Quản trị — đang chuẩn bị",
  description: "Khu vực quản trị TeaPremium chưa được kích hoạt trong Foundation.",
  robots: {
    index: false,
    follow: false,
    noarchive: true,
  },
};

export default function AdminLayout({ children }: Readonly<{ children: ReactNode }>) {
  return children;
}

