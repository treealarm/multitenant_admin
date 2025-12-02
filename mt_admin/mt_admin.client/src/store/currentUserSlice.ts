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
  realmOp?: {
    loading: boolean;
    error?: string;
    result?: string;
  };
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
  reducers: {
    clearRealmOp(state) {
      state.realmOp = undefined;
    }

  },
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
      .addCase(createRealm.pending, (state) => {
        state.realmOp = { loading: true };
      })
      .addCase(createRealm.fulfilled, (state) => {
        state.realmOp = { loading: false, result: "Realm created successfully" };
      })
      .addCase(createRealm.rejected, (state, action) => {
        state.realmOp = { loading: false, error: action.error.message };
      })

      // --- deleteRealm ---
      .addCase(deleteRealm.pending, (state) => {
        state.realmOp = { loading: true };
      })
      .addCase(deleteRealm.fulfilled, (state) => {
        state.realmOp = { loading: false, result: "Realm deleted successfully" };
      })
      .addCase(deleteRealm.rejected, (state, action) => {
        state.realmOp = { loading: false, error: action.error.message };
      });
      ;
  },
});
export const { clearRealmOp } = currentUserSlice.actions;
export default currentUserSlice.reducer;
