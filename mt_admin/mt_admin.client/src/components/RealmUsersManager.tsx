/* eslint-disable @typescript-eslint/no-unused-vars */
import {
  Box,
  Toolbar,
  IconButton,
  Tooltip,
  List,
  ListItemText,
  Dialog,
  TextField,
  Button,
  ListItemButton
} from "@mui/material";

import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import { createRealm, deleteRealm, fetchLoggedInUser } from "../store/currentUserSlice";
import { useEffect, useState } from "react";
import { useAppDispatch, useAppSelector } from "../store";
import { UsersList } from "./UsersList";
import { RolesEditor } from "./RolesEditor";

export function RealmUsersManager() {
  const dispatch = useAppDispatch();
  const { user } = useAppSelector((s) => s.curUser);

  const [selectedRealm, setSelectedRealm] = useState<string | null>(null);
  const [selectedUser, setSelectedUser] = useState<string | null>(null);

  const [newRealmDialogOpen, setNewRealmDialogOpen] = useState(false);
  const [newRealmName, setNewRealmName] = useState("");

  const realmsOwned = user?.attributes?.realmsOwned ?? [];

  useEffect(() => {
    dispatch(fetchLoggedInUser());
  }, [dispatch]);

  const handleCreateRealm = async () => {
    await dispatch(createRealm(newRealmName)).unwrap();
    setNewRealmName("");
    setNewRealmDialogOpen(false);
    dispatch(fetchLoggedInUser());
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
    <Box display="flex" height="100%">
      {/* LEFT COLUMN */}
      <Box
        width={260}
        borderRight="1px solid #ccc"
        display="flex"
        flexDirection="column"
      >
        <Toolbar variant="dense">
          <Tooltip title="Create Realm">
            <IconButton onClick={() => setNewRealmDialogOpen(true)}>
              <AddIcon />
            </IconButton>
          </Tooltip>

          <Tooltip title="Delete Realm">
            <IconButton
              color="error"
              disabled={!selectedRealm}
              onClick={handleDeleteRealm}
            >
              <DeleteIcon />
            </IconButton>
          </Tooltip>
        </Toolbar>

        <Box flexGrow={1} overflow="auto">
          <List dense>
            {realmsOwned.map((realm) => (
              <ListItemButton
                key={realm}
                selected={realm === selectedRealm}
                onClick={() => {
                  setSelectedRealm(realm);
                  setSelectedUser(null);
                }}
                sx={{
                  "&.Mui-selected": {
                    backgroundColor: "primary.light",
                    color: "white",
                  },
                  "&.Mui-selected:hover": {
                    backgroundColor: "primary.main",
                  },
                }}
              >
                <ListItemText primary={realm} />
              </ListItemButton>
            ))}
          </List>
        </Box>

      </Box>

      {/* CENTER COLUMN */}
      <Box
        flexGrow={1}
        borderRight="1px solid #ccc"
        display="flex"
        flexDirection="column"
        overflow="hidden"
      >
        {selectedRealm ? (
          <UsersList
            realm={selectedRealm}
            selectedUser={selectedUser}
            onSelectUser={setSelectedUser}
          />
        ) : (
          <Box p={2}>Select a realm</Box>
        )}
      </Box>

      {/* RIGHT COLUMN */}
      <Box
        width={260}
        display="flex"
        flexDirection="column"
        overflow="hidden"
      >
        {selectedUser ? (
          <RolesEditor realm={selectedRealm!} username={selectedUser} />
        ) : (
          <Box p={2}>Select a user</Box>
        )}
      </Box>

      {/* CREATE REALM DIALOG */}
      <Dialog open={newRealmDialogOpen} onClose={() => setNewRealmDialogOpen(false)}>
        <Box p={2} width={300}>
          <h3>Create new realm</h3>
          <TextField
            fullWidth
            label="Realm name"
            value={newRealmName}
            onChange={(e) => setNewRealmName(e.target.value)}
          />
          <Box mt={2} display="flex" justifyContent="flex-end">
            <Button onClick={() => setNewRealmDialogOpen(false)}>Cancel</Button>
            <Button onClick={handleCreateRealm} variant="contained">
              Create
            </Button>
          </Box>
        </Box>
      </Dialog>
    </Box>
  );
}
