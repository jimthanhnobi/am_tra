export default function AdminLoading() {
  return <main role="status" aria-label="Đang tải dữ liệu quản trị" className="space-y-6">
    <span className="sr-only">Đang tải dữ liệu quản trị…</span>
    <div aria-hidden="true" className="space-y-3">
      <div className="h-3 w-24 rounded bg-[#e6e1d6]" />
      <div className="h-9 w-56 max-w-full rounded bg-[#e6e1d6]" />
      <div className="h-4 w-80 max-w-full rounded bg-[#eeeae1]" />
    </div>
    <div aria-hidden="true" className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
      {[0, 1, 2].map((item) => <div key={item} className="admin-panel h-32 p-6"><div className="h-4 w-28 rounded bg-[#e6e1d6]" /><div className="mt-5 h-8 w-16 rounded bg-[#eeeae1]" /></div>)}
    </div>
    <div aria-hidden="true" className="admin-panel space-y-5 p-6">
      <div className="h-5 w-40 rounded bg-[#e6e1d6]" />
      {[0, 1, 2, 3].map((item) => <div key={item} className="flex gap-5 border-t border-[#eeeae1] pt-5"><div className="h-4 w-1/3 rounded bg-[#eeeae1]" /><div className="h-4 w-1/4 rounded bg-[#eeeae1]" /></div>)}
    </div>
  </main>;
}
