import { useEffect } from "react";
import { useAppDispatch, useAppSelector } from '../store';
import { fetchUsers } from "../store/usersSlice";

import {
  CircularProgress,
  Container,
  Typography,
  List,
  ListItem,
  ListItemText,
} from "@mui/material";
import React from "react";

export function UsersList({ realm }: { realm: string }) {
  const dispatch = useAppDispatch();

  const { items, loading, error } = useAppSelector(
    (state: RootState) => state.users
  );

  useEffect(() => {
    dispatch(fetchUsers(realm));
  }, [realm, dispatch]);

  if (loading) return <CircularProgress />;
  if (error) return <Typography color="error">{error}</Typography>;

  return (
    <Container>
      <Typography variant="h6" gutterBottom>
        Users in realm: {realm}
      </Typography>
      <List>
        {items.map((u) => (
          <ListItem key={u.id}>
            <ListItemText primary={u.username} secondary={u.email} />
          </ListItem>
        ))}
      </List>
    </Container>
  );
}
