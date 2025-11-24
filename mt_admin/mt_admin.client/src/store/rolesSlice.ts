import { createSlice, createAsyncThunk } from "@reduxjs/toolkit";
import { authFetch } from "../authFetch";

export const fetchRealmRoles = createAsyncThunk<string[], string>(
  "roles/fetchRealmRoles",
  async (realm) => {
    const res = await authFetch(`/admin/${realm}/roles`);
    return await res.json();
  }
);

export const fetchUserRoles = createAsyncThunk<
  string[],
  { realm: string; username: string }
>("roles/fetchUserRoles", async ({ realm, username }) => {
  const res = await authFetch(`/admin/${realm}/users/${username}/roles`);
  return await res.json();
});

export const assignRole = createAsyncThunk<
  void,
  { realm: string; username: string; roleName: string }
>("roles/assignRole", async ({ realm, username, roleName }) => {
  await authFetch(`/admin/${realm}/users/${username}/roles/${roleName}`, {
    method: "PUT",
  });
});

export const removeRole = createAsyncThunk<
  void,
  { realm: string; username: string; roleName: string }
>("roles/removeRole", async ({ realm, username, roleName }) => {
  await authFetch(`/admin/${realm}/users/${username}/roles/${roleName}`, {
    method: "DELETE",
  });
});

const rolesSlice = createSlice({
  name: "roles",
  initialState: {
    realmRoles: [] as string[],
    userRoles: [] as string[],
    loading: false,
  },
  reducers: {},
  extraReducers: (builder) => {
    builder
      .addCase(fetchRealmRoles.pending, (s) => {
        s.loading = true;
      })
      .addCase(fetchRealmRoles.fulfilled, (s, a) => {
        s.realmRoles = a.payload;
        s.loading = false;
      })
      .addCase(fetchUserRoles.fulfilled, (s, a) => {
        s.userRoles = a.payload;
      });
  },
});

export default rolesSlice.reducer;
