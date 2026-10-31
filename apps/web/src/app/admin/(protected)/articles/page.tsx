import Link from "next/link";
import { getAdminApi } from "@/lib/api/server";
import { getCurrentAdminUser } from "@/lib/auth/server";
import { hasPermission, permissions } from "@/lib/permissions";
import { AccessDenied } from "@/components/admin/access-denied";
import type { ArticlePage } from "@/lib/api/article-types";
import { ArticleList } from "./article-list";

export default async function ArticlesAdminPage({ searchParams }: Readonly<{ searchParams: Promise<{ page?: string }> }>) {
  const user = await getCurrentAdminUser();
  if (!hasPermission(user?.permissions ?? [], permissions.contentRead)) return <AccessDenied permission={permissions.contentRead} />;
  const requested = Number((await searchParams).page ?? 1);
  const page = Number.isInteger(requested) && requested > 0 ? requested : 1;
  const data = await getAdminApi<ArticlePage>(`articles?page=${page}&pageSize=20`);
  return <main className="space-y-6"><header className="admin-page-header"><div><p className="admin-eyebrow">CMS</p><h1 className="admin-title">Bài viết</h1><p className="admin-description">{data.totalItems} bài viết trong hệ thống</p></div>{hasPermission(user!.permissions, permissions.contentWrite) && <Link href="/admin/articles/new" className="admin-primary-link inline-flex min-h-11 items-center rounded-lg bg-[#233b32] px-5 text-sm font-semibold hover:bg-[#355749]">Tạo bài viết</Link>}</header><ArticleList data={data} canPublish={hasPermission(user!.permissions, permissions.contentPublish)} /></main>;
}
