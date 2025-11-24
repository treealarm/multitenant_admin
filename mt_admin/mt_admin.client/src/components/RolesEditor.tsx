/* eslint-disable react-hooks/exhaustive-deps */
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
import { useEffect, useState } from "react";

import {
  fetchRealmRoles,
  fetchUserRoles,
  assignRolesToUser,
} from "../store/rolesSlice";

interface RolesEditorProps {
  realm: string;
  username: string;
}

export function RolesEditor({ realm, username }: RolesEditorProps) {
  const dispatch = useAppDispatch();


  const { realmRoles, userRoles, loading, lastUpdate } = useAppSelector(
    (s) => s.roles
  );


  // загружаем роли при выборе юзера/реалма
  useEffect(() => {
    if (realm && username) {
      dispatch(fetchRealmRoles(realm));
      dispatch(fetchUserRoles({ realm, username }));
    }
  }, [realm, username, lastUpdate]);

  // проверка, есть ли роль в текущем списке
  const isAssigned = (roleName: string) => userRoles?.includes(roleName) ?? false;


  // toggleRole для галочек: обновляем локальный state и сразу отправляем весь массив на backend
  const toggleRole = (roleName: string) => {
    let updatedRoles: string[];

    if (isAssigned(roleName)) {
      updatedRoles = userRoles.filter((r) => r !== roleName); // снимаем галочку
    } else {
      updatedRoles = [...userRoles, roleName]; // добавляем галочку
    }

    dispatch(assignRolesToUser({ realm, username, roles: updatedRoles }));
  };

  // Refresh кнопка: заново загружаем роли с backend
  const refreshRoles = async () => {
    dispatch(fetchRealmRoles(realm));
    dispatch(fetchUserRoles({ realm, username }));
  };

  return (
    <Box height="100%" display="flex" flexDirection="column">
      <Toolbar variant="dense">
        <Tooltip title="Refresh roles">
          <IconButton onClick={refreshRoles}>
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
