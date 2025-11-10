import { useState } from "react";
import { TextField, Button, Box } from "@mui/material";
import { useAppDispatch } from "../store";
import { login } from "../store/authSlice";

export  function LoginForm() {
  const dispatch = useAppDispatch();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [realm, setRealm] = useState("");
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async () => {
    try {
      const resultAction = await dispatch(
        login({ username, password, realm })
      );

      if (login.rejected.match(resultAction)) {
        setError(resultAction.payload as string);
      } else {
        setError(null);
      }
    } catch (err: any) {
      setError(err.message);
    }
  };

  return (
    <Box display="flex" flexDirection="column" gap={2} maxWidth={400}>
      <TextField
        label="Realm"
        value={realm}
        onChange={(e) => setRealm(e.target.value)}
      />
      <TextField
        label="Username"
        value={username}
        onChange={(e) => setUsername(e.target.value)}
      />
      <TextField
        label="Password"
        type="password"
        value={password}
        onChange={(e) => setPassword(e.target.value)}
      />
      {error && <div style={{ color: "red" }}>{error}</div>}
      <Button variant="contained" onClick={handleSubmit}>
        Login
      </Button>
    </Box>
  );
}
