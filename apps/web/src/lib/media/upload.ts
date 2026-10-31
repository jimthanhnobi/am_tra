"use client";

import { adminRequest, jsonBody } from "@/lib/api/client";
import type { AdminMediaAsset, MediaUploadIntent } from "@/lib/api/catalog-types";

export const supportedImageTypes = ["image/jpeg", "image/png", "image/webp"];
export const maximumImageBytes = 10 * 1024 * 1024;

export function validateImage(file: File): string | null {
  if (!supportedImageTypes.includes(file.type)) return "Chỉ chấp nhận ảnh JPEG, PNG hoặc WebP.";
  if (file.size < 1 || file.size > maximumImageBytes) return "Ảnh phải nhỏ hơn hoặc bằng 10 MB.";
  return null;
}

export async function uploadImage(file: File, onProgress: (percent: number) => void): Promise<AdminMediaAsset> {
  const error = validateImage(file);
  if (error) throw new Error(error);
  const intent = await adminRequest<MediaUploadIntent>("media/upload-intent", { method: "POST", body: jsonBody({ fileName: file.name, contentType: file.type, fileSize: file.size }) });
  await new Promise<void>((resolve, reject) => {
    const request = new XMLHttpRequest();
    request.open("PUT", intent.presignedUrl);
    for (const [key, value] of Object.entries(intent.requiredHeaders)) request.setRequestHeader(key, value);
    request.upload.onprogress = (event) => { if (event.lengthComputable) onProgress(Math.round(event.loaded / event.total * 100)); };
    request.onload = () => request.status >= 200 && request.status < 300 ? resolve() : reject(new Error(`R2 từ chối ảnh (HTTP ${request.status}).`));
    request.onerror = () => reject(new Error("Không thể tải ảnh lên R2."));
    request.send(file);
  });
  return await adminRequest<AdminMediaAsset>(`media/${intent.uploadId}/complete`, { method: "POST" });
}
