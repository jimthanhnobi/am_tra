"use client";

import { useState } from "react";
import { EditorContent, useEditor } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";

const emptyDoc = { type: "doc", content: [{ type: "paragraph" }] };

export function TiptapArticleEditor({ value, onChange, disabled }: Readonly<{
  value: string; onChange: (value: string) => void; disabled: boolean;
}>) {
  const [linkOpen, setLinkOpen] = useState(false);
  const [linkHref, setLinkHref] = useState("");
  const [linkError, setLinkError] = useState("");
  const editor = useEditor({
    extensions: [StarterKit.configure({ heading: { levels: [2, 3] } })],
    content: (() => { try { return JSON.parse(value) as object; } catch { return emptyDoc; } })(),
    immediatelyRender: false,
    editable: !disabled,
    editorProps: { attributes: { "aria-label": "Nội dung bài viết", class: "focus:outline-none" } },
    onUpdate: ({ editor }) => onChange(JSON.stringify(editor.getJSON())),
  });
  if (!editor) return <div role="status" className="min-h-56 rounded-lg border border-[#ddd8cc] p-4">Đang mở trình biên tập…</div>;

  const actions = [
    { label: "Đậm", run: () => editor.chain().focus().toggleBold().run(), active: editor.isActive("bold") },
    { label: "Nghiêng", run: () => editor.chain().focus().toggleItalic().run(), active: editor.isActive("italic") },
    { label: "H2", run: () => editor.chain().focus().toggleHeading({ level: 2 }).run(), active: editor.isActive("heading", { level: 2 }) },
    { label: "H3", run: () => editor.chain().focus().toggleHeading({ level: 3 }).run(), active: editor.isActive("heading", { level: 3 }) },
    { label: "Danh sách", run: () => editor.chain().focus().toggleBulletList().run(), active: editor.isActive("bulletList") },
    { label: "Đánh số", run: () => editor.chain().focus().toggleOrderedList().run(), active: editor.isActive("orderedList") },
    { label: "Trích dẫn", run: () => editor.chain().focus().toggleBlockquote().run(), active: editor.isActive("blockquote") },
  ];
  function applyLink() {
    try {
      const url = new URL(linkHref.trim());
      if (!['https:', 'http:'].includes(url.protocol)) throw new Error("invalid protocol");
      editor?.chain().focus().extendMarkRange("link").setLink({ href: url.href }).run();
      setLinkError(""); setLinkOpen(false); setLinkHref("");
    } catch { setLinkError("Nhập URL bắt đầu bằng https:// hoặc http://."); }
  }
  return <div className="overflow-hidden rounded-xl border border-[#ddd8cc] bg-white">
    <div role="toolbar" aria-label="Định dạng bài viết" className="flex flex-wrap items-center gap-1.5 border-b border-[#ddd8cc] bg-[#f7f4ec] p-2 sm:p-3">
      {actions.map((action) => <button key={action.label} type="button" disabled={disabled} onClick={action.run} aria-pressed={action.active} className={`min-h-9 rounded-lg border px-3 text-xs font-semibold transition-colors sm:min-h-10 sm:text-sm ${action.active ? "border-[#233b32] bg-[#233b32] text-white" : "border-transparent text-[#233b32] hover:border-[#c8c5b9] hover:bg-white"}`}>{action.label}</button>)}
      <span aria-hidden="true" className="mx-1 h-6 w-px bg-[#ddd8cc]" />
      <button type="button" disabled={disabled} onClick={() => { setLinkOpen((open) => !open); setLinkHref(editor.getAttributes("link").href || ""); }} className="min-h-9 rounded-lg px-3 text-xs font-semibold text-[#233b32] hover:bg-white sm:min-h-10 sm:text-sm">Liên kết</button>
      {editor.isActive("link") && <button type="button" disabled={disabled} onClick={() => editor.chain().focus().unsetLink().run()} className="min-h-9 rounded-lg px-3 text-xs font-semibold text-[#233b32] hover:bg-white sm:min-h-10 sm:text-sm">Gỡ link</button>}
      <a href="#article-media" className="inline-flex min-h-9 items-center rounded-lg px-3 text-xs font-semibold text-[#233b32] hover:bg-white sm:min-h-10 sm:text-sm">Ảnh bìa ↗</a>
    </div>
    {linkOpen && <div className="flex flex-wrap items-end gap-2 border-b border-[#ddd8cc] bg-[#fffefa] p-3"><label className="min-w-48 flex-1 text-xs font-semibold text-[#233b32]">URL liên kết<input type="url" autoFocus value={linkHref} onChange={(event) => { setLinkHref(event.target.value); setLinkError(""); }} onKeyDown={(event) => { if (event.key === "Enter") { event.preventDefault(); applyLink(); } }} placeholder="https://..." className="mt-1 h-10 w-full rounded-lg border border-[#c8c5b9] px-3 text-sm" /></label><button type="button" onClick={applyLink} className="min-h-10 rounded-lg bg-[#233b32] px-4 text-sm font-semibold text-white">Áp dụng</button><button type="button" onClick={() => setLinkOpen(false)} className="min-h-10 px-3 text-sm">Hủy</button>{linkError && <p role="alert" className="w-full text-xs text-[#a33d32]">{linkError}</p>}</div>}
    <EditorContent editor={editor} className="article-editor min-h-56 p-5 sm:p-8" />
    <p className="border-t border-[#eeeae1] px-5 py-3 text-xs leading-5 text-[#56635b]">Nội dung được lưu dưới dạng JSON; HTML công khai do server tạo và làm sạch. Ảnh trong bài chưa được hỗ trợ, hãy chọn ảnh bìa bên dưới.</p>
  </div>;
}
