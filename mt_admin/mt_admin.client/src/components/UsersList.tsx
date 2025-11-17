/* eslint-disable @typescript-eslint/no-explicit-any */
import {
    Box,
    Button,
    CircularProgress,
    Container,
    List,
    ListItem,
    ListItemText,
    TextField,
    Typography,
} from "@mui/material";
import { useEffect, useState } from "react";
import { useAppDispatch, useAppSelector } from "../store";
import { addUser, deleteUser, fetchUsers } from "../store/usersSlice";

export function UsersList({ realm }: { realm: string }) {
  const dispatch = useAppDispatch();
  const { items, loading, error } = useAppSelector((state: any) => state.users);

  const [selectedUserName, setSelectedUserName] = useState<string | null>(null);
  const [newUser, setNewUser] = useState({ username: "", password: "" });

  // Загружаем пользователей при монтировании и при смене realm
  useEffect(() => {
    dispatch(fetchUsers(realm));
  }, [realm, dispatch]);

  const handleSelect = (userName: string) => {
    setSelectedUserName(userName === selectedUserName ? null : userName);
  };

  const handleAdd = async () => {
    if (newUser.username && newUser.password) {
      try {
        await dispatch(addUser({ realmname: realm, username: newUser.username, password: newUser.password })).unwrap();
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
    if (selectedUserName) {
      if (!confirm("Are you sure you want to delete this user?")) return;

      try {
        await dispatch(deleteUser({ realmname: realm, username: selectedUserName })).unwrap();
        setSelectedUserName(null);
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
            key={u.username}
            selected={u.username === selectedUserName}
            onClick={() => handleSelect(u.username)}
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
          disabled={!selectedUserName}
          sx={{ textTransform: "none" }} 
        >
          Delete Selected User {selectedUserName}
        </Button>
      </Box>
    </Container>
  );
}
