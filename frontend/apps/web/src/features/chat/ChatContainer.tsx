import { useEffect } from "react"
import { useQuery } from "@tanstack/react-query"
import api from "@/lib/api"
import { useChatStore } from "@/store/useChatStore"
import { useAuthStore } from "@/store/useAuthStore"
import { ChannelList } from "./ChannelList"
import { MessageList } from "./MessageList"
import { ChatInput } from "./ChatInput"
import { Button } from "@/components/ui/button"
import { 
  LogOut, 
  Settings, 
  User, 
  MessageSquare, 
  Activity, 
  Bell,
  Search,
  Cpu,
  Layout
} from "lucide-react"
import { useSignalR } from "@/store/useSignalR"
import { EngineeringTimeline } from "@/features/engineering/EngineeringTimeline"
import { TicketKanban } from "@/features/tickets/TicketKanban"
import { useState } from "react"
import { cn } from "@/lib/utils"

export function ChatContainer() {
  const { setChannels, setActiveChannel, activeChannel } = useChatStore()
  const { user, logout } = useAuthStore()
  const [view, setView] = useState<"chat" | "engineering" | "tickets">("chat")

  // Initialize SignalR connection
  useSignalR()

  const { data: fetchedChannels, isLoading } = useQuery({
    queryKey: ["channels"],
    queryFn: async () => {
      const response = await api.get("/channels")
      return response.data
    }
  })

  useEffect(() => {
    if (fetchedChannels) {
      setChannels(fetchedChannels)
      // Auto-select first channel if none is active
      if (!activeChannel && fetchedChannels.length > 0) {
        setActiveChannel(fetchedChannels[0])
      }
    }
  }, [fetchedChannels, setChannels, setActiveChannel, activeChannel])

  if (isLoading) {
    return (
      <div className="h-screen w-full flex items-center justify-center bg-[#0F172A]">
        <div className="flex flex-col items-center gap-6">
          <div className="h-12 w-12 bg-[#06B6D4] rounded-xl animate-pulse flex items-center justify-center shadow-lg shadow-[#06B6D4]/20">
            <span className="text-white font-bold text-xl">N</span>
          </div>
          <p className="text-slate-400 text-sm font-bold uppercase tracking-[0.2em] animate-pulse">Initializing Nexus</p>
        </div>
      </div>
    )
  }

  return (
    <div className="flex h-screen w-full bg-[#0F172A] overflow-hidden flex-col selection:bg-[#06B6D4]/30">
      {/* App Header */}
      <header className="h-14 border-b border-[#334155]/50 flex items-center justify-between px-4 bg-[#0F172A] z-30 shrink-0">
        <div className="flex items-center gap-6">
          <div className="flex items-center gap-2 group cursor-pointer">
            <div className="h-7 w-7 bg-[#06B6D4] flex items-center justify-center rounded-lg shadow-lg shadow-[#06B6D4]/10">
              <span className="text-white font-bold text-xs">N</span>
            </div>
            <span className="font-bold tracking-tight text-md text-white">Nexus</span>
          </div>

          <div className="h-4 w-px bg-[#334155]/50 mx-1" />
          
          <nav className="flex items-center gap-1">
            <Button 
              variant="ghost" 
              size="sm" 
              className={cn(
                "h-8 gap-2 px-3 text-xs font-bold uppercase tracking-wider transition-all",
                view === "chat" ? "text-[#06B6D4] bg-[#06B6D4]/10" : "text-slate-400 hover:text-white"
              )}
              onClick={() => setView("chat")}
            >
              <MessageSquare className="h-3.5 w-3.5" />
              Collaboration
            </Button>
            <Button 
              variant="ghost" 
              size="sm" 
              className={cn(
                "h-8 gap-2 px-3 text-xs font-bold uppercase tracking-wider transition-all",
                view === "engineering" ? "text-[#06B6D4] bg-[#06B6D4]/10" : "text-slate-400 hover:text-white"
              )}
              onClick={() => setView("engineering")}
            >
              <Activity className="h-3.5 w-3.5" />
              Intelligence
            </Button>
            <Button 
              variant="ghost" 
              size="sm" 
              className={cn(
                "h-8 gap-2 px-3 text-xs font-bold uppercase tracking-wider transition-all",
                view === "tickets" ? "text-[#06B6D4] bg-[#06B6D4]/10" : "text-slate-400 hover:text-white"
              )}
              onClick={() => setView("tickets")}
            >
              <Layout className="h-3.5 w-3.5" />
              Tickets
            </Button>
          </nav>
        </div>
        
        <div className="flex items-center gap-3">
          <div className="flex items-center bg-[#1E293B] rounded-lg px-2 py-1 border border-[#334155] mr-2">
            <Search className="h-3.5 w-3.5 text-slate-500 mr-2" />
            <span className="text-[10px] font-bold text-slate-500 uppercase tracking-widest mr-4">Search...</span>
            <kbd className="hidden sm:inline-flex h-4 items-center gap-1 rounded bg-slate-800 px-1.5 font-mono text-[8px] font-medium text-slate-400">
              <span className="text-[10px]">⌘</span>K
            </kbd>
          </div>

          <Button variant="ghost" size="icon" className="h-8 w-8 text-slate-400 hover:text-[#06B6D4] hover:bg-[#06B6D4]/10">
            <Bell className="h-4 w-4" />
          </Button>

          <div className="h-6 w-px bg-[#334155]/50 mx-1" />

          <div className="flex items-center gap-2 pl-2">
            <div className="flex flex-col items-end hidden sm:flex">
                <span className="text-xs font-bold text-white">{user?.username}</span>
                <span className="text-[9px] font-bold text-[#06B6D4] uppercase tracking-tighter">Pro Plan</span>
            </div>
            <div className="h-8 w-8 rounded-lg bg-slate-800 border border-[#334155] flex items-center justify-center text-[10px] font-bold text-white">
                {user?.username?.substring(0, 2).toUpperCase()}
            </div>
            <Button variant="ghost" size="icon" onClick={logout} className="h-8 w-8 text-slate-500 hover:text-red-400 hover:bg-red-400/10">
                <LogOut className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </header>

      <div className="flex flex-1 overflow-hidden relative text-slate-400 min-h-0">
        {view === "chat" && <ChannelList />}
        
        <main className="flex-1 flex flex-col relative bg-[#0F172A] min-h-0">
          {/* Subtle Background Glow for Main Content */}
          <div className="absolute top-0 right-0 w-[400px] h-[400px] bg-[#06B6D4]/5 rounded-full blur-[120px] pointer-events-none z-0" />
          
          <div className="flex flex-1 flex-col relative z-10 min-h-0">
            {view === "engineering" ? (
              <EngineeringTimeline />
            ) : view === "tickets" ? (
              <TicketKanban />
            ) : activeChannel ? (
              <>
                <div className="h-12 border-b border-[#334155]/30 flex items-center px-6 justify-between bg-[#0F172A]/50 backdrop-blur-sm shrink-0">
                  <div className="flex items-center gap-3">
                    <div className="h-5 w-5 rounded bg-[#06B6D4]/10 flex items-center justify-center">
                        <Hash className="h-3.5 w-3.5 text-[#06B6D4]" />
                    </div>
                    <span className="font-bold text-sm tracking-tight text-white">{activeChannel.name}</span>
                    <div className="h-3 w-px bg-[#334155] mx-1" />
                    <span className="text-xs font-medium text-slate-500 truncate max-w-[300px]">
                      {activeChannel.description}
                    </span>
                  </div>
                  <div className="flex items-center gap-2">
                    <Button variant="ghost" size="sm" className="h-7 text-[10px] font-bold uppercase tracking-widest text-slate-400 hover:text-white">
                        <User className="h-3 w-3 mr-2" />
                        12 Members
                    </Button>
                    <div className="h-4 w-px bg-[#334155] mx-2" />
                    <Button variant="ghost" size="icon" className="h-7 w-7 text-slate-500">
                        <Settings className="h-3.5 w-3.5" />
                    </Button>
                  </div>
                </div>
                <div className="flex-1 overflow-hidden flex flex-col min-h-0">
                    <MessageList />
                    <ChatInput />
                </div>
              </>
            ) : (
              <div className="flex-1 flex flex-col items-center justify-center text-center p-8 space-y-6">
                <div className="p-6 bg-[#1E293B] rounded-[2rem] border border-[#334155] shadow-2xl relative overflow-hidden group">
                  <div className="absolute inset-0 bg-[#06B6D4]/5 opacity-0 group-hover:opacity-100 transition-opacity" />
                  <Cpu className="h-12 w-12 text-[#06B6D4] relative z-10" />
                </div>
                <div className="space-y-2 max-w-sm">
                  <h3 className="text-xl font-bold text-white tracking-tight">Ready for Intelligence?</h3>
                  <p className="text-sm text-slate-500 font-medium leading-relaxed">
                    Select a collaboration channel or switch to the intelligence view to see real-time engineering signals.
                  </p>
                </div>
                <Button 
                    onClick={() => setView("engineering")}
                    className="bg-[#06B6D4] hover:bg-[#0891B2] text-white font-bold rounded-xl"
                >
                    View Engineering Timeline
                </Button>
              </div>
            )}
          </div>
        </main>
      </div>
    </div>
  )
}

function Hash({ className }: { className?: string }) {
    return (
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" className={className}>
            <line x1="4" y1="9" x2="20" y2="9"></line>
            <line x1="4" y1="15" x2="20" y2="15"></line>
            <line x1="10" y1="3" x2="8" y2="21"></line>
            <line x1="16" y1="3" x2="14" y2="21"></line>
        </svg>
    )
}
