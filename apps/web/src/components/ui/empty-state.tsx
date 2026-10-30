import type { LucideIcon } from "lucide-react";

export function EmptyState({
  icon: Icon,
  title,
  description,
}: Readonly<{ icon: LucideIcon; title: string; description: string }>) {
  return (
    <div className="grid min-h-64 place-items-center px-6 py-12 text-center">
      <div>
        <span className="mx-auto grid size-12 place-items-center rounded-2xl bg-[#e8eee9] text-[#315d4d]">
          <Icon className="size-5" />
        </span>
        <h2 className="mt-4 font-serif text-xl font-semibold text-[#21382e]">{title}</h2>
        <p className="mx-auto mt-2 max-w-md text-sm leading-6 text-[#6a7770]">{description}</p>
      </div>
    </div>
  );
}
