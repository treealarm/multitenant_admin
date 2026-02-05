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

import { useEffect, useState } from "react";
import { useAppDispatch, useAppSelector } from "../store";

import { UsersList } from "./UsersList";
import { RolesEditor } from "./RolesEditor";

import { createRealm, deleteRealm, resetProvision } from "../store/provisionSlice";
import { fetchLoggedInUser } from "../store/currentUserSlice";

export function RealmUsersManager() {
  const dispatch = useAppDispatch();
  const { user } = useAppSelector((s) => s.curUser);
  const provisionLoading = useAppSelector((s) => s.provision.loading);

  const [selectedRealm, setSelectedRealm] = useState<string | null>(null);
  const [selectedUser, setSelectedUser] = useState<string | null>(null);

  const [newRealmDialogOpen, setNewRealmDialogOpen] = useState(false);
  const [newRealmName, setNewRealmName] = useState("");

  const realmsOwned = user?.attributes?.realmsOwned ?? [];

  // --- Fetch logged in user ---
  useEffect(() => {
    dispatch(fetchLoggedInUser());
  }, [dispatch]);

  // --- Refresh user info after realm ops ---
  useEffect(() => {
    if (!provisionLoading) {
      dispatch(fetchLoggedInUser());
    }
  }, [dispatch, provisionLoading]);

  // --- Create Realm (Provision: Keycloak + DB + K8s) ---
  const handleCreateRealm = () => {
    if (!newRealmName) return;

    dispatch(resetProvision()); // очистка прошлых ошибок / статусов
    dispatch(createRealm(newRealmName));
    setNewRealmName("");
    setNewRealmDialogOpen(false);
  };

  // --- Delete Realm (ProvisionController удал€ет всЄ сразу) ---
  const handleDeleteRealm = () => {
    if (!selectedRealm) return;
    if (!confirm(`Delete realm '${selectedRealm}'?`)) return;

    dispatch(resetProvision());
    dispatch(deleteRealm(selectedRealm));
    setSelectedRealm(null);
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
            <IconButton
              disabled={provisionLoading}
              onClick={() => setNewRealmDialogOpen(true)}
            >
              <AddIcon />
            </IconButton>
          </Tooltip>

          <Tooltip title="Delete Realm">
            <IconButton
              color="error"
              disabled={!selectedRealm || provisionLoading}
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
            <Button onClick={handleCreateRealm} variant="contained" disabled={provisionLoading}>
              Create
            </Button>
          </Box>
        </Box>
      </Dialog>
    </Box>
  );
}
