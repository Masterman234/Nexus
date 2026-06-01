import { useState } from "react"
import { LoginForm } from "@/features/auth/LoginForm"
import { RegisterForm } from "@/features/auth/RegisterForm"
import { useAuthStore } from "@/store/useAuthStore"
import { ChatContainer } from "@/features/chat/ChatContainer"

function App() {
  const { isAuthenticated } = useAuthStore()
  const [isLogin, setIsLogin] = useState(true)

  if (isAuthenticated) {
    return <ChatContainer />
  }

  return (
    <div className="min-h-screen bg-background flex flex-col items-center justify-center p-4">
      <div className="w-full max-w-sm space-y-4">
        <div className="text-center space-y-2 mb-8">
          <h1 className="text-4xl font-bold tracking-tight text-primary">Nexus</h1>
          <p className="text-muted-foreground text-sm font-medium uppercase tracking-widest">Engineering Intelligence</p>
        </div>
        
        {isLogin ? <LoginForm /> : <RegisterForm />}
        
        <div className="text-center text-sm">
          {isLogin ? (
            <p>
              Don't have an account?{" "}
              <button onClick={() => setIsLogin(false)} className="text-primary hover:underline font-bold">
                Register
              </button>
            </p>
          ) : (
            <p>
              Already have an account?{" "}
              <button onClick={() => setIsLogin(true)} className="text-primary hover:underline font-bold">
                Login
              </button>
            </p>
          )}
        </div>
      </div>
    </div>
  )
}

export default App
