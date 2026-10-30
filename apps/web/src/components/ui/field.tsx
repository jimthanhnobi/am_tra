import type {
  InputHTMLAttributes,
  LabelHTMLAttributes,
  SelectHTMLAttributes,
  TextareaHTMLAttributes,
} from "react";
import { cn } from "@/lib/utils";

export function Label({ className, ...props }: LabelHTMLAttributes<HTMLLabelElement>) {
  return (
    <label
      className={cn("mb-2 block text-sm font-semibold text-[#344a40]", className)}
      {...props}
    />
  );
}

export function Input({ className, ...props }: InputHTMLAttributes<HTMLInputElement>) {
  return (
    <input
      className={cn(
        "h-11 w-full rounded-xl border border-[#d8d1c3] bg-white px-3 text-sm text-[#1d2923] shadow-sm outline-none transition placeholder:text-[#8a958f] focus:border-[#8d4f34] focus:ring-2 focus:ring-[#8d4f34]/15 disabled:bg-[#eeeae2]",
        className,
      )}
      {...props}
    />
  );
}

export function Textarea({ className, ...props }: TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return (
    <textarea
      className={cn(
        "min-h-28 w-full resize-y rounded-xl border border-[#d8d1c3] bg-white px-3 py-3 text-sm leading-6 text-[#1d2923] shadow-sm outline-none transition placeholder:text-[#8a958f] focus:border-[#8d4f34] focus:ring-2 focus:ring-[#8d4f34]/15 disabled:bg-[#eeeae2]",
        className,
      )}
      {...props}
    />
  );
}

export function Select({ className, ...props }: SelectHTMLAttributes<HTMLSelectElement>) {
  return (
    <select
      className={cn(
        "h-11 w-full rounded-xl border border-[#d8d1c3] bg-white px-3 text-sm text-[#1d2923] shadow-sm outline-none transition focus:border-[#8d4f34] focus:ring-2 focus:ring-[#8d4f34]/15 disabled:bg-[#eeeae2]",
        className,
      )}
      {...props}
    />
  );
}

export function FieldError({ message }: Readonly<{ message?: string }>) {
  if (!message) return null;
  return <p className="mt-1.5 text-xs font-medium text-[#a33d32]">{message}</p>;
}
