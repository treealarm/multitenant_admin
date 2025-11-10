import { useAppSelector } from "./store";
import { useState } from "react";
import { LoginForm } from "./components/LoginForm";
import { UsersList } from "./components/UsersList";
import { Container } from "@mui/material";

export default function App() {
  const { token, realm } = useAppSelector((s) => s.auth);

  // Если токена нет или realm пустой — показываем логин
  if (!token || !realm) {
    return (
      <Container sx={{ mt: 4 }}>
        <LoginForm />
      </Container>
    );
  }

  return (
    <Container sx={{ mt: 4 }}>
      <UsersList realm={realm} />
    </Container>
  );
}
