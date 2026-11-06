import type { Metadata } from "next";
import { Be_Vietnam_Pro, Lora } from "next/font/google";
import type { ReactNode } from "react";
import { getSiteContext } from "@/lib/config/site-context";
import "./globals.css";

const sans = Be_Vietnam_Pro({
  variable: "--font-be-vietnam",
  subsets: ["vietnamese"],
  weight: ["400", "500", "600"],
});

const serif = Lora({
  variable: "--font-lora",
  subsets: ["vietnamese"],
  weight: "variable",
});

export async function generateMetadata(): Promise<Metadata> {
  const { site, faviconUrl, defaultOgImageUrl } = await getSiteContext();
  return {
  metadataBase: new URL(site.url),
  title: {
    default: site.name,
    template: `%s · ${site.name}`,
  },
  description: site.description,
  applicationName: site.name,
  icons: faviconUrl ? { icon: faviconUrl } : undefined,
  openGraph: {
    type: "website",
    locale: "vi_VN",
    siteName: site.name,
    title: site.name,
    description: site.description,
    url: "/",
    images: defaultOgImageUrl ? [defaultOgImageUrl] : [],
  },
  robots: {
    index: site.indexable,
    follow: true,
  },
  };
}

export default function RootLayout({ children }: Readonly<{ children: ReactNode }>) {
  return (
    <html lang="vi" className={`${sans.variable} ${serif.variable}`}>
      <body>{children}</body>
    </html>
  );
}
