import type { HTMLAttributes } from "react";
import { cn } from "@/lib/utils";

export function Card({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn(
        "rounded-2xl border border-[#ded7ca] bg-white shadow-[0_12px_30px_rgba(32,50,41,0.05)]",
        className,
      )}
      {...props}
    />
  );
}
