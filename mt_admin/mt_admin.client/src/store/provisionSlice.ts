import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import { authFetch } from "../authFetch";

export interface ProvisionState {
  loading: boolean;
  error?: string;
  result?: string;
}

const initialState: ProvisionState = {
  loading: false,
};

/* =======================
   CREATE REALM
   ======================= */

export const createRealm = createAsyncThunk<string, string>(
  "provision/createRealm",
  async (realmName) => {
    const res = await authFetch("/api/Provision/CreateRealm", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ realmName }),
    });

    const text = await res.text();
    if (!res.ok)
      throw new Error(text || `Failed to create realm (${res.status})`);

    return text;
  }
);

/* =======================
   DELETE REALM
   ======================= */

export const deleteRealm = createAsyncThunk<string, string>(
  "provision/DeprovisionRealm",
  async (realmName) => {
    const res = await authFetch("/api/Provision/DeprovisionRealm", {
      method: "DELETE",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ realmName }),
    });

    const text = await res.text();
    if (!res.ok)
      throw new Error(text || `Failed to delete realm (${res.status})`);

    return text;
  }
);

const provisionSlice = createSlice({
  name: "provision",
  initialState,
  reducers: {
    resetProvision(state) {
      state.loading = false;
      state.error = undefined;
      state.result = undefined;
    },
  },
  extraReducers: (builder) => {
    builder
      // CREATE
      .addCase(createRealm.pending, (state) => {
        state.loading = true;
        state.error = undefined;
        state.result = undefined;
      })
      .addCase(createRealm.fulfilled, (state, action) => {
        state.loading = false;
        state.result = action.payload;
      })
      .addCase(createRealm.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message;
      })

      // DELETE
      .addCase(deleteRealm.pending, (state) => {
        state.loading = true;
        state.error = undefined;
      })
      .addCase(deleteRealm.fulfilled, (state, action) => {
        state.loading = false;
        state.result = action.payload;
      })
      .addCase(deleteRealm.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message;
      });
  },
});

export const { resetProvision } = provisionSlice.actions;
export default provisionSlice.reducer;
