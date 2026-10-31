import { getAdminApi } from "@/lib/api/server";
import { getCurrentAdminUser } from "@/lib/auth/server";
import { hasPermission, permissions } from "@/lib/permissions";
import type { AdminMediaAsset, PagedResult } from "@/lib/api/catalog-types";
import { AccessDenied } from "@/components/admin/access-denied";
import { MediaLibrary } from "./media-library";



export default async function MediaPage({ searchParams }: Readonly<{ searchParams: Promise<Record<string, string | string[] | undefined>> }>) {
  const user = await getCurrentAdminUser();
  if (!hasPermission(user?.permissions ?? [], permissions.catalogRead)) return <AccessDenied permission={permissions.catalogRead} />;
  const input = await searchParams;
  const page = Number(input.page ?? 1);
  const query = new URLSearchParams({ page: String(Number.isInteger(page) && page > 0 ? page : 1), pageSize: "24" });
  if (typeof input.search === "string" && input.search) query.set("search", input.search);
  if (typeof input.contentType === "string" && ["image/jpeg", "image/png", "image/webp"].includes(input.contentType)) query.set("contentType", input.contentType);
  const media = await getAdminApi<PagedResult<AdminMediaAsset>>(`media?${query}`);
  return <MediaLibrary initial={media} query={query.toString()} canUpload={hasPermission(user!.permissions, permissions.mediaWrite)} />;
}
