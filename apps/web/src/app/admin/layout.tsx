import type { Metadata } from "next";
import type { ReactNode } from "react";
import "./admin.css";

export const metadata: Metadata = {
  title: "Quản trị",
  description: "Khu vực quản trị TeaPremium.",
  robots: {
    index: false,
    follow: false,
    noarchive: true,
  },
};

export default function AdminLayout({ children }: Readonly<{ children: ReactNode }>) {
  return <div className="admin-root">{children}</div>;
}

