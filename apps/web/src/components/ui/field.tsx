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
      className={cn("mb-2 block text-sm font-semibold text-[#233b32]", className)}
      {...props}
    />
  );
}

export function Input({ className, ...props }: InputHTMLAttributes<HTMLInputElement>) {
  return (
    <input
      className={cn(
        "h-11 w-full rounded-lg border border-[#c8c5b9] bg-white px-3 text-sm text-[#202722] outline-none transition-colors placeholder:text-[#6a756e] focus:border-[#925b3d] focus:ring-2 focus:ring-[#925b3d]/15 disabled:bg-[#eeeae2]",
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
        "min-h-28 w-full resize-y rounded-lg border border-[#c8c5b9] bg-white px-3 py-3 text-sm leading-6 text-[#202722] outline-none transition-colors placeholder:text-[#6a756e] focus:border-[#925b3d] focus:ring-2 focus:ring-[#925b3d]/15 disabled:bg-[#eeeae2]",
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
        "h-11 w-full rounded-lg border border-[#c8c5b9] bg-white px-3 text-sm text-[#202722] outline-none transition-colors focus:border-[#925b3d] focus:ring-2 focus:ring-[#925b3d]/15 disabled:bg-[#eeeae2]",
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
