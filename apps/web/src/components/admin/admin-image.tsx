import Image from "next/image";

export function AdminImage({ src, alt, width, height, className, loading = "lazy" }: Readonly<{ src: string; alt: string; width: number; height: number; className?: string; loading?: "lazy" | "eager" }>) {
  return <Image src={src} alt={alt} width={width} height={height} sizes="(max-width: 640px) 50vw, (max-width: 1280px) 25vw, 180px" loading={loading} className={className} />;
}
