import React, { useEffect, useState } from "react";
import { loadConfig, type AppConfig } from "./config";
import { StandaloneUsers } from "./components/StandaloneUsers";
import { useAppDispatch, useAppSelector } from "./store";
import { validateToken, logout } from "./store/authSlice";
import { Routes, Route, Navigate, useNavigate, BrowserRouter } from "react-router-dom";
import { createTheme, ThemeProvider, Container } from "@mui/material";
import { AppHeader } from "./components/AppHeader";

import { LoginForm } from "./components/LoginForm";
import { RegisterForm } from "./components/RegisterForm";
import { RealmUsersManager } from "./components/RealmUsersManager";
import { AuthGuard } from "./components/AuthGuard";
import { SnackbarProvider } from "./components/SnackbarProvider";

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

function AppRoutes({ config }: { config: AppConfig }) {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { token } = useAppSelector((s) => s.auth);

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
        element={token
          ? (config.standalone ? <StandaloneUsers /> : <RealmUsersManager />)
          : <Navigate to="/login" replace />}
      />
      {!config.standalone && <Route path="/register" element={<RegisterForm />} />}
      <Route path="/login" element={<LoginForm standalone={config.standalone} />} />
    </Routes>
  );
}

export default function App() {
  const [config, setConfig] = useState<AppConfig | null>(null);
  const [configError, setConfigError] = useState<string | null>(null);
  useEffect(() => {
    loadConfig().then(setConfig, (e: Error) => setConfigError(e.message));
  }, []);

  if (!config) return <Container sx={{ mt: 4 }}>{configError ?? "Loading..."}</Container>;

  return (
    <ThemeProvider theme={theme}>
      <BrowserRouter>
        <AuthGuard /> 
        <SnackbarProvider />
        <AppHeader standalone={config.standalone} />  {/* тулбар будет сверху на всех страницах */}
        <Container sx={{ mt: 4 }}>
          <AppRoutes config={config} />
        </Container>
      </BrowserRouter>
    </ThemeProvider>
  );
}

