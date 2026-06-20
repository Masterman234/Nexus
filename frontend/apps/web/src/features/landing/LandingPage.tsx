import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { DemoActions } from "./DemoActions";
import { 
  Activity, 
  MessageSquare, 
  Zap, 
  Cpu, 
  ShieldCheck, 
  Code,
  ArrowRight
} from "lucide-react";

export function LandingPage() {
  return (
    <div className="flex flex-col min-h-screen bg-[#0F172A] text-white selection:bg-[#06B6D4]/30">
      {/* Header */}
      <header className="px-6 lg:px-10 h-20 flex items-center border-b border-[#334155]/50 sticky top-0 bg-[#0F172A]/80 backdrop-blur-xl z-50">
        <Link className="flex items-center justify-center gap-3 group" to="/">
          <div className="h-10 w-10 bg-[#06B6D4] flex items-center justify-center rounded-xl shadow-lg shadow-[#06B6D4]/20 text-white font-bold text-xl group-hover:scale-110 transition-transform duration-300">N</div>
          <span className="font-bold text-2xl tracking-tighter">Nexus</span>
        </Link>
        <nav className="ml-auto flex gap-8 items-center">
          <Link className="text-sm font-semibold text-slate-400 hover:text-[#06B6D4] transition-colors hidden md:block" to="#features">
            Features
          </Link>
          <Link className="text-sm font-semibold text-slate-400 hover:text-[#06B6D4] transition-colors hidden md:block" to="#platform">
            Platform
          </Link>
          <div className="h-4 w-px bg-[#334155] hidden md:block" />
          <Link className="text-sm font-semibold text-slate-400 hover:text-white transition-colors" to="/login">
            Sign In
          </Link>
          <Button asChild className="bg-[#06B6D4] hover:bg-[#0891B2] text-white font-bold rounded-xl px-6">
            <Link to="/register">Get Started</Link>
          </Button>
        </nav>
      </header>

      <main className="flex-1">
        {/* Hero Section */}
        <section className="w-full py-20 lg:py-32 relative overflow-hidden">
          {/* Background Grid & Glow */}
          <div className="absolute inset-0 bg-[url('https://grid.layout.dev/grid.svg')] bg-center [mask-image:radial-gradient(ellipse_at_center,transparent_20%,black)] opacity-20 -z-10" />
          <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[800px] h-[500px] bg-[#06B6D4]/10 rounded-full blur-[120px] -z-10" />
          
          <div className="container px-4 md:px-6 mx-auto relative">
            <div className="flex flex-col items-center space-y-8 text-center max-w-4xl mx-auto">
              <div className="inline-flex items-center rounded-full px-4 py-1.5 text-xs font-bold uppercase tracking-widest bg-[#06B6D4]/10 text-[#06B6D4] border border-[#06B6D4]/20 animate-in fade-in slide-in-from-bottom-4 duration-1000">
                <Zap className="h-3.5 w-3.5 mr-2 fill-[#06B6D4]" />
                <span>Now Powered by Gemini 1.5 Pro</span>
              </div>
              
              <h1 className="text-5xl font-extrabold tracking-tight sm:text-6xl md:text-7xl lg:text-8xl/none text-white animate-in fade-in slide-in-from-bottom-6 duration-1000 delay-200">
                Engineering Intelligence <br />
                <span className="text-[#06B6D4]">at Scale.</span>
              </h1>
              
              <p className="max-w-[750px] text-slate-400 text-lg md:text-xl leading-relaxed animate-in fade-in slide-in-from-bottom-8 duration-1000 delay-300 font-medium">
                Nexus is the command center for modern engineering teams. We unify your chat, code, and CI/CD into an AI-native workspace designed for high-velocity teams.
              </p>
              
              <div className="flex flex-col sm:flex-row gap-5 pt-4 animate-in fade-in slide-in-from-bottom-10 duration-1000 delay-500">
                <Button asChild size="lg" className="bg-[#06B6D4] hover:bg-[#0891B2] text-white font-bold rounded-2xl px-10 h-14 text-lg shadow-2xl shadow-[#06B6D4]/20 group">
                  <Link to="/register" className="flex items-center gap-2">
                    Build Your Workspace <ArrowRight className="h-5 w-5 group-hover:translate-x-1 transition-transform" />
                  </Link>
                </Button>
                <DemoActions />
              </div>

              {/* Illustration Placeholder */}
              <div className="w-full mt-20 relative animate-in fade-in zoom-in-95 duration-1000 delay-700">
                <div className="absolute -inset-0.5 bg-gradient-to-r from-[#06B6D4] to-blue-600 rounded-3xl blur opacity-20" />
                <div className="relative bg-[#1E293B] rounded-3xl border border-[#334155] p-2 overflow-hidden shadow-2xl">
                    <img 
                      src="/nexus-banner.png" 
                      alt="Nexus Dashboard Preview" 
                      className="w-full h-auto rounded-2xl grayscale hover:grayscale-0 transition-all duration-700 opacity-80" 
                    />
                </div>
              </div>
            </div>
          </div>
        </section>

        {/* Feature Grid */}
        <section id="features" className="w-full py-24 lg:py-40 bg-[#0F172A] border-t border-[#334155]/30">
          <div className="container px-4 md:px-6 mx-auto">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-12 max-w-6xl mx-auto">
              <div className="flex flex-col space-y-4 p-8 rounded-3xl border border-[#334155]/50 bg-[#1E293B]/30 hover:border-[#06B6D4]/50 transition-colors group">
                <div className="h-12 w-12 bg-[#06B6D4]/10 text-[#06B6D4] rounded-2xl flex items-center justify-center group-hover:scale-110 transition-transform">
                  <MessageSquare className="h-6 w-6" />
                </div>
                <h3 className="text-2xl font-bold">Collaborative Context</h3>
                <p className="text-slate-400 leading-relaxed font-medium">
                  Chat that understands your codebase. Directly reference pull requests, issues, and deployments without leaving the conversation.
                </p>
              </div>
              
              <div className="flex flex-col space-y-4 p-8 rounded-3xl border border-[#06B6D4]/30 bg-[#06B6D4]/5 hover:border-[#06B6D4]/50 transition-colors group relative overflow-hidden">
                <div className="absolute top-0 right-0 p-4">
                    <div className="h-2 w-2 rounded-full bg-[#06B6D4] animate-ping" />
                </div>
                <div className="h-12 w-12 bg-[#06B6D4] text-white rounded-2xl flex items-center justify-center group-hover:scale-110 transition-transform">
                  <Activity className="h-6 w-6" />
                </div>
                <h3 className="text-2xl font-bold">The Engineering Spine</h3>
                <p className="text-slate-400 leading-relaxed font-medium">
                  A high-density event timeline that tracks every signal from GitHub, Linear, and your CI/CD pipelines in real-time.
                </p>
              </div>

              <div className="flex flex-col space-y-4 p-8 rounded-3xl border border-[#334155]/50 bg-[#1E293B]/30 hover:border-[#06B6D4]/50 transition-colors group">
                <div className="h-12 w-12 bg-[#06B6D4]/10 text-[#06B6D4] rounded-2xl flex items-center justify-center group-hover:scale-110 transition-transform">
                  <Cpu className="h-6 w-6" />
                </div>
                <h3 className="text-2xl font-bold">AI Reasoning</h3>
                <p className="text-slate-400 leading-relaxed font-medium">
                  Autonomous agents that draft standups, detect architectural drift, and summarize cross-functional activity.
                </p>
              </div>
            </div>
          </div>
        </section>

        {/* Technical Ingestion Section */}
        <section id="platform" className="w-full py-24 lg:py-40 bg-[#1E293B]/20 relative">
          <div className="container px-4 md:px-6 mx-auto">
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-20 items-center max-w-6xl mx-auto">
              <div className="space-y-8">
                <div className="inline-flex items-center rounded-md px-3 py-1 text-[10px] font-bold uppercase tracking-[0.2em] bg-white/5 text-slate-400 border border-[#334155]">
                  Architecture
                </div>
                <h2 className="text-4xl font-bold tracking-tight sm:text-6xl text-white">Event-Driven Intelligence.</h2>
                <p className="text-slate-400 text-xl leading-relaxed font-medium">
                  Nexus isn't just another dashboard. It's an event processor that ingests HMAC-verified signals and distributes them via a RabbitMQ spine.
                </p>
                
                <div className="space-y-6">
                    <div className="flex items-start gap-4">
                        <div className="mt-1 h-6 w-6 rounded-full bg-[#06B6D4]/10 flex items-center justify-center shrink-0">
                            <ShieldCheck className="h-4 w-4 text-[#06B6D4]" />
                        </div>
                        <div>
                            <h4 className="font-bold text-white">End-to-End Verification</h4>
                            <p className="text-slate-500 text-sm">Every incoming signal is cryptographically verified at the edge.</p>
                        </div>
                    </div>
                    <div className="flex items-start gap-4">
                        <div className="mt-1 h-6 w-6 rounded-full bg-[#06B6D4]/10 flex items-center justify-center shrink-0">
                            <Code className="h-4 w-4 text-[#06B6D4]" />
                        </div>
                        <div>
                            <h4 className="font-bold text-white">Typed Contracts</h4>
                            <p className="text-slate-500 text-sm">Strict domain entities ensure your engineering data is always queryable.</p>
                        </div>
                    </div>
                </div>
              </div>

              <div className="relative group">
                <div className="absolute -inset-2 bg-gradient-to-r from-[#06B6D4] to-blue-600 rounded-[2rem] blur opacity-20 group-hover:opacity-40 transition duration-1000" />
                <div className="relative bg-[#0F172A] rounded-[2rem] border border-[#334155] p-8 shadow-2xl overflow-hidden">
                  <div className="flex items-center gap-2 mb-8 border-b border-[#334155] pb-4">
                    <div className="h-3 w-3 rounded-full bg-red-500/50" />
                    <div className="h-3 w-3 rounded-full bg-yellow-500/50" />
                    <div className="h-3 w-3 rounded-full bg-green-500/50" />
                    <span className="text-[10px] font-mono text-slate-600 ml-4">webhook_processor.go</span>
                  </div>
                  <pre className="text-[#06B6D4] font-mono text-sm leading-relaxed overflow-x-auto whitespace-pre-wrap">
                    {`func HandleEvent(payload []byte) {
  // 1. Verify HMAC Signature
  if !Verify(payload, secret) { return }

  // 2. Map to Domain Entity
  event := &CommitEntity{}
  json.Unmarshal(payload, event)

  // 3. Publish to RabbitMQ Spine
  spine.Publish("engineering.signal", event)
}`}
                  </pre>
                </div>
              </div>
            </div>
          </div>
        </section>
      </main>

      {/* Footer */}
      <footer className="border-t border-[#334155]/30 py-20 bg-[#0F172A]">
        <div className="container px-4 md:px-6 mx-auto">
          <div className="flex flex-col md:flex-row justify-between items-center gap-10">
            <div className="flex flex-col items-center md:items-start gap-4">
                <div className="flex items-center gap-3">
                    <div className="h-8 w-8 bg-[#06B6D4] flex items-center justify-center rounded-lg text-white font-bold text-sm">N</div>
                    <span className="font-bold text-xl tracking-tighter">Nexus</span>
                </div>
                <p className="text-sm text-slate-500 max-w-xs text-center md:text-left font-medium">
                    The platform where engineering intuition meets data-driven intelligence.
                </p>
            </div>
            
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-12 sm:gap-24">
                <div className="space-y-4">
                    <h5 className="text-sm font-bold uppercase tracking-widest text-white">Product</h5>
                    <ul className="space-y-2 text-sm text-slate-500 font-medium">
                        <li><Link to="#" className="hover:text-[#06B6D4] transition-colors">Features</Link></li>
                        <li><Link to="#" className="hover:text-[#06B6D4] transition-colors">Integrations</Link></li>
                        <li><Link to="#" className="hover:text-[#06B6D4] transition-colors">Enterprise</Link></li>
                    </ul>
                </div>
                <div className="space-y-4">
                    <h5 className="text-sm font-bold uppercase tracking-widest text-white">Company</h5>
                    <ul className="space-y-2 text-sm text-slate-500 font-medium">
                        <li><Link to="#" className="hover:text-[#06B6D4] transition-colors">About</Link></li>
                        <li><Link to="#" className="hover:text-[#06B6D4] transition-colors">Careers</Link></li>
                        <li><Link to="#" className="hover:text-[#06B6D4] transition-colors">Privacy</Link></li>
                    </ul>
                </div>
            </div>
          </div>
          
          <div className="mt-20 pt-8 border-t border-[#334155]/20 flex flex-col sm:flex-row justify-between items-center gap-4 text-[10px] font-bold uppercase tracking-widest text-slate-600">
            <p>© 2026 Nexus Platform Inc. All rights reserved.</p>
            <div className="flex gap-6">
                <Link to="#" className="hover:text-white transition-colors">Twitter</Link>
                <Link to="#" className="hover:text-white transition-colors">GitHub</Link>
                <Link to="#" className="hover:text-white transition-colors">Discord</Link>
            </div>
          </div>
        </div>
      </footer>
    </div>
  );
}
