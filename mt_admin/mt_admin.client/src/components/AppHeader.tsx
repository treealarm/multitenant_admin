import React from "react";
import { AppBar, Toolbar, Typography, Button } from "@mui/material";
import { useAppSelector, useAppDispatch } from "../store";
import { logout } from "../store/authSlice";
import { Link as RouterLink } from "react-router-dom";

export function AppHeader() {
  const { token } = useAppSelector((s) => s.auth);
  const dispatch = useAppDispatch();

  const handleLogout = () => {
    dispatch(logout());
  };

  return (
    <AppBar position="static">
      <Toolbar>
        <Typography variant="h6" sx={{ flexGrow: 1 }}>
          MultiTenant Admin
        </Typography>

        {!token && (
          <>
            <Button color="inherit" component={RouterLink} to="/login">
              Login
            </Button>
            <Button color="inherit" component={RouterLink} to="/register">
              Register
            </Button>
          </>
        )}

        {token && (
          <Button color="inherit" onClick={handleLogout}>
            Logout
          </Button>
        )}
      </Toolbar>
    </AppBar>
  );
}
