export const permissions = {
  catalogRead: "Catalog.Read",
  catalogWrite: "Catalog.Write",
  catalogPublish: "Catalog.Publish",
  mediaWrite: "Media.Write",
} as const;

export function hasPermission(
  current: readonly string[],
  required: string,
): boolean {
  return current.includes(required);
}
