import { ShieldX } from "lucide-react";
import { Card } from "@/components/ui/card";

export function AccessDenied({ permission }: Readonly<{ permission: string }>) {
  return (
    <Card className="mx-auto max-w-xl p-8 text-center">
      <ShieldX className="mx-auto size-10 text-[#a33d32]" />
      <h1 className="mt-4 font-serif text-2xl font-semibold">Không đủ quyền truy cập</h1>
      <p className="mt-3 text-sm leading-6 text-[#68766f]">
        Tài khoản hiện tại cần quyền <code>{permission}</code>. API vẫn kiểm tra quyền ở máy chủ cho mọi yêu cầu.
      </p>
    </Card>
  );
}
