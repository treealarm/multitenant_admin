import { theStore } from "./store";

export async function authFetch(input: RequestInfo, init?: RequestInit): Promise<Response> {
  const state = theStore.getState();
  const token = state.auth.token;

  const headers = new Headers(init?.headers || {});

  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const response = await fetch(input, {
    ...init,
    headers,
  });

  // если токен истЄк Ч можно обработать здесь
  if (response.status === 401) {
    console.warn("Unauthorized Ч возможно, токен истЄк");
    // TODO: dispatch(logout()) или refresh-токен
  }

  return response;
}
