"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useRef, useState, type ReactNode } from "react";
import { LayoutDashboard, Package, FolderTree, Tags, Images, Menu, X, BookOpen, Inbox, Settings2, ChevronDown, UserRound } from "lucide-react";
import { Toaster } from "sonner";
import type { CurrentAdminUser } from "@/lib/auth/types";
import { permissions, hasPermission } from "@/lib/permissions";
import { LogoutButton } from "@/app/admin/(protected)/logout-button";

const links = [
  { href: "/admin", label: "Tổng quan", icon: LayoutDashboard, group: "Không gian làm việc" },
  { href: "/admin/products", label: "Sản phẩm", icon: Package, group: "Catalog" },
  { href: "/admin/categories", label: "Danh mục", icon: FolderTree, group: "Catalog" },
  { href: "/admin/brands", label: "Thương hiệu", icon: Tags, group: "Catalog" },
  { href: "/admin/media", label: "Thư viện ảnh", icon: Images, group: "Catalog" },
  { href: "/admin/articles", label: "Bài viết", icon: BookOpen, group: "Nội dung" },
  { href: "/admin/article-categories", label: "Danh mục bài viết", icon: FolderTree, group: "Nội dung" },
  { href: "/admin/leads", label: "Yêu cầu tư vấn", icon: Inbox, group: "Khách liên hệ" },
  { href: "/admin/settings", label: "Cài đặt website", icon: Settings2, group: "Hệ thống" },
] as const;

