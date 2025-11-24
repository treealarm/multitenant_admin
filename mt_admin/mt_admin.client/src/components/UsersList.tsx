/* eslint-disable react-hooks/exhaustive-deps */
/* eslint-disable @typescript-eslint/no-explicit-any */
import {
  Box,
  Button,
  CircularProgress,
  List,
  ListItem,
  ListItemButton,
  ListItemText,
  TextField,
  Typography,
} from "@mui/material";
import { useEffect, useState } from "react";
import { useAppDispatch, useAppSelector } from "../store";
import { addUser, deleteUser, fetchUsers } from "../store/usersSlice";

export function UsersList({
  realm,
  selectedUser,
  onSelectUser,
}: {
  realm: string;
  selectedUser: string | null;
  onSelectUser: (u: string | null) => void;
}) {
  const dispatch = useAppDispatch();
  const { items, loading, error } = useAppSelector((state: any) => state.users);

  const [newUser, setNewUser] = useState({ username: "", password: "" });

  // Загружаем пользователей при монтировании и смене realm
  useEffect(() => {
    dispatch(fetchUsers(realm));
    onSelectUser(null); // сбрасываем выбранного пользователя при смене realm
  }, [realm, dispatch]);

  const handleAdd = async () => {
    if (newUser.username && newUser.password) {
      try {
        await dispatch(
          addUser({
            realmname:realm,
            username: newUser.username,
            password: newUser.password,
          })
        ).unwrap();

        setNewUser({ username: "", password: "" });
        dispatch(fetchUsers(realm));
      } catch (err: any) {
        alert(err.message);
      }
    } else {
      alert("Please enter username and password");
    }
  };

  const handleDelete = async () => {
    if (!selectedUser) return;

    if (!confirm(`Delete user "${selectedUser}"?`)) return;

    try {
      await dispatch(
        deleteUser({
          realm,
          username: selectedUser,
        })
      ).unwrap();

      onSelectUser(null);
      dispatch(fetchUsers(realm));
    } catch (err: any) {
      alert(err.message);
    }
  };

  if (loading) {
    return (
      <Box p={2}>
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Typography color="error" p={2}>
        {error}
      </Typography>
    );
  }

  return (
    <Box display="flex" flexDirection="column" height="100%" p={2}>
      {/* Заголовок */}
      <Typography variant="h6" mb={2}>
        Users in realm: <strong>{realm}</strong>
      </Typography>

      {/* Список пользователей */}
      <Box flexGrow={1} overflow="auto" border="1px solid #ddd" borderRadius={1}>
        <List dense>
          {items.map((u: any) => (
            <ListItemButton
              key={u.username}
              selected={u.username === selectedUser}
              onClick={() =>
                onSelectUser(u.username === selectedUser ? null : u.username)
              }
              sx={{
                "&.Mui-selected": {
                  backgroundColor: "primary.light",
                  color: "white",
                },
              }}
            >
              <ListItemText primary={u.username} secondary={u.email} />
            </ListItemButton>
          ))}
        </List>
      </Box>

      {/* Добавление пользователя */}
      <Box mt={2} display="flex" gap={1}>
        <TextField
          label="Username"
          size="small"
          value={newUser.username}
          onChange={(e) =>
            setNewUser({ ...newUser, username: e.target.value })
          }
        />
        <TextField
          label="Password"
          size="small"
          type="password"
          value={newUser.password}
          onChange={(e) =>
            setNewUser({ ...newUser, password: e.target.value })
          }
        />
        <Button variant="contained" onClick={handleAdd}>
          Add
        </Button>
      </Box>

      {/* Удаление пользователя */}
      <Box mt={2}>
        <Button
          variant="outlined"
          color="error"
          disabled={!selectedUser}
          onClick={handleDelete}
          sx={{ textTransform: "none" }}
        >
          Delete Selected User {selectedUser ?? ""}
        </Button>
      </Box>
    </Box>
  );
}
