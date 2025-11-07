import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import { authFetch } from "../authFetch";
export interface User {
  id: string;
  username: string;
  email?: string;
}

interface UsersState {
  items: User[];
  loading: boolean;
  error?: string;
}

const initialState: UsersState = {
  items: [],
  loading: false,
};

export const fetchUsers = createAsyncThunk<User[], string>(
  "users/fetch",
  async (realmName) => {
    const res = await authFetch(`/api/KeycloakAdmin/${realmName}`);

    const text = await res.text();  // читаем как текст
    console.log("Response body:", text);

    if (!res.ok) {
      throw new Error(`Failed to load users (${res.status})`);
    }

    try {
      return JSON.parse(text) as User[]; // пробуем парсить JSON
    } catch (err) {
      console.error("Failed to parse JSON:", err);
      throw new Error("Invalid JSON received from server");
    }
  }
);


const usersSlice = createSlice({
  name: "users",
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      .addCase(fetchUsers.pending, (state) => {
        state.loading = true;
        state.error = undefined;
      })
      .addCase(fetchUsers.fulfilled, (state, action) => {
        state.loading = false;
        state.items = action.payload;
      })
      .addCase(fetchUsers.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message;
      });
  },
});

export default usersSlice.reducer;
