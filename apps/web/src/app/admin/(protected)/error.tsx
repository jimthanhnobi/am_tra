"use client";

import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";

export default function AdminError({ reset }: Readonly<{ error: Error & { digest?: string }; reset: () => void }>) {
  return <Card className="mx-auto max-w-xl p-8 text-center" role="alert"><h1 className="font-serif text-2xl">Không thể tải dữ liệu quản trị</h1><p className="mt-3 text-sm text-[#718077]">Kiểm tra kết nối API hoặc đăng nhập lại nếu phiên đã hết hạn.</p><Button className="mt-5" onClick={reset}>Thử lại</Button></Card>;
}
