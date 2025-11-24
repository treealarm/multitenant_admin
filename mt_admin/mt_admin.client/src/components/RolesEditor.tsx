import {
  Box,
  Checkbox,
  CircularProgress,
  FormControlLabel,
  IconButton,
  List,
  ListItem,
  Toolbar,
  Tooltip,
} from "@mui/material";
import RefreshIcon from "@mui/icons-material/Refresh";
import { useAppDispatch, useAppSelector } from "../store";
import { useEffect } from "react";

import {
  fetchRealmRoles,
  fetchUserRoles,
  assignRole,
  removeRole,
} from "../store/rolesSlice"; // поправь путь под свой

interface RolesEditorProps {
  realm: string;
  username: string;
}

export function RolesEditor({ realm, username }: RolesEditorProps) {
  const dispatch = useAppDispatch();

  const { realmRoles, userRoles, loading } = useAppSelector((s) => s.roles);

  // загружаем роли при выборе юзера/реалма
  useEffect(() => {
    if (realm && username) {
      dispatch(fetchRealmRoles(realm));
      dispatch(fetchUserRoles({ realm, username }));
    }
  }, [realm, username, dispatch]);

  const isAssigned = (roleName: string) => userRoles.includes(roleName);

  const toggleRole = async (roleName: string) => {
    if (isAssigned(roleName)) {
      await dispatch(removeRole({ realm, username, roleName })).unwrap();
    } else {
      await dispatch(assignRole({ realm, username, roleName })).unwrap();
    }

    dispatch(fetchUserRoles({ realm, username }));
  };

  return (
    <Box height="100%" display="flex" flexDirection="column">
      <Toolbar variant="dense">
        <Tooltip title="Refresh roles">
          <IconButton
            onClick={() => {
              dispatch(fetchRealmRoles(realm));
              dispatch(fetchUserRoles({ realm, username }));
            }}
          >
            <RefreshIcon />
          </IconButton>
        </Tooltip>
      </Toolbar>

      {loading ? (
        <Box p={2} display="flex" justifyContent="center">
          <CircularProgress />
        </Box>
      ) : (
        <List dense>
          {realmRoles.map((role) => (
            <ListItem key={role}>
              <FormControlLabel
                control={
                  <Checkbox
                    checked={isAssigned(role)}
                    onChange={() => toggleRole(role)}
                  />
                }
                label={role}
              />
            </ListItem>
          ))}
        </List>
      )}
    </Box>
  );
}
