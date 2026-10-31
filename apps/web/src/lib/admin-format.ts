export function formatAdminDate(value: string): string {
  return new Intl.DateTimeFormat("vi-VN", { dateStyle: "medium", timeStyle: "short", timeZone: "Asia/Ho_Chi_Minh" }).format(new Date(value));
}

export function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Không thể hoàn tất yêu cầu. Vui lòng thử lại.";
}
