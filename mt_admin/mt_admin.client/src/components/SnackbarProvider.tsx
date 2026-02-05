import { Snackbar, Alert } from "@mui/material";
import { useAppSelector, useAppDispatch } from "../store";
import { resetProvision } from "../store/provisionSlice";
import { clearRealmOp } from "../store/currentUserSlice";

export function SnackbarProvider() {
  const dispatch = useAppDispatch();

  const provision = useAppSelector((s) => s.provision);
  const realmOp = useAppSelector((s) => s.curUser.realmOp);

  return (
    <>
      {/* ---------------- Provision Snackbar ---------------- */}
      {provision.error ? (
        <Snackbar
          open={!!provision.error}
          autoHideDuration={4000}
          onClose={() => dispatch(resetProvision())}
        >
          <Alert severity="error">{provision.error}</Alert>
        </Snackbar>
      ) : provision.result ? (
        <Snackbar
          open={!!provision.result}
          autoHideDuration={3000}
            onClose={() => dispatch(resetProvision())}
        >
          <Alert severity="success">{provision.result}</Alert>
        </Snackbar>
      ) : null}

      {/* ---------------- Realm Snackbar ---------------- */}
      {realmOp?.error ? (
        <Snackbar
          open={!!realmOp.error}
          autoHideDuration={4000}
          onClose={() => dispatch(clearRealmOp())}
        >
          <Alert severity="error">{realmOp.error}</Alert>
        </Snackbar>
      ) : realmOp?.result ? (
        <Snackbar
          open={!!realmOp.result}
          autoHideDuration={3000}
          onClose={() => dispatch(clearRealmOp())}
        >
          <Alert severity="success">{realmOp.result}</Alert>
        </Snackbar>
      ) : null}
    </>
  );
}
