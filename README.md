# multitenant_admin
## Standalone mode (TreeAlarm VMS)

With `MT_ADMIN_REALM` set, mt_admin is the user list of a standalone TreeAlarm VMS installation
instead of the multi-tenant console: it manages that one realm and nothing else. There are no
customers, no self-registration, no realm or database provisioning and no Kubernetes; only the
realm's administrators (realm role `admin`) can sign in, and only once they have replaced their
initial password in TreeAlarm VMS.

Administrators add and delete users, make them administrators, and reset passwords. A new user,
and one whose password was reset, gets the realm role `password-change-required`, so TreeAlarm VMS
makes them set a password of their own at the next sign-in.

Required in this mode: `MT_ADMIN_REALM`, `KEYCLOAK_URL`, `KEYCLOAK_ADMIN_USER`,
`KEYCLOAK_ADMIN_PASSWORD` (Keycloak's master-realm administrator), `KEYCLOAK_CLIENT_ID` (the realm's
public client with direct access grants) and `HTTP_ADMIN_PORT`.

`.github/workflows/release.yml` publishes what the installers take: the image `treealarm/mt_admin`
and `mt_admin-windows-x64.zip` in the `windows-build` prerelease.
