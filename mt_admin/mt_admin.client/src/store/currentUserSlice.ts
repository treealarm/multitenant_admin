/* eslint-disable @typescript-eslint/no-unused-vars */
import { createAsyncThunk, createSlice, type PayloadAction } from "@reduxjs/toolkit";
import { authFetch } from "../authFetch";

export interface CurrentUser {
  id: string;
  username: string;
  email?: string;
  attributes?: {
    realmsOwned?: string[];
  };
}

interface CurrentUserState {
  user?: CurrentUser;
  loading: boolean;
  error?: string;
}

const initialState: CurrentUserState = {
  loading: false,
};

export const createRealm = createAsyncThunk<void, string>(
  "realms/createRealm",
  async (realmName) => {
    const res = await authFetch(`/api/KeycloakAdmin/CreateRealm?realmName=${encodeURIComponent(realmName)}`, {
      method: "POST",
    });

    const text = await res.text();
    if (!res.ok) throw new Error(text);
  }
);

export const deleteRealm = createAsyncThunk<void, string>(
  "realms/deleteRealm",
  async (realmName) => {
    const res = await authFetch(`/api/KeycloakAdmin/DeleteRealm`, {
      method: "DELETE",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ realmName }),
    });

    const text = await res.text();
    if (!res.ok) throw new Error(text);
  }
);


// --- Thunk to load logged-in user ---
export const fetchLoggedInUser = createAsyncThunk<CurrentUser>(
  "currentUser/fetch",
  async () => {
    const res = await authFetch(`/api/KeycloakAdmin/GetLoggedInUser`);

    const text = await res.text();

    if (!res.ok) throw new Error(`Failed to load current user (${res.status})`);

    return JSON.parse(text) as CurrentUser;
  }
);

const currentUserSlice = createSlice({
  name: "currentUser",
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      .addCase(fetchLoggedInUser.pending, (state) => {
        state.loading = true;
        state.error = undefined;
      })
      .addCase(fetchLoggedInUser.fulfilled, (state, action: PayloadAction<CurrentUser>) => {
        state.loading = false;
        state.user = action.payload;
      })
      .addCase(fetchLoggedInUser.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message;
      })
      .addCase(createRealm.fulfilled, (state) => { })
      .addCase(deleteRealm.fulfilled, (state) => { });
      ;
  },
});

export default currentUserSlice.reducer;
