"use client";

import { EditorContent, useEditor } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";

const emptyDoc = { type: "doc", content: [{ type: "paragraph" }] };

export function TiptapArticleEditor({ value, onChange, disabled }: Readonly<{
  value: string; onChange: (value: string) => void; disabled: boolean;
}>) {
  const editor = useEditor({
    extensions: [StarterKit.configure({ heading: { levels: [2, 3] } })],
    content: (() => { try { return JSON.parse(value) as object; } catch { return emptyDoc; } })(),
    immediatelyRender: false,
    editable: !disabled,
    onUpdate: ({ editor }) => onChange(JSON.stringify(editor.getJSON())),
  });
  if (!editor) return <div className="min-h-56 rounded border border-[#d8d1c3] p-4">Đang mở trình biên tập…</div>;
  const actions = [
    { label: "Đậm", run: () => editor.chain().focus().toggleBold().run(), active: editor.isActive("bold") },
    { label: "Nghiêng", run: () => editor.chain().focus().toggleItalic().run(), active: editor.isActive("italic") },
    { label: "H2", run: () => editor.chain().focus().toggleHeading({ level: 2 }).run(), active: editor.isActive("heading", { level: 2 }) },
    { label: "H3", run: () => editor.chain().focus().toggleHeading({ level: 3 }).run(), active: editor.isActive("heading", { level: 3 }) },
    { label: "Danh sách", run: () => editor.chain().focus().toggleBulletList().run(), active: editor.isActive("bulletList") },
    { label: "Đánh số", run: () => editor.chain().focus().toggleOrderedList().run(), active: editor.isActive("orderedList") },
    { label: "Trích dẫn", run: () => editor.chain().focus().toggleBlockquote().run(), active: editor.isActive("blockquote") },
  ];
  return <div className="overflow-hidden rounded border border-[#d8d1c3] bg-white">
    <div role="toolbar" aria-label="Định dạng bài viết" className="flex flex-wrap gap-1 border-b border-[#d8d1c3] p-2">{actions.map((action) =>
      <button key={action.label} type="button" disabled={disabled} onClick={action.run} aria-pressed={action.active}
        className={`rounded px-3 py-1.5 text-sm ${action.active ? "bg-[#183c31] text-white" : "hover:bg-[#ece7dc]"}`}>{action.label}</button>)}</div>
    <EditorContent editor={editor} className="article-editor min-h-56 p-4" />
  </div>;
}
