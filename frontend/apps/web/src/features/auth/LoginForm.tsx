import { useState } from "react"
import { useMutation } from "@tanstack/react-query"
import { AxiosError } from "axios"
import api from "@/lib/api"
import { useAuthStore, type User } from "@/store/useAuthStore"

interface AuthResponse {
  user: User
  // Backend (AuthResponse record) serializes the JWT as `accessToken`.
  accessToken: string
}
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { AlertCircle, ArrowRight } from "lucide-react"

export function LoginForm() {
  const [email, setEmail] = useState("")
  const [password, setPassword] = useState("")
  const setAuth = useAuthStore((state) => state.setAuth)

  const mutation = useMutation({
    mutationFn: async () => {
      const response = await api.post("/auth/login", { email, password })
      return response.data
    },
    onSuccess: (data: AuthResponse) => {
      setAuth(data.user, data.accessToken)
    },
  })

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    mutation.mutate()
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="space-y-2">
        <Label htmlFor="email" className="text-sm font-semibold text-slate-700 ml-1">Email Address</Label>
        <Input
          id="email"
          type="email"
          placeholder="name@company.com"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          className="h-12 bg-slate-50 border-slate-200 focus:border-[#06B6D4] focus:ring-[#06B6D4] rounded-xl transition-all duration-200"
          required
        />
      </div>
      <div className="space-y-2">
        <div className="flex items-center justify-between ml-1">
            <Label htmlFor="password" className="text-sm font-semibold text-slate-700">Password</Label>
            <button type="button" className="text-xs text-[#06B6D4] hover:text-[#0891B2] font-bold transition-colors">Forgot password?</button>
        </div>
        <Input
          id="password"
          type="password"
          placeholder="••••••••"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          className="h-12 bg-slate-50 border-slate-200 focus:border-[#06B6D4] focus:ring-[#06B6D4] rounded-xl transition-all duration-200"
          required
        />
      </div>

      {mutation.isError && (
        <div className="flex items-center gap-3 text-sm text-red-600 bg-red-50 p-4 rounded-xl border border-red-100 animate-in fade-in zoom-in-95 duration-200">
          <AlertCircle className="h-4 w-4 shrink-0" />
          <p className="font-medium">{(mutation.error as AxiosError)?.response?.data as string || "Invalid email or password."}</p>
        </div>
      )}

      <Button 
        type="submit" 
        className="w-full h-12 text-md font-bold bg-[#06B6D4] hover:bg-[#0891B2] text-white rounded-xl shadow-lg shadow-[#06B6D4]/20 transition-all duration-200 group" 
        disabled={mutation.isPending}
      >
        {mutation.isPending ? "Authenticating..." : (
          <span className="flex items-center gap-2">
            Sign In <ArrowRight className="h-4 w-4 group-hover:translate-x-1 transition-transform" />
          </span>
        )}
      </Button>
    </form>
  )
}
