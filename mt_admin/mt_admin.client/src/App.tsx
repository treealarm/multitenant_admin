import React, { useState, useEffect } from "react";
import { TextField, Button, Container, Typography, Box } from "@mui/material";
import { useAppDispatch, useAppSelector } from "./store";
import { login, logout, restoreSession } from "./store/authSlice";
import { UsersList } from "./components/UsersList";

export default function App() {
  const dispatch = useAppDispatch();
  const { realm, token, loading, error } = useAppSelector((s) => s.auth);

  const [inputRealm, setInputRealm] = useState(realm ?? "testrealm");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [showUsers, setShowUsers] = useState(false);

  useEffect(() => {
    dispatch(restoreSession());
  }, [dispatch]);

  const handleLogin = () => {
    dispatch(login({ realm: inputRealm, username, password }));
  };

  const handleLogout = () => {
    dispatch(logout());
    setShowUsers(false);
  };

  return (
    <Container sx={{ mt: 6 }}>
      {!token ? (
        <Box sx={{ display: "flex", flexDirection: "column", gap: 2, maxWidth: 400 }}>
          <Typography variant="h5">Login to Keycloak Realm</Typography>
          <TextField label="Realm" value={inputRealm} onChange={(e) => setInputRealm(e.target.value)} />
          <TextField label="Username" value={username} onChange={(e) => setUsername(e.target.value)} />
          <TextField label="Password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} />
          {error && <Typography color="error">{error}</Typography>}
          <Button
            variant="contained"
            onClick={handleLogin}
            disabled={loading || !inputRealm || !username || !password}
          >
            {loading ? "Logging in..." : "Login"}
          </Button>
        </Box>
      ) : (
        <>
          <Typography variant="h6" sx={{ mb: 2 }}>
            Logged in to realm: {realm}
          </Typography>

          <Box sx={{ mb: 2 }}>
            <Button variant="outlined" onClick={handleLogout}>
              Logout
            </Button>
            <Button sx={{ ml: 2 }} variant="contained" onClick={() => setShowUsers(true)}>
              Load Users
            </Button>
          </Box>

          {showUsers && realm && token && <UsersList realm={realm} token={token} />}
        </>
      )}
    </Container>
  );
}
