import { useCallback, useEffect, useState } from "react";
import {
  Alert, Box, Button, Checkbox, Dialog, DialogActions, DialogContent, DialogTitle,
  FormControlLabel, IconButton, Paper, Switch, Table, TableBody, TableCell, TableHead, TableRow,
  TextField, Tooltip, Typography,
} from "@mui/material";
import DeleteIcon from "@mui/icons-material/Delete";
import LockResetIcon from "@mui/icons-material/LockReset";
import PersonAddIcon from "@mui/icons-material/PersonAdd";
import { authFetch } from "../authFetch";

interface User {
  username: string;
  isAdmin: boolean;
}

// The server's rule (UsersController.MinPasswordLength), checked here first.
const MIN_PASSWORD_LENGTH = 8;

async function call(method: string, url: string, body?: unknown): Promise<void> {
  const res = await authFetch(url, {
    method,
    headers: body === undefined ? undefined : { "Content-Type": "application/json" },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (!res.ok) {
    const text = await res.text();
    let message = text || `${method} ${url}: ${res.status}`;
    try { message = (JSON.parse(text) as { error?: string }).error ?? message; } catch { /* plain text */ }
    throw new Error(message);
  }
}

/**
 * The users of a standalone TreeAlarm VMS installation. Which cameras a non-administrator sees is
 * decided in TreeAlarm VMS, on its Permissions page.
 */
export function StandaloneUsers() {
  const [users, setUsers] = useState<User[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [adding, setAdding] = useState(false);
  const [resetting, setResetting] = useState<string | null>(null);
  const [deleting, setDeleting] = useState<string | null>(null);

  const reload = useCallback(async () => {
    try {
      const res = await authFetch("/api/users");
      if (!res.ok) throw new Error(`GET /api/users: ${res.status}`);
      setUsers((await res.json()) as User[]);
    } catch (e) {
      setError((e as Error).message);
    }
  }, []);

  useEffect(() => { void reload(); }, [reload]);

  const act = async (action: () => Promise<void>) => {
    setError(null);
    try {
      await action();
      await reload();
      return true;
    } catch (e) {
      setError((e as Error).message);
      return false;
    }
  };

  return (
    <Paper sx={{ p: 4 }}>
      <Box display="flex" alignItems="center" mb={3}>
        <Typography variant="h6" sx={{ flexGrow: 1 }}>Users</Typography>
        <Button startIcon={<PersonAddIcon />} variant="contained" onClick={() => setAdding(true)}>Add user</Button>
      </Box>
      {error && <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError(null)}>{error}</Alert>}
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>User</TableCell>
            <TableCell>Administrator</TableCell>
            <TableCell align="right" />
          </TableRow>
        </TableHead>
        <TableBody>
          {users.map((u) => (
            <TableRow key={u.username}>
              <TableCell>{u.username}</TableCell>
              <TableCell>
                <Switch
                  checked={u.isAdmin}
                  onChange={(e) => void act(() => call("PUT", `/api/users/${encodeURIComponent(u.username)}/admin`, { isAdmin: e.target.checked }))}
                />
              </TableCell>
              <TableCell align="right">
                <Tooltip title="Reset password">
                  <IconButton onClick={() => setResetting(u.username)}><LockResetIcon /></IconButton>
                </Tooltip>
                <Tooltip title="Delete">
                  <IconButton onClick={() => setDeleting(u.username)}><DeleteIcon /></IconButton>
                </Tooltip>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>

      <AddUserDialog
        open={adding}
        onClose={() => setAdding(false)}
        onSubmit={(username, password, isAdmin) =>
          act(() => call("POST", "/api/users", { username, password, isAdmin })).then((ok) => ok && setAdding(false))}
      />
      <PasswordDialog
        username={resetting}
        onClose={() => setResetting(null)}
        onSubmit={(password) =>
          act(() => call("POST", `/api/users/${encodeURIComponent(resetting!)}/password`, { password })).then((ok) => ok && setResetting(null))}
      />
      <Dialog open={deleting !== null} onClose={() => setDeleting(null)}>
        <DialogTitle>Delete {deleting}?</DialogTitle>
        <DialogActions>
          <Button onClick={() => setDeleting(null)}>Cancel</Button>
          <Button color="error" onClick={() => void act(() => call("DELETE", `/api/users/${encodeURIComponent(deleting!)}`)).then(() => setDeleting(null))}>
            Delete
          </Button>
        </DialogActions>
      </Dialog>
    </Paper>
  );
}

function passwordProblem(password: string, username: string): string | null {
  if (password.length < MIN_PASSWORD_LENGTH) return `At least ${MIN_PASSWORD_LENGTH} characters`;
  if (password.toLowerCase() === username.toLowerCase()) return "Must differ from the username";
  return null;
}

function AddUserDialog({ open, onClose, onSubmit }: {
  open: boolean;
  onClose: () => void;
  onSubmit: (username: string, password: string, isAdmin: boolean) => void;
}) {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [isAdmin, setIsAdmin] = useState(false);
  useEffect(() => { if (open) { setUsername(""); setPassword(""); setIsAdmin(false); } }, [open]);
  const problem = password ? passwordProblem(password, username) : null;

  return (
    <Dialog open={open} onClose={onClose}>
      <DialogTitle>Add user</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 3, pt: "12px !important", minWidth: 360 }}>
        <TextField label="Username" value={username} onChange={(e) => setUsername(e.target.value)} autoFocus />
        <TextField label="Password" type="password" value={password} onChange={(e) => setPassword(e.target.value)}
          error={problem !== null} helperText={problem ?? " "} />
        <FormControlLabel control={<Checkbox checked={isAdmin} onChange={(e) => setIsAdmin(e.target.checked)} />} label="Administrator" />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" disabled={!username.trim() || !password || problem !== null}
          onClick={() => onSubmit(username.trim(), password, isAdmin)}>Add</Button>
      </DialogActions>
    </Dialog>
  );
}

function PasswordDialog({ username, onClose, onSubmit }: {
  username: string | null;
  onClose: () => void;
  onSubmit: (password: string) => void;
}) {
  const [password, setPassword] = useState("");
  useEffect(() => { if (username) setPassword(""); }, [username]);
  const problem = password && username ? passwordProblem(password, username) : null;

  return (
    <Dialog open={username !== null} onClose={onClose}>
      <DialogTitle>Reset the password of {username}</DialogTitle>
      <DialogContent sx={{ pt: "12px !important", minWidth: 360 }}>
        <TextField fullWidth label="New password" type="password" value={password} onChange={(e) => setPassword(e.target.value)}
          error={problem !== null} helperText={problem ?? " "} autoFocus />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" disabled={!password || problem !== null} onClick={() => onSubmit(password)}>Reset</Button>
      </DialogActions>
    </Dialog>
  );
}
