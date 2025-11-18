import { Button, Container } from "@mui/material";
import { Link as RouterLink } from "react-router-dom";

export function AuthPage() {
  return (
    <Container sx={{ mt: 4 }}>
      <h2>Welcome</h2>
      <p>Please login or register to continue:</p>
      <Button variant="contained" component={RouterLink} to="/login" sx={{ mr: 2 }}>
        Login
      </Button>
      <Button variant="outlined" component={RouterLink} to="/register">
        Register
      </Button>
    </Container>
  );
}
