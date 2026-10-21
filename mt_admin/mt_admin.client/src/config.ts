// What the server is: the multi-tenant console, or one realm's user list inside a standalone
// TreeAlarm VMS installation (mt_admin's MT_ADMIN_REALM). Asked once, before anything is drawn.
export interface AppConfig {
  standalone: boolean;
  realm: string | null;
}

let pending: Promise<AppConfig> | null = null;

export function loadConfig(): Promise<AppConfig> {
  pending ??= fetch("/api/config").then(async (res) => {
    if (!res.ok) throw new Error(`GET /api/config: ${res.status}`);
    return (await res.json()) as AppConfig;
  });
  return pending;
}
