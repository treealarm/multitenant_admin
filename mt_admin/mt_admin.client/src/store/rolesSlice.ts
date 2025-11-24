import { createSlice, createAsyncThunk } from "@reduxjs/toolkit";
import { authFetch } from "../authFetch";


// --- Получение ролей реалма ---
export const fetchRealmRoles = createAsyncThunk<string[], string>(
  "roles/fetchRealmRoles",
  async (realmName) => {
    const res = await authFetch(
      `/api/KeycloakAdmin/GetRealmRoles?realmName=${encodeURIComponent(
        realmName
      )}`
    );

    if (!res.ok) {
      throw new Error(`Failed to load roles (${res.status})`);
    }

    const roles = await res.json();

    return roles;
  }
);

export const fetchUserRoles = createAsyncThunk<
  string[],
  { realm: string; username: string }
>("roles/fetchUserRoles", async ({ realm, username }) => {
  const res = await authFetch(`/api/KeycloakAdmin/GetUserRoles`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ RealmName: realm, UserName: username }),
  });

  if (!res.ok) {
    throw new Error(`Failed to load user roles (${res.status})`);
  }
  return await res.json();
});


// --- Назначение роли ---
export const assignRolesToUser = createAsyncThunk<
  void,
  { realm: string; username: string; roles: string[] }
>("roles/assignRolesToUser", async ({ realm, username, roles }) => {
  const res = await authFetch(`/api/KeycloakAdmin/AssignRolesToUser`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      RealmName: realm,
      UserName: username,
      Roles: roles, // массив всех отмеченных ролей
    }),
  });

  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Failed to assign roles: ${text}`);
  }
});

interface RolesState {
  realmRoles: string[];
  userRoles: string[];
  loading: boolean;
  error?: string;
  lastUpdate: number; // новое поле
}

const rolesSlice = createSlice({
  name: "roles",
  initialState: {
    realmRoles: [] as string[],
    userRoles: [] as string[],
    loading: false,
    error: undefined,
    lastUpdate: 0, // инициализация
  } as RolesState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      .addCase(fetchRealmRoles.pending, (state) => {
        state.loading = true;
      })
      .addCase(fetchRealmRoles.fulfilled, (state, action) => {
        state.realmRoles = action.payload;
        state.loading = false;
      })
      .addCase(fetchRealmRoles.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message;
      })
      .addCase(fetchUserRoles.fulfilled, (state, action) => {
        state.userRoles = action.payload;
      })
      .addCase(assignRolesToUser.fulfilled, (state) => {
        state.lastUpdate = Date.now();
      });

  },
});


export default rolesSlice.reducer;
