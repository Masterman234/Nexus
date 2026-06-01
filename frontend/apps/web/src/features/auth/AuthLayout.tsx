import { Link } from "react-router-dom";

interface AuthLayoutProps {
  children: React.ReactNode;
  title: string;
  subtitle: string;
}

export function AuthLayout({ children, title, subtitle }: AuthLayoutProps) {
  return (
    <div className="min-h-screen grid grid-cols-1 lg:grid-cols-2 overflow-hidden bg-[#F8FAFC]">
      {/* BRAND SIDE (Left) */}
      <div className="hidden lg:flex flex-col justify-between p-12 bg-[#0F172A] relative overflow-hidden">
        {/* Background Illustration with Opacity Blending */}
        <div className="absolute inset-0 z-0 pointer-events-none flex items-center justify-end pr-[-10%]">
            <img 
              src="/nexus-banner.png" 
              alt="Nexus Illustration" 
              className="w-[120%] h-auto object-contain opacity-20 transform translate-x-[15%]" 
            />
            {/* Subtle Glow Effects */}
            <div className="absolute top-1/4 right-0 w-[400px] h-[400px] bg-[#06B6D4]/10 rounded-full blur-[120px]" />
            <div className="absolute bottom-1/4 left-1/4 w-[300px] h-[300px] bg-[#06B6D4]/5 rounded-full blur-[100px]" />
        </div>

        {/* Top Logo */}
        <Link to="/" className="flex items-center gap-2 relative z-10 w-fit group">
          <div className="h-10 w-10 bg-[#06B6D4] flex items-center justify-center rounded-lg shadow-xl shadow-[#06B6D4]/20 text-white font-bold text-xl group-hover:scale-105 transition-transform duration-300">N</div>
          <span className="font-bold text-2xl tracking-tight text-white">Nexus</span>
        </Link>

        {/* Headline Content */}
        <div className="relative z-10 max-w-lg mb-20">
          <h1 className="text-6xl font-bold leading-[1.1] mb-8 tracking-tight">
            <span className="text-white block">Build smarter,</span>
            <span className="text-slate-400 block">not harder.</span>
          </h1>
          <p className="text-slate-300 text-xl leading-relaxed font-medium max-w-[90%]">
            Stop switching between tabs. Nexus brings your code, team, and AI into one unified workspace.
          </p>
        </div>

        {/* Social Proof / Footer */}
        <div className="relative z-10 flex items-center gap-4">
            <div className="flex -space-x-2">
                {[1, 2, 3, 4].map((i) => (
                    <div key={i} className="h-8 w-8 rounded-full border-2 border-[#0F172A] bg-slate-800 flex items-center justify-center text-[10px] font-bold text-white">
                        {String.fromCharCode(64 + i)}
                    </div>
                ))}
            </div>
            <p className="text-xs text-slate-500 font-semibold uppercase tracking-widest">
                Trusted by 10k+ Engineers
            </p>
        </div>
      </div>

      {/* FORM SIDE (Right) */}
      <div className="flex flex-col items-center justify-center p-8 bg-[#F8FAFC] relative">
        {/* Mobile Logo */}
        <div className="lg:hidden absolute top-8 left-8">
            <Link to="/" className="flex items-center gap-2">
                <div className="h-8 w-8 bg-[#06B6D4] flex items-center justify-center rounded text-white font-bold">N</div>
                <span className="font-bold text-xl tracking-tight text-[#0F172A]">Nexus</span>
            </Link>
        </div>

        <div className="w-full max-w-[420px] space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-700">
          <div className="space-y-2 text-center lg:text-left">
            <h2 className="text-4xl font-bold tracking-tight text-[#0F172A]">{title}</h2>
            <p className="text-slate-500 text-lg font-medium">{subtitle}</p>
          </div>
          
          <div className="bg-white border border-slate-200 rounded-3xl p-10 shadow-[0_8px_30px_rgb(0,0,0,0.04)]">
            {children}
          </div>
        </div>
      </div>
    </div>
  );
}
