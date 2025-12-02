import { Snackbar, Alert } from "@mui/material";
import { useAppSelector, useAppDispatch } from "../store";
import { clearDbState } from "../store/dbSlice";
import { clearRealmOp } from "../store/currentUserSlice";

export function SnackbarProvider() {
  const dispatch = useAppDispatch();

  const dbState = useAppSelector((s) => s.db);
  const realmOp = useAppSelector((s) => s.curUser.realmOp);

  return (
    <>
      {/* ---------------- DB Snackbar ---------------- */}
      <Snackbar
        open={!!dbState.error}
        autoHideDuration={4000}
        onClose={() => dispatch(clearDbState())}
      >
        <Alert severity="error">{dbState.error}</Alert>
      </Snackbar>

      <Snackbar
        open={!!dbState.result}
        autoHideDuration={3000}
        onClose={() => dispatch(clearDbState())}
      >
        <Alert severity="success">{dbState.result}</Alert>
      </Snackbar>

      {/* ---------------- Realm Snackbar ---------------- */}
      <Snackbar
        open={!!realmOp?.error}
        autoHideDuration={4000}
        onClose={() => dispatch(clearRealmOp())}
      >
        <Alert severity="error">{realmOp?.error}</Alert>
      </Snackbar>

      <Snackbar
        open={!!realmOp?.result}
        autoHideDuration={3000}
        onClose={() => dispatch(clearRealmOp())}
      >
        <Alert severity="success">{realmOp?.result}</Alert>
      </Snackbar>
    </>
  );
}
