import { createSlice, createAsyncThunk } from "@reduxjs/toolkit";

interface LoginDto {
  realm: string;
  username: string;
  password: string;
}

interface AuthState {
  realm: string | null;
  token: string | null;
  loading: boolean;
  error?: string;
}

const initialState: AuthState = {
  realm: localStorage.getItem("realm"),
  token: localStorage.getItem("token"),
  loading: false,
};

export const login = createAsyncThunk<
  { realm: string; token: string },
  LoginDto
>("auth/login", async ({ realm, username, password }) => {
  const res = await fetch("/api/Auth/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ realm, username, password }),
  });

  const text = await res.text();

  if (!res.ok) {
    throw new Error(text || `Login failed (${res.status})`);
  }

  const data = JSON.parse(text);
  const token = data.access_token;
  localStorage.setItem("token", token);
  localStorage.setItem("realm", realm);

  return { realm, token };
});

const authSlice = createSlice({
  name: "auth",
  initialState,
  reducers: {
    logout(state) {
      state.realm = null;
      state.token = null;
      localStorage.removeItem("token");
      localStorage.removeItem("realm");
    },
    restoreSession(state) {
      const savedToken = localStorage.getItem("token");
      const savedRealm = localStorage.getItem("realm");
      if (savedToken && savedRealm) {
        state.token = savedToken;
        state.realm = savedRealm;
      }
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(login.pending, (state) => {
        state.loading = true;
        state.error = undefined;
      })
      .addCase(login.fulfilled, (state, action) => {
        state.loading = false;
        state.realm = action.payload.realm;
        state.token = action.payload.token;
      })
      .addCase(login.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message;
      });
  },
});

export const { logout, restoreSession } = authSlice.actions;
export default authSlice.reducer;
