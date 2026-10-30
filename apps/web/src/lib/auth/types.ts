export type CurrentAdminUser = Readonly<{
  id: string;
  email: string;
  roles: readonly string[];
  permissions: readonly string[];
}>;

export type AntiforgeryToken = Readonly<{
  headerName: string;
  requestToken: string;
}>;
