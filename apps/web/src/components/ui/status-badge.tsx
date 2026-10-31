import type { PublicationStatus } from "@/lib/api/catalog-types";
import { cn } from "@/lib/utils";

const styles: Record<PublicationStatus, string> = {
  Draft: "bg-[#eeeae2] text-[#5f6c65]",
  InReview: "bg-[#fff0c7] text-[#765410]",
  Published: "bg-[#dcefe3] text-[#245b3d]",
  Archived: "bg-[#eedddd] text-[#7d3832]",
};

const labels: Record<PublicationStatus, string> = {
  Draft: "Bản nháp",
  InReview: "Chờ duyệt",
  Published: "Đã xuất bản",
  Archived: "Đã lưu trữ",
};

export function StatusBadge({ status }: Readonly<{ status: PublicationStatus }>) {
  return (
    <span className={cn("inline-flex whitespace-nowrap rounded-full px-2.5 py-1 text-xs font-semibold", styles[status])}>
      {labels[status]}
    </span>
  );
}
