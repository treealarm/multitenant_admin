import { Box, Button, List, ListItem, ListItemText, TextField } from "@mui/material";
import { useEffect, useState } from "react";
import { useAppDispatch, useAppSelector } from "../store";
import { createRealm, deleteRealm, fetchLoggedInUser } from "../store/currentUserSlice";
import { UsersList } from "./UsersList";

export function RealmUsersManager() {
  const dispatch = useAppDispatch();
  const { user, loading } = useAppSelector((s) => s.curUser);

  const realmsOwned = user?.attributes?.realmsOwned ?? [];

  const [selectedRealm, setSelectedRealm] = useState<string | null>(null);
  const [newRealmName, setNewRealmName] = useState("");

  useEffect(() => {
    dispatch(fetchLoggedInUser());
  }, [dispatch]);

  const handleSelectRealm = (realm: string) => {
    setSelectedRealm(realm);
  };

  const handleCreateRealm = async () => {
    if (!newRealmName) return alert("Enter realm name");

    try {
      await dispatch(createRealm(newRealmName)).unwrap();
      setNewRealmName("");
      dispatch(fetchLoggedInUser());
    } catch (err: any) {
      alert(err.message);
    }
  };

  const handleDeleteRealm = async () => {
    if (!selectedRealm) return;

    if (!confirm(`Delete realm '${selectedRealm}'?`)) return;

    try {
      await dispatch(deleteRealm(selectedRealm)).unwrap();
      setSelectedRealm(null);
      dispatch(fetchLoggedInUser());
    } catch (err: any) {
      alert(err.message);
    }
  };


  return (
    <Box display="flex" height="100%" gap={2}>

      {/* LEFT COLUMN – REALMS */}
      <Box width={250} borderRight="1px solid #ccc" pr={1}>
        <h3>Your Realms</h3>

        <List>
          {realmsOwned.map((realm) => (
            <ListItem
              button
              key={realm}
              selected={realm === selectedRealm}
              onClick={() => handleSelectRealm(realm)}
            >
              <ListItemText primary={realm} />
            </ListItem>
          ))}
        </List>

        <Box mt={2} display="flex" flexDirection="column" gap={1}>
          <TextField
            label="New realm name"
            value={newRealmName}
            onChange={(e) => setNewRealmName(e.target.value)}
          />
          <Button variant="contained" onClick={handleCreateRealm}>
            Create Realm
          </Button>
          <Button
            variant="outlined"
            color="error"
            disabled={!selectedRealm}
            onClick={handleDeleteRealm}
          >
            Delete Realm
          </Button>
        </Box>
      </Box>

      {/* RIGHT COLUMN – USERS */}
      <Box flexGrow={1} pl={2}>
        {selectedRealm ? (
          <UsersList realm={selectedRealm} />
        ) : (
          <p>Select a realm to view its users</p>
        )}
      </Box>
    </Box>
  );
}
