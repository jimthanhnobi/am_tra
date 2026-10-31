"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useState, type ReactNode } from "react";
import { LayoutDashboard, Package, FolderTree, Tags, Images, Menu, X, BookOpen, Inbox } from "lucide-react";
import { Toaster } from "sonner";
import type { CurrentAdminUser } from "@/lib/auth/types";
import { permissions, hasPermission } from "@/lib/permissions";
import { LogoutButton } from "@/app/admin/(protected)/logout-button";

const links = [
  { href: "/admin", label: "Tổng quan", icon: LayoutDashboard },
  { href: "/admin/products", label: "Sản phẩm", icon: Package },
  { href: "/admin/categories", label: "Danh mục", icon: FolderTree },
  { href: "/admin/brands", label: "Thương hiệu", icon: Tags },
  { href: "/admin/media", label: "Thư viện ảnh", icon: Images },
  { href: "/admin/articles", label: "Bài viết", icon: BookOpen },
  { href: "/admin/article-categories", label: "Danh mục bài viết", icon: FolderTree },
  { href: "/admin/leads", label: "Leads", icon: Inbox },
];

export function AdminShell({ user, children }: Readonly<{ user: CurrentAdminUser; children: ReactNode }>) {
  const pathname = usePathname();
  const [open, setOpen] = useState(false);
  const current = [...links].reverse().find((link) => pathname === link.href || (link.href !== "/admin" && pathname.startsWith(`${link.href}/`)));
  const canRead = hasPermission(user.permissions, permissions.catalogRead);
  const canReadContent = hasPermission(user.permissions, permissions.contentRead);
  const canReadLeads = hasPermission(user.permissions, permissions.leadsRead);
  return <div className="min-h-screen bg-[#f5f2eb] text-[#20362c] lg:flex">
    <Toaster position="top-right" richColors />
    {open && <button type="button" aria-label="Đóng menu" className="fixed inset-0 z-30 bg-black/45 lg:hidden" onClick={() => setOpen(false)} />}
    <aside className={`${open ? "translate-x-0" : "-translate-x-full"} fixed inset-y-0 left-0 z-40 flex w-64 flex-col bg-[#183c31] text-[#f8f4eb] transition-transform lg:sticky lg:top-0 lg:h-screen lg:translate-x-0`}>
      <div className="border-b border-white/10 px-6 py-7"><p className="font-serif text-2xl">TeaPremium</p><p className="mt-1 text-[11px] uppercase tracking-[0.28em] text-[#d8ab91]">Catalog Studio</p></div>
      <nav aria-label="Quản trị" className="flex-1 space-y-1 px-3 py-5">{links.filter((link) => link.href === "/admin" || (link.href === "/admin/leads" ? canReadLeads : link.href.startsWith("/admin/article") ? canReadContent : canRead)).map(({ href, label, icon: Icon }) => {
        const active = pathname === href || (href !== "/admin" && pathname.startsWith(`${href}/`));
        return <Link key={href} href={href} onClick={() => setOpen(false)} aria-current={active ? "page" : undefined} className={`flex items-center gap-3 rounded-xl px-4 py-3 text-sm transition ${active ? "bg-white/15 text-white" : "text-[#c5d3c9] hover:bg-white/10 hover:text-white"}`}><Icon size={18} />{label}</Link>;
      })}</nav>
      <div className="border-t border-white/10 px-5 py-5 text-xs text-[#b8c9be]"><p className="truncate text-[#f8f4eb] lg:hidden">{user.email}</p><p className="mt-1 lg:hidden">{user.roles.join(", ") || "Chưa có vai trò"}</p><p className="mt-3 lg:mt-0">Quản trị nội dung · vi-VN</p></div>
    </aside>
    <div className="min-w-0 flex-1"><header className="sticky top-0 z-20 flex min-h-18 items-center justify-between gap-4 border-b border-[#dfd9cc] bg-[#fbf9f4]/95 px-4 backdrop-blur sm:px-8"><div className="flex items-center gap-3"><button className="rounded-lg p-2 lg:hidden" type="button" aria-label={open ? "Đóng menu" : "Mở menu"} onClick={() => setOpen(!open)}>{open ? <X size={20} /> : <Menu size={20} />}</button><div><p className="text-xs text-[#718077]">Quản trị / {current?.label ?? "Tổng quan"}</p><p className="font-semibold">{current?.label ?? "Tổng quan"}</p></div></div><div className="flex items-center gap-3"><div className="hidden text-right sm:block"><p className="max-w-40 truncate text-sm font-medium">{user.email}</p><p className="text-xs text-[#718077]">{user.roles.join(", ") || "Chưa có vai trò"}</p></div><LogoutButton /></div></header><div className="mx-auto max-w-7xl px-4 py-7 sm:px-8 sm:py-9">{children}</div></div>
  </div>;
}
