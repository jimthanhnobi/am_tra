import type { ButtonHTMLAttributes } from "react";
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/lib/utils";

const buttonVariants = cva(
  "inline-flex min-h-11 items-center justify-center gap-2 rounded-lg px-4 text-sm font-semibold transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#925b3d] focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50",
  {
    variants: {
      variant: {
        default: "bg-[#233b32] text-white hover:bg-[#355749]",
        secondary: "border border-[#c8c5b9] bg-white text-[#233b32] hover:bg-[#f4f1e9]",
        ghost: "text-[#344a40] hover:bg-[#eee8dc]",
        danger: "bg-[#a33d32] text-white hover:bg-[#873027]",
      },
      size: {
        default: "h-11",
        sm: "h-10 min-h-10 px-3 text-xs",
        icon: "size-11 min-h-11 px-0",
      },
    },
    defaultVariants: { variant: "default", size: "default" },
  },
);

export type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> &
  VariantProps<typeof buttonVariants>;

export function Button({ className, variant, size, ...props }: ButtonProps) {
  return (
    <button
      className={cn(buttonVariants({ variant, size }), className)}
      {...props}
    />
  );
}

export { buttonVariants };
