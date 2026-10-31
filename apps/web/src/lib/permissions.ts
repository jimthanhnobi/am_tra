export const permissions = {
  catalogRead: "Catalog.Read",
  catalogWrite: "Catalog.Write",
  catalogPublish: "Catalog.Publish",
  mediaWrite: "Media.Write",
  contentRead: "Content.Read",
  contentWrite: "Content.Write",
  contentPublish: "Content.Publish",
  leadsRead: "Leads.Read",
  leadsManage: "Leads.Manage",
  settingsManage: "Settings.Manage",
} as const;

export function hasPermission(
  current: readonly string[],
  required: string,
): boolean {
  return current.includes(required);
}
