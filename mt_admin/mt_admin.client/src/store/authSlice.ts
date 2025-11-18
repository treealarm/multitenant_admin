import { createSlice, createAsyncThunk } from "@reduxjs/toolkit";

interface LoginDto {
  realm: string;
  username: string;
  password: string;
}

interface AuthState {
  realm: string | null;
  token: string | null;
  refresh_token: string | null;
  loading: boolean;
  error?: string;
}

const initialState: AuthState = {
  realm: localStorage.getItem("realm"),
  token: localStorage.getItem("token"),
  refresh_token: localStorage.getItem("refresh_token"),
  loading: false,
};

export const login = createAsyncThunk<
  { realm: string; token: string; refresh_token:string },
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
  const refresh_token = data.refresh_token;
  localStorage.setItem("token", token);
  localStorage.setItem("realm", realm);
  localStorage.setItem("refresh_token", refresh_token);

  return { realm, token , refresh_token};
});

export const refreshToken = createAsyncThunk<{ token: string }, void>(
  "auth/refresh",
  async () => {
    const refresh = localStorage.getItem("refresh_token");
    const res = await fetch("/api/Auth/Refresh", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refresh_token: refresh }),
    });
    if (!res.ok) throw new Error("Cannot refresh token");
    const data = await res.json();
    localStorage.setItem("token", data.access_token);
    return { token: data.access_token };
  }
);

export const validateToken = createAsyncThunk<void, void>(
  "auth/validate",
  async (_, { dispatch }) => {
    const token = localStorage.getItem("token");
    if (!token) throw new Error("No token");

    const res = await fetch("/api/Auth/ValidateToken", {
      headers: { Authorization: `Bearer ${token}` },
    });

    if (res.status === 401) {
      dispatch(logout());
      throw new Error("Token expired or invalid");
    }
  }
);


const authSlice = createSlice({
  name: "auth",
  initialState,
  reducers: {
    logout(state) {
      state.realm = null;
      state.token = null;
      localStorage.removeItem("token");
      localStorage.removeItem("realm");
      localStorage.removeItem("refresh_token");
    },
    restoreSession(state) {
      const savedToken = localStorage.getItem("token");
      const savedRealm = localStorage.getItem("realm");
      const savedRefreshToken = localStorage.getItem("refresh_token");
      if (savedToken && savedRealm) {
        state.token = savedToken;
        state.realm = savedRealm;
        state.refresh_token = savedRefreshToken;
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
        state.refresh_token = action.payload.refresh_token;
      })
      .addCase(login.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message;
      });
  },
});

export const { logout, restoreSession } = authSlice.actions;
export default authSlice.reducer;
