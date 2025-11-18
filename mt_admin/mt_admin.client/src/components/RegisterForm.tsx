// components/RegisterForm.tsx
import { useState } from "react";
import { useAppDispatch, useAppSelector } from "../store";
import { registerUser } from "../store/usersSlice";
import { Button, TextField, Container, Alert } from "@mui/material";
import { useNavigate } from "react-router-dom";

export function RegisterForm() {
  const dispatch = useAppDispatch();
  const { loading, error } = useAppSelector((s) => s.users);
  const navigate = useNavigate();

  const [username, setUsername] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const handleSubmit = async () => {
    try {
      await dispatch(registerUser({ username, email, password })).unwrap();
      alert("Registration successful!");
      navigate("/login");
    } catch (e) {
      console.error(e);
    }
  };

  return (
    <Container sx={{ mt: 4, maxWidth: 400 }}>
      <h2>Register</h2>

      {error && <Alert severity="error">{error}</Alert>}

      <TextField
        fullWidth
        label="Username"
        value={username}
        onChange={(e) => setUsername(e.target.value)}
        sx={{ mt: 2 }}
      />

      <TextField
        fullWidth
        label="Email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        sx={{ mt: 2 }}
      />

      <TextField
        fullWidth
        label="Password"
        type="password"
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        sx={{ mt: 2 }}
      />

      <Button
        variant="contained"
        fullWidth
        onClick={handleSubmit}
        sx={{ mt: 3 }}
        disabled={loading}
      >
        Register
      </Button>
    </Container>
  );
}
