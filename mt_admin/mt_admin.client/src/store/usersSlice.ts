// usersSlice.ts
import { createAsyncThunk, createSlice, type PayloadAction } from "@reduxjs/toolkit";
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

// --- Async thunks ---
export const fetchUsers = createAsyncThunk<User[], string>(
  "users/fetch",
  async (realmName) => {
    const res = await authFetch(`/api/KeycloakAdmin/GetUsersByRealm?realm=${encodeURIComponent(realmName)}`);

    const text = await res.text();

    if (!res.ok) throw new Error(`Failed to load users (${res.status})`);
    return JSON.parse(text) as User[];
  }
);

// Добавляем пользователя
export const addUser = createAsyncThunk<void, { realmname: string; username: string; password: string }>(
  "users/add",
  async ({ realmname, username, password }) => {
    const res = await authFetch(`/api/KeycloakAdmin/CreateUser`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json", // <- важно
      },
      body: JSON.stringify({ username, password, realmname: realmname }),
    }

    );
    if (!res.ok) {
      const text = await res.text();
      throw new Error(`Failed to add user: ${text}`);
    }
    // ничего не возвращаем, после добавления можно заново fetchUsers
  }
);

// Удаление пользователя (если у Keycloak API есть DELETE)
export const deleteUser = createAsyncThunk<
  void,
  { realmname: string; username: string }
  >
  ("users/delete", async ({ username, realmname }) => {
    const res = await authFetch(`/api/KeycloakAdmin/DeleteUser`, {
      method: "DELETE",
      headers: {
        "Content-Type": "application/json", // <- важно
      },
      body: JSON.stringify({ username, realmname }),
  });

  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Failed to delete user: ${text}`);
  }
});


const usersSlice = createSlice({
  name: "users",
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      // fetchUsers
      .addCase(fetchUsers.pending, (state) => {
        state.loading = true;
        state.error = undefined;
      })
      .addCase(fetchUsers.fulfilled, (state, action: PayloadAction<User[]>) => {
        state.loading = false;
        state.items = action.payload;
      })
      .addCase(fetchUsers.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message;
      })
      // addUser
      .addCase(addUser.fulfilled, (state, action) => {
        // после добавления заново подгружаем пользователей
      })
      .addCase(addUser.rejected, (state, action) => {
        state.error = action.error.message;
      })
      // deleteUser
      .addCase(deleteUser.fulfilled, (state, action) => {
        // после удаления заново fetchUsers
      })
      .addCase(deleteUser.rejected, (state, action) => {
        state.error = action.error.message;
      });
  },
});

export default usersSlice.reducer;
