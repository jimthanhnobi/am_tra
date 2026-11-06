"use client";

import Image from "next/image";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useRef, useState } from "react";
import { ArrowUpRight, ChevronLeft, ChevronRight } from "lucide-react";
import type { PublicProductCard } from "@/lib/api/public-types";

type OrbitProduct = PublicProductCard & { coverImage: NonNullable<PublicProductCard["coverImage"]> };
type Size = { width: number; height: number };
type Drag = { id: number; x: number; y: number; start: number; live: boolean; moved: boolean };

const mod = (value: number, count: number) => ((value % count) + count) % count;
const pad = (value: number) => String(value).padStart(2, "0");

function geometry({ width, height }: Size) {
  if (width <= 650) return { cx: width * 1.08, cy: height * (width <= 350 ? 1 : .90), radius: width * .9, angle: 126, step: 38 };
  if (width <= 1050) return { cx: width * 1.10, cy: height * 1.14, radius: height * .73, angle: 122, step: 33 };
  return { cx: width * .83, cy: height * 1.13, radius: height * .84, angle: 122, step: 32 };
}

export function HomeOrbit({ products }: Readonly<{ products: readonly OrbitProduct[] }>) {
  const router = useRouter();
  const heroRef = useRef<HTMLElement>(null);
  const dragRef = useRef<Drag | null>(null);
  const suppressClickRef = useRef(false);
  const positionRef = useRef(0);
  const targetRef = useRef(0);
  const velocityRef = useRef(0);
  const frameRef = useRef<number | null>(null);
  const lastFrameRef = useRef(0);
  const reducedMotionRef = useRef(false);
  const [size, setSize] = useState<Size>({ width: 0, height: 0 });
  const [position, setPosition] = useState(0);
  const [dragging, setDragging] = useState(false);
  const count = products.length;

  const tick = useCallback(function tickFrame(time: number) {
    const dt = lastFrameRef.current ? Math.min(.05, (time - lastFrameRef.current) / 1000) : 1 / 60;
    lastFrameRef.current = time;
    if (reducedMotionRef.current) {
      positionRef.current = targetRef.current;
      velocityRef.current = 0;
    } else {
      const settle = dragRef.current?.live ? .12 : .46;
      const omega = 2 / settle;
      const x = omega * dt;
      const decay = 1 / (1 + x + .48 * x * x + .235 * x * x * x);
      const change = positionRef.current - targetRef.current;
      const temp = (velocityRef.current + omega * change) * dt;
      velocityRef.current = (velocityRef.current - omega * temp) * decay;
      positionRef.current = targetRef.current + (change + temp) * decay;
    }
    if (!dragRef.current && Math.abs(targetRef.current - positionRef.current) < .0004 && Math.abs(velocityRef.current) < .0004) {
      positionRef.current = targetRef.current;
      velocityRef.current = 0;
    }
    setPosition(positionRef.current);
    if (dragRef.current || Math.abs(targetRef.current - positionRef.current) > .0004 || Math.abs(velocityRef.current) > .0004) {
      frameRef.current = requestAnimationFrame(tickFrame);
    } else {
      frameRef.current = null;
      lastFrameRef.current = 0;
    }
  }, []);

  const start = useCallback(() => {
    if (frameRef.current === null) frameRef.current = requestAnimationFrame(tick);
  }, [tick]);

  useEffect(() => {
    const hero = heroRef.current;
    if (!hero) return;
    reducedMotionRef.current = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const observer = new ResizeObserver(([entry]) => {
      if (entry) setSize({ width: entry.contentRect.width, height: entry.contentRect.height });
    });
    observer.observe(hero);
    return () => {
      observer.disconnect();
      if (frameRef.current !== null) cancelAnimationFrame(frameRef.current);
      frameRef.current = null;
    };
  }, []);

  const goTo = useCallback((index: number) => {
    if (count < 2) return;
    const current = mod(Math.round(targetRef.current), count);
    let delta = mod(index - current, count);
    if (delta > count / 2) delta -= count;
    targetRef.current = Math.round(targetRef.current) + delta;
    start();
  }, [count, start]);

  const step = (direction: number) => {
    targetRef.current = Math.round(targetRef.current) + direction;
    start();
  };

  const onPointerDown = (event: React.PointerEvent<HTMLDivElement>) => {
    if (count < 2 || (event.pointerType === "mouse" && event.button !== 0)) return;
    dragRef.current = { id: event.pointerId, x: event.clientX, y: event.clientY, start: targetRef.current, live: false, moved: false };
  };
  const onPointerMove = (event: React.PointerEvent<HTMLDivElement>) => {
    const drag = dragRef.current;
    if (!drag || drag.id !== event.pointerId) return;
    const dx = event.clientX - drag.x;
    const dy = event.clientY - drag.y;
    if (!drag.live) {
      if (Math.abs(dy) > 10 && Math.abs(dy) > Math.abs(dx)) { dragRef.current = null; return; }
      if (Math.abs(dx) < 8 || Math.abs(dx) < Math.abs(dy)) return;
      drag.live = true;
      event.currentTarget.setPointerCapture(event.pointerId);
      setDragging(true);
    }
    drag.moved = true;
    targetRef.current = drag.start - dx / (size.width <= 650 ? 115 : 205);
    start();
    event.preventDefault();
  };
  const onPointerEnd = (event: React.PointerEvent<HTMLDivElement>) => {
    const drag = dragRef.current;
    if (!drag || drag.id !== event.pointerId) return;
    dragRef.current = null;
    setDragging(false);
    if (drag.moved) {
      suppressClickRef.current = true;
      window.setTimeout(() => { suppressClickRef.current = false; }, 0);
    }
    targetRef.current = Math.round(targetRef.current);
    start();
  };

  const ready = size.width > 0;
  const g = geometry(ready ? size : { width: 1440, height: 900 });
  const activeIndex = count ? mod(Math.round(position), count) : 0;
  const active = products[activeIndex];
  const ringStyle = (radius: number): React.CSSProperties => ({ left: g.cx, top: g.cy, width: radius * 2, height: radius * 2 });
  const itemPosition = (index: number) => {
    let distance = mod(index - position, count);
    if (distance > count / 2) distance -= count;
    const angle = (g.angle - distance * g.step) * Math.PI / 180;
    const abs = Math.abs(distance);
    const opacity = Math.max(0, Math.min(1, distance < 0 ? 1 + distance * 1.8 : 1 - Math.max(0, distance - 1) * 1.8));
    return {
      left: g.cx + g.radius * Math.cos(angle),
      top: g.cy - g.radius * Math.sin(angle),
      transform: `translate(-50%, -62%) scale(${Math.max(.58, 1 - .18 * abs)})`,
      opacity,
      zIndex: 10 - Math.round(abs),
      pointerEvents: opacity < .15 ? "none" as const : "auto" as const,
    };
  };

  return <section ref={heroRef} className="sf-orbit-hero" data-ready={ready} aria-label="Sản phẩm nổi bật">
    <div className="sf-orbit-scene" aria-hidden="true" />
    <div className="sf-orbit-stage" data-dragging={dragging} onPointerDown={onPointerDown} onPointerMove={onPointerMove} onPointerUp={onPointerEnd} onPointerCancel={onPointerEnd}>
      <span className="sf-orbit-ring" style={ringStyle(g.radius)} aria-hidden="true" />
      <span className="sf-orbit-ring sf-orbit-ring-inner" style={ringStyle(Math.max(110, g.radius - 120))} aria-hidden="true" />
      <span className="sf-orbit-ring sf-orbit-ring-outer" style={ringStyle(g.radius + 34)} aria-hidden="true" />
      {products.map((product, index) => {
        const placement = itemPosition(index);
        return <button key={product.slug} type="button" className="sf-orbit-item" data-active={index === activeIndex}
          style={ready ? placement : { opacity: index === 0 ? 1 : 0 }}
          aria-label={index === activeIndex ? `Xem chi tiết ${product.name}` : `Chọn ${product.name}`}
          aria-current={index === activeIndex ? "true" : undefined}
          tabIndex={index === activeIndex ? 0 : -1}
          onClick={() => { if (suppressClickRef.current) return; if (index === activeIndex) router.push(`/san-pham/${product.slug}`); else goTo(index); }}>
          <Image src={product.coverImage.publicUrl!} alt={product.coverImage.altText || product.name}
            width={product.coverImage.width} height={product.coverImage.height} sizes="(max-width: 650px) 280px, 360px"
            preload={index === 0} draggable={false} />
        </button>;
      })}
      {!count && <div className="sf-orbit-empty-art" aria-hidden="true" />}
    </div>
    <div className="sf-orbit-copy" aria-live="polite">
      <p className="sf-orbit-eyebrow">{count ? `${pad(activeIndex + 1)} / ${pad(count)}　 Tuyển chọn` : "Trà cụ · Góc thưởng trà"}</p>
      <h1>{active?.name || "Một khoảng dừng dành cho trà."}</h1>
      <p className="sf-orbit-description">{active?.summary || "Khám phá hình dáng, chất liệu và câu chuyện của từng món trà cụ khi bộ sưu tập được cập nhật."}</p>
      {active && <p className="sf-orbit-category">{active.primaryCategory?.name || "Trà cụ"}{active.brand ? `　·　${active.brand.name}` : ""}</p>}
      <Link href={active ? `/san-pham/${active.slug}` : "#bo-suu-tap"} className="sf-orbit-detail">
        {active ? "Xem chi tiết" : "Khám phá bộ sưu tập"} <ArrowUpRight size={15} aria-hidden="true" />
      </Link>
    </div>
    <div className="sf-orbit-edition" aria-hidden="true">TRÀ CỤ · TUYỂN CHỌN</div>
    <div className="sf-orbit-bottom">
      <div className="sf-orbit-count"><span>Sản phẩm nổi bật</span><strong>{pad(count ? activeIndex + 1 : 0)} / {pad(count)}</strong></div>
      {count > 1 && <div className="sf-orbit-controls">
        <button type="button" aria-label="Sản phẩm trước" onClick={() => step(-1)}><ChevronLeft size={21} strokeWidth={1.5} aria-hidden="true" /></button>
        <div className="sf-orbit-range-wrap">
          <div className="sf-orbit-range-heading"><span>Khám phá bộ sưu tập</span><strong>{pad(activeIndex + 1)} / {pad(count)}</strong></div>
          <input type="range" min={0} max={count - 1} step={1} value={activeIndex}
            aria-label="Chọn sản phẩm nổi bật" aria-valuetext={active.name}
            onChange={(event) => goTo(Number(event.target.value))} />
        </div>
        <button type="button" aria-label="Sản phẩm tiếp theo" onClick={() => step(1)}><ChevronRight size={21} strokeWidth={1.5} aria-hidden="true" /></button>
        <p className="sf-orbit-hint">Giữ và kéo để xoay</p>
      </div>}
      <div className="sf-orbit-selected"><span>{active?.primaryCategory?.name || "Bộ sưu tập"}</span><strong>{active?.name || "Đang cập nhật"}</strong></div>
    </div>
  </section>;
}
