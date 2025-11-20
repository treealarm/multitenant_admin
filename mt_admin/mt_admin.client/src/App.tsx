import React, { useEffect } from "react";
import { useAppDispatch, useAppSelector } from "./store";
import { validateToken, logout } from "./store/authSlice";
import { Routes, Route, Navigate, useNavigate, BrowserRouter } from "react-router-dom";
import { createTheme, ThemeProvider, Container } from "@mui/material";
import { AppHeader } from "./components/AppHeader";

import { LoginForm } from "./components/LoginForm";
import { RegisterForm } from "./components/RegisterForm";
import { UsersList } from "./components/UsersList";

// Настройка темы MUI
const theme = createTheme({
  spacing: 3,
  typography: {
    button: { textTransform: "none" },
  },
  palette: {
    mode: "light",
    primary: { main: "#3f51b5" },
    secondary: { main: "#f50057" },
  },
});

function AppRoutes() {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { token, realm } = useAppSelector((s) => s.auth);

  useEffect(() => {
    if (token) {
      dispatch(validateToken())
        .unwrap()
        .catch(() => {
          dispatch(logout());
          navigate("/login");
        });
    }
  }, [token, dispatch, navigate]);

  return (
    <Routes>
      <Route
        path="/"
        element={token && realm ? <UsersList realm={realm} /> : <Navigate to="/auth" replace />}
      />
      <Route path="/register" element={<RegisterForm />} />
      <Route path="/login" element={<LoginForm />} />
    </Routes>
  );
}

export default function App() {
  return (
    <ThemeProvider theme={theme}>
      <BrowserRouter>
        <AppHeader />  {/* тулбар будет сверху на всех страницах */}
        <Container sx={{ mt: 4 }}>
          <AppRoutes />
        </Container>
      </BrowserRouter>
    </ThemeProvider>
  );
}

