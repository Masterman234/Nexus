import { LoginForm } from "./LoginForm";
import { AuthLayout } from "./AuthLayout";
import { Link } from "react-router-dom";

export function LoginPage() {
  return (
    <AuthLayout 
      title="Welcome back" 
      subtitle="Enter your credentials to access your engineering workspace."
    >
      <LoginForm />
      <div className="mt-6 text-center text-sm text-muted-foreground">
        Don't have an account?{" "}
        <Link to="/register" className="text-primary hover:underline font-semibold">
          Create one for free
        </Link>
      </div>
    </AuthLayout>
  );
}
