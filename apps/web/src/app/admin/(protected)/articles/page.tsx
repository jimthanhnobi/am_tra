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
  return <main className="space-y-6"><header className="flex flex-wrap items-end justify-between gap-4"><div><p className="text-xs uppercase tracking-[0.2em] text-[#9c6349]">CMS</p><h1 className="mt-2 font-serif text-3xl">Bài viết</h1><p className="mt-1 text-sm text-[#718077]">{data.totalItems} bài viết</p></div>{hasPermission(user!.permissions, permissions.contentWrite) && <Link href="/admin/articles/new" className="rounded bg-[#183c31] px-5 py-3 text-sm text-white">Tạo bài viết</Link>}</header><ArticleList data={data} canPublish={hasPermission(user!.permissions, permissions.contentPublish)} /></main>;
}
