import Link from "next/link";
import Image from "next/image";
import type { PublicArticlePreview } from "@/lib/api/articles";

export function ArticleCard({ article, headingLevel = 2 }: Readonly<{ article: PublicArticlePreview; headingLevel?: 2 | 3 }>) {
  const Heading = headingLevel === 2 ? "h2" : "h3";
  return <article className="sf-card group min-w-0">
    <Link href={`/kien-thuc/${article.slug}`} className="sf-card-media block aspect-[3/2]">
      {article.coverUrl && article.coverWidth && article.coverHeight
        ? <Image src={article.coverUrl} alt="" width={article.coverWidth} height={article.coverHeight} sizes="(max-width: 640px) 90vw, (max-width: 1024px) 45vw, 30vw" className="h-full w-full object-cover" />
        : <div className="sf-image-empty"><span>Ảnh bài viết chưa được cập nhật</span></div>}
    </Link>
    <div className="flex min-h-48 flex-col p-6">
      <time dateTime={article.publishedAt} className="sf-kicker">{new Date(article.publishedAt).toLocaleDateString("vi-VN")}</time>
      <Heading className="mt-3 font-serif text-[1.45rem] leading-snug text-[#233b32]"><Link href={`/kien-thuc/${article.slug}`} className="hover:underline">{article.title}</Link></Heading>
      {article.excerpt && <p className="mt-3 line-clamp-3 text-sm leading-7 text-[#56635b]">{article.excerpt}</p>}
      <Link href={`/kien-thuc/${article.slug}`} className="sf-text-link mt-auto pt-5">Đọc bài viết <span aria-hidden="true">↗</span></Link>
    </div>
  </article>;
}
