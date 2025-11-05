import { useState } from "react";
import { TextField, Button, Container } from "@mui/material";
import { UsersList } from "./components/UsersList";
import React from "react";

export default function App() {
  const [realm, setRealm] = useState("testrealm");
  const [showUsers, setShowUsers] = useState(false);

  return (
    <Container sx={{ mt: 4 }}>
      <TextField
        label="Realm"
        value={realm}
        onChange={(e) => setRealm(e.target.value)}
      />
      <Button
        sx={{ ml: 2 }}
        variant="contained"
        onClick={() => setShowUsers(true)}
      >
        Load Users
      </Button>

      {showUsers && <UsersList realm={realm} />}
    </Container>
  );
}
