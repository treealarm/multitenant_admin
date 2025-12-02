import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import { authFetch } from "../authFetch";

interface DBState {
  loading: boolean;
  error?: string;
  result?: string;
}

const initialState: DBState = {
  loading: false,
};

// =======================
// CREATE DB
// =======================
export const createDB = createAsyncThunk<string, string | null>(
  "db/create",
  async (dbName) => {
    const res = await authFetch(`/api/DB/CreateDB`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: dbName ? JSON.stringify(dbName) : "null",
    });

    const text = await res.text();
    if (!res.ok) throw new Error(text || `Failed to create DB (${res.status})`);

    return text; // "Realm X initialized"
  }
);

// =======================
// DROP DB
// =======================
export const dropDB = createAsyncThunk<string, string | null>(
  "db/drop",
  async (dbName) => {
    const res = await authFetch(`/api/DB/DropDB`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: dbName ? JSON.stringify(dbName) : "null",
    });

    const text = await res.text();
    if (!res.ok) throw new Error(text || `Failed to drop DB (${res.status})`);

    return text; // "Realm X dropped"
  }
);

// =======================
// SLICE
// =======================
const dbSlice = createSlice({
  name: "db",
  initialState,
  reducers: {
    clearDbState(state) {
      state.loading = false;
      state.error = undefined;
      state.result = undefined;
    },
  },
  extraReducers: (builder) => {
    builder
      // -------- CREATE DB ----------
      .addCase(createDB.pending, (state) => {
        state.loading = true;
        state.error = undefined;
        state.result = undefined;
      })
      .addCase(createDB.fulfilled, (state, action) => {
        state.loading = false;
        state.result = action.payload;
      })
      .addCase(createDB.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message;
      })

      // -------- DROP DB ----------
      .addCase(dropDB.pending, (state) => {
        state.loading = true;
        state.error = undefined;
        state.result = undefined;
      })
      .addCase(dropDB.fulfilled, (state, action) => {
        state.loading = false;
        state.result = action.payload;
      })
      .addCase(dropDB.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message;
      });
  },
});

export const { clearDbState } = dbSlice.actions;

export default dbSlice.reducer;
