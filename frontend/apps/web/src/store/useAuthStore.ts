import { create } from "zustand"
import { persist } from "zustand/middleware"

interface User {
  id: string
  email: string
  username: string
}

interface AuthState {
  user: User | null
  token: string | null
  isAuthenticated: boolean
  setAuth: (user: User, token: string) => void
  logout: () => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      token: null,
      isAuthenticated: false,
      setAuth: (user, token) => {
        localStorage.setItem("auth-token", token)
        set({ user, token, isAuthenticated: true })
      },
      logout: () => {
        localStorage.removeItem("auth-token")
        set({ user: null, token: null, isAuthenticated: false })
      },
    }),
    {
      name: "auth-storage",
    }
  )
)
