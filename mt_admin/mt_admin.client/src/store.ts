// store.ts
import { configureStore } from '@reduxjs/toolkit';
import { combineReducers } from 'redux';
import { useDispatch, useSelector, type TypedUseSelectorHook } from 'react-redux';
// Тут импортируешь свои редьюсеры
// Например, usersReducer для списка пользователей
import usersReducer from './store/usersSlice';
import authReducer from "./store/authSlice";
import curUserReducer from "./store/currentUserSlice";
import rolesReducer from "./store/rolesSlice";
import dbReducer from "./store/dbSlice";

export const reducers = combineReducers({
  users: usersReducer,
  auth: authReducer,
  curUser: curUserReducer,
  roles: rolesReducer,
  db: dbReducer
  // можно добавить другие редьюсеры
});

// Создание стора
export const theStore = configureStore({
  reducer: reducers,
});

// TypeScript типы (только для TS, не экспортируй их как обычный JS)
export type RootState = ReturnType<typeof theStore.getState>;
export type AppDispatch = typeof theStore.dispatch;

// хуки для TS
export const useAppDispatch: () => AppDispatch = useDispatch;
export const useAppSelector: TypedUseSelectorHook<RootState> = useSelector;
