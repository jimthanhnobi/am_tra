import Link from "next/link";
import { Plus } from "lucide-react";
import { getAdminApi } from "@/lib/api/server";
import { getCurrentAdminUser } from "@/lib/auth/server";
import { permissions, hasPermission } from "@/lib/permissions";
import type { AdminProductListItem, AdminCategoryNode, PagedResult } from "@/lib/api/catalog-types";
import { AccessDenied } from "@/components/admin/access-denied";
import { Button } from "@/components/ui/button";
import { ProductList } from "./product-list";



type Props = Readonly<{ searchParams: Promise<Record<string, string | string[] | undefined>> }>;

export default async function ProductsPage({ searchParams }: Props) {
  const user = await getCurrentAdminUser();
  if (!hasPermission(user?.permissions ?? [], permissions.catalogRead)) return <AccessDenied permission={permissions.catalogRead} />;
  const input = await searchParams;
  const query = new URLSearchParams();
  const page = Number(input.page ?? 1);
  query.set("page", String(Number.isInteger(page) && page > 0 ? page : 1));
  query.set("pageSize", "20");
  for (const key of ["search", "status", "categoryId", "sort"] as const) {
    const value = input[key];
    if (typeof value === "string" && value) query.set(key, value);
  }
  const [products, categories] = await Promise.all([
    getAdminApi<PagedResult<AdminProductListItem>>(`products?${query}`),
    getAdminApi<AdminCategoryNode[]>("categories"),
  ]);
  const canWrite = hasPermission(user!.permissions, permissions.catalogWrite);
  const canPublish = hasPermission(user!.permissions, permissions.catalogPublish);
  return <main className="space-y-6"><div className="flex flex-wrap items-end justify-between gap-4"><div><p className="text-xs font-semibold uppercase tracking-[0.2em] text-[#9c6349]">Catalog</p><h1 className="mt-2 font-serif text-3xl">Sản phẩm</h1><p className="mt-1 text-sm text-[#718077]">{products.totalItems} sản phẩm trong hệ thống</p></div>{canWrite && <Link href="/admin/products/new"><Button><Plus size={17} /> Thêm sản phẩm</Button></Link>}</div><ProductList data={products} categories={categories} query={query.toString()} canWrite={canWrite} canPublish={canPublish} /></main>;
}
