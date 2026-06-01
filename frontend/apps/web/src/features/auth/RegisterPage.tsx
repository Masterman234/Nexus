import { RegisterForm } from "./RegisterForm";
import { AuthLayout } from "./AuthLayout";
import { Link } from "react-router-dom";

export function RegisterPage() {
  return (
    <AuthLayout 
      title="Create an account" 
      subtitle="Join Nexus and start unifying your engineering signals today."
    >
      <RegisterForm />
      <div className="mt-6 text-center text-sm text-muted-foreground">
        Already have an account?{" "}
        <Link to="/login" className="text-primary hover:underline font-semibold">
          Sign in here
        </Link>
      </div>
    </AuthLayout>
  );
}
