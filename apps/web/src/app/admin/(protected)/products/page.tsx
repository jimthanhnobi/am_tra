import Link from "next/link";
import { Plus } from "lucide-react";
import { getAdminApi } from "@/lib/api/server";
import { getCurrentAdminUser } from "@/lib/auth/server";
import { permissions, hasPermission } from "@/lib/permissions";
import type { AdminProductListItem, AdminCategoryNode, PagedResult } from "@/lib/api/catalog-types";
import { AccessDenied } from "@/components/admin/access-denied";
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
  return <main className="space-y-6"><header className="admin-page-header"><div><p className="admin-eyebrow">Catalog</p><h1 className="admin-title">Sản phẩm</h1><p className="admin-description">{products.totalItems} sản phẩm trong hệ thống</p></div>{canWrite && <Link href="/admin/products/new" className="admin-primary-link inline-flex min-h-11 items-center gap-2 rounded-lg bg-[#233b32] px-4 text-sm font-semibold hover:bg-[#355749]"><Plus size={17} aria-hidden="true" /> Thêm sản phẩm</Link>}</header><ProductList data={products} categories={categories} query={query.toString()} canWrite={canWrite} canPublish={canPublish} /></main>;
}