export function AdminShell({ user, children }: Readonly<{ user: CurrentAdminUser; children: ReactNode }>) {
  const pathname = usePathname();
  const [open, setOpen] = useState(false);
  const menuTriggerRef = useRef<HTMLButtonElement>(null);
  const menuCloseRef = useRef<HTMLButtonElement>(null);
  const current = [...links].reverse().find((link) => pathname === link.href || (link.href !== "/admin" && pathname.startsWith(`${link.href}/`)));
  const canRead = hasPermission(user.permissions, permissions.catalogRead);
  const canReadContent = hasPermission(user.permissions, permissions.contentRead);
  const canReadLeads = hasPermission(user.permissions, permissions.leadsRead);
  const canManageSettings = hasPermission(user.permissions, permissions.settingsManage);
  const visible = links.filter((link) => link.href === "/admin" || (link.href === "/admin/settings" ? canManageSettings : link.href === "/admin/leads" ? canReadLeads : link.href.startsWith("/admin/article") ? canReadContent : canRead));
  const detail = pathname.endsWith("/new") ? "Tạo mới" : pathname.includes("/edit") ? "Chỉnh sửa" : null;

  useEffect(() => {
    function escape(event: KeyboardEvent) {
      if (event.key === "Escape" && open) { setOpen(false); menuTriggerRef.current?.focus(); }
    }
    document.addEventListener("keydown", escape);
    return () => document.removeEventListener("keydown", escape);
  }, [open]);
  useEffect(() => { if (open) menuCloseRef.current?.focus(); }, [open]);

  return <div className="min-h-screen bg-[#f7f4ec] text-[#202722] lg:flex">
    <Toaster position="top-right" richColors />
    <a href="#admin-content" className="sr-only fixed left-2 top-2 z-[60] bg-white p-3 focus:not-sr-only">Đến nội dung quản trị</a>
    {open && <button type="button" aria-label="Đóng menu" className="fixed inset-0 z-30 bg-[#202722]/55 lg:hidden" onClick={() => setOpen(false)} />}
    <aside id="admin-navigation" className={`${open ? "visible translate-x-0" : "invisible -translate-x-full lg:visible"} fixed inset-y-0 left-0 z-40 flex w-[17rem] flex-col border-r border-[#244338] bg-[#233b32] text-[#f7f4ec] transition-transform lg:sticky lg:top-0 lg:h-screen lg:translate-x-0`}>
      <div className="flex min-h-24 items-center justify-between border-b border-white/15 px-6">
        <Link href="/admin" onClick={() => setOpen(false)} className="block min-w-0 no-underline"><span className="block font-serif text-[1.65rem] leading-none tracking-tight">TeaPremium</span><span className="mt-1.5 block text-[.62rem] font-semibold uppercase tracking-[.2em] text-[#d1b88c]">Quản trị nội dung</span></Link>
        <button ref={menuCloseRef} type="button" aria-label="Đóng menu" className="grid size-10 place-items-center rounded-lg hover:bg-white/10 lg:hidden" onClick={() => { setOpen(false); menuTriggerRef.current?.focus(); }}><X size={20} /></button>
      </div>
      <nav aria-label="Quản trị" className="flex-1 overflow-y-auto px-3 py-5">
        {["Không gian làm việc", "Catalog", "Nội dung", "Khách liên hệ", "Hệ thống"].map((group) => {
          const items = visible.filter((link) => link.group === group);
          if (!items.length) return null;
          return <div key={group} className="mb-5"><p className="px-4 pb-2 text-[.62rem] font-semibold uppercase tracking-[.2em] text-[#aebfb3]">{group}</p><div className="space-y-1">{items.map(({ href, label, icon: Icon }) => {
            const active = pathname === href || (href !== "/admin" && pathname.startsWith(`${href}/`));
            return <Link key={href} href={href} onClick={() => setOpen(false)} aria-current={active ? "page" : undefined} className={`flex min-h-11 items-center gap-3 rounded-lg border-l-[3px] px-3 py-2 text-sm transition-colors ${active ? "border-[#d1b88c] bg-white/13 font-semibold text-white" : "border-transparent text-[#d9e4d8] hover:bg-white/10 hover:text-white"}`}><Icon size={18} aria-hidden="true" />{label}</Link>;
          })}</div></div>;
        })}
      </nav>
      <div className="border-t border-white/15 px-6 py-5 text-xs text-[#d9e4d8]"><p className="truncate font-medium text-white">{user.email}</p><p className="mt-1">{user.roles.join(", ") || "Chưa có vai trò"}</p></div>
    </aside>
    <div className="min-w-0 flex-1">
      <header className="sticky top-0 z-20 flex min-h-20 items-center justify-between gap-4 border-b border-[#ddd8cc] bg-[#fffefa]/96 px-4 sm:px-7 xl:px-10">
        <div className="flex min-w-0 items-center gap-3">
          <button ref={menuTriggerRef} type="button" aria-label={open ? "Đóng menu" : "Mở menu"} aria-expanded={open} aria-controls="admin-navigation" className="grid size-11 shrink-0 place-items-center rounded-lg border border-[#ddd8cc] text-[#233b32] lg:hidden" onClick={() => setOpen(!open)}>{open ? <X size={20} /> : <Menu size={20} />}</button>
          <div className="min-w-0"><p className="truncate text-xs text-[#56635b]">Quản trị / {current?.label ?? "Tổng quan"}{detail ? ` / ${detail}` : ""}</p><p className="truncate text-sm font-semibold text-[#233b32]">{detail ? `${detail} ${current?.label.toLowerCase() ?? ""}` : current?.label ?? "Tổng quan"}</p></div>
        </div>
        <details className="group relative shrink-0"><summary className="flex min-h-11 cursor-pointer list-none items-center gap-2 rounded-lg border border-[#ddd8cc] bg-white px-3 text-sm text-[#233b32] [&::-webkit-details-marker]:hidden"><span className="grid size-7 place-items-center rounded-full bg-[#e7eee8]"><UserRound size={15} aria-hidden="true" /></span><span className="sr-only">Tài khoản quản trị: </span><span className="hidden max-w-36 truncate font-medium sm:inline">{user.email}</span><ChevronDown size={15} aria-hidden="true" /></summary><div className="absolute right-0 top-[calc(100%+.5rem)] z-30 w-64 rounded-xl border border-[#ddd8cc] bg-white p-4 shadow-xl"><p className="break-all text-sm font-semibold text-[#233b32]">{user.email}</p><p className="mt-1 text-xs text-[#56635b]">{user.roles.join(", ") || "Chưa có vai trò"}</p><div className="mt-4 border-t border-[#eeeae1] pt-3"><LogoutButton /></div></div></details>
      </header>
      <div id="admin-content" className="admin-page px-4 py-7 sm:px-7 sm:py-9 xl:px-10 xl:py-10">{children}</div>
    </div>
  </div>;
}
