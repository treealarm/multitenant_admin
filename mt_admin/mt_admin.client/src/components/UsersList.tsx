import { useEffect, useState } from "react";
import { useAppDispatch, useAppSelector } from "../store";
import { fetchUsers, addUser, deleteUser } from "../store/usersSlice";
import {
  CircularProgress,
  Container,
  Typography,
  List,
  ListItem,
  ListItemText,
  Button,
  TextField,
  Box,
} from "@mui/material";

export function UsersList({ realm }: { realm: string }) {
  const dispatch = useAppDispatch();
  const { items, loading, error } = useAppSelector((state: any) => state.users);

  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
  const [newUser, setNewUser] = useState({ username: "", password: "" });

  // Загружаем пользователей при монтировании и при смене realm
  useEffect(() => {
    dispatch(fetchUsers(realm));
  }, [realm, dispatch]);

  const handleSelect = (id: string) => {
    setSelectedUserId(id === selectedUserId ? null : id);
  };

  const handleAdd = async () => {
    if (newUser.username && newUser.password) {
      try {
        await dispatch(addUser({ realm, username: newUser.username, password: newUser.password })).unwrap();
        setNewUser({ username: "", password: "" });
        dispatch(fetchUsers(realm)); // обновляем список
      } catch (err: any) {
        alert(err.message);
      }
    } else {
      alert("Please enter username and password");
    }
  };

  const handleDelete = async () => {
    if (selectedUserId) {
      if (!confirm("Are you sure you want to delete this user?")) return;

      try {
        await dispatch(deleteUser({ realm, id: selectedUserId })).unwrap();
        setSelectedUserId(null);
        dispatch(fetchUsers(realm)); // обновляем список
      } catch (err: any) {
        alert(err.message);
      }
    }
  };

  if (loading) return <CircularProgress />;
  if (error) return <Typography color="error">{error}</Typography>;

  return (
    <Container>
      <Typography variant="h6" gutterBottom>
        Users in realm: {realm}
      </Typography>

      <List>
        {items.map((u: any) => (
          <ListItem
            key={u.id}
            selected={u.id === selectedUserId}
            onClick={() => handleSelect(u.id)}
            sx={{ cursor: "pointer" }}
          >
            <ListItemText primary={u.username} secondary={u.email} />
          </ListItem>
        ))}
      </List>

      <Box mt={2} display="flex" gap={1}>
        <TextField
          label="Username"
          value={newUser.username}
          onChange={(e) => setNewUser({ ...newUser, username: e.target.value })}
        />
        <TextField
          label="Password"
          type="password"
          value={newUser.password}
          onChange={(e) => setNewUser({ ...newUser, password: e.target.value })}
        />
        <Button variant="contained" onClick={handleAdd}>
          Add User
        </Button>
      </Box>

      <Box mt={2}>
        <Button
          variant="outlined"
          color="error"
          onClick={handleDelete}
          disabled={!selectedUserId}
        >
          Delete Selected User
        </Button>
      </Box>
    </Container>
  );
}
