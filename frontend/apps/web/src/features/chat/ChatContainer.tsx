import { useEffect } from "react"
import { useQuery } from "@tanstack/react-query"
import api from "@/lib/api"
import { useChatStore } from "@/store/useChatStore"
import { useAuthStore } from "@/store/useAuthStore"
import { ChannelList } from "./ChannelList"
import { MessageList } from "./MessageList"
import { ChatInput } from "./ChatInput"
import { Button } from "@/components/ui/button"
import { LogOut, LayoutDashboard, Settings, User, MessageSquare, Activity } from "lucide-react"
import { WorkspaceSelector } from "./WorkspaceSelector"
import { useSignalR } from "@/store/useSignalR"
import { EngineeringTimeline } from "@/features/engineering/EngineeringTimeline"
import { useState } from "react"

export function ChatContainer() {
  const { setChannels, setActiveChannel, activeChannel, channels } = useChatStore()
  const { user, logout } = useAuthStore()
  const [view, setView] = useState<"chat" | "engineering">("chat")

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
      <div className="h-screen w-full flex items-center justify-center bg-background">
        <div className="animate-pulse flex flex-col items-center gap-4">
          <div className="h-12 w-12 bg-primary rounded-full"></div>
          <p className="text-sm font-medium">Initializing Nexus...</p>
        </div>
      </div>
    )
  }

  return (
    <div className="flex h-screen w-full bg-background overflow-hidden flex-col">
      {/* App Header */}
      <header className="h-14 border-b flex items-center justify-between px-4 bg-card z-10">
        <div className="flex items-center gap-4">
          <div className="h-8 w-8 bg-primary flex items-center justify-center rounded">
            <span className="text-primary-foreground font-bold">N</span>
          </div>
          <h1 className="font-bold tracking-tight text-lg">Nexus</h1>
          <nav className="flex items-center ml-4 gap-1 text-sm font-medium">
            <WorkspaceSelector />
            <div className="h-4 w-[1px] bg-border mx-2" />
            <Button 
              variant={view === "chat" ? "secondary" : "ghost"} 
              size="sm" 
              className="gap-2"
              onClick={() => setView("chat")}
            >
              <MessageSquare className="h-4 w-4" />
              Chat
            </Button>
            <Button 
              variant={view === "engineering" ? "secondary" : "ghost"} 
              size="sm" 
              className="gap-2"
              onClick={() => setView("engineering")}
            >
              <Activity className="h-4 w-4" />
              Engineering
            </Button>
          </nav>
        </div>
        
        <div className="flex items-center gap-2">
          <div className="flex items-center gap-2 px-3 py-1 bg-muted rounded-full text-xs font-medium mr-2">
            <User className="h-3 w-3" />
            {user?.username}
          </div>
          <Button variant="ghost" size="icon" className="h-8 w-8 text-muted-foreground hover:text-foreground">
            <Settings className="h-4 w-4" />
          </Button>
          <Button variant="ghost" size="icon" onClick={logout} className="h-8 w-8 text-muted-foreground hover:text-destructive">
            <LogOut className="h-4 w-4" />
          </Button>
        </div>
      </header>

      <div className="flex flex-1 overflow-hidden">
        {view === "chat" && <ChannelList />}
        <main className="flex-1 flex flex-col relative bg-background">
          {view === "engineering" ? (
            <EngineeringTimeline />
          ) : activeChannel ? (
            <>
              <div className="h-12 border-b flex items-center px-4 justify-between bg-card/50">
                <div className="flex items-center gap-2 font-semibold">
                  <span className="text-muted-foreground text-xl">#</span>
                  {activeChannel.name}
                  <span className="text-xs font-normal text-muted-foreground ml-2">
                    {activeChannel.description}
                  </span>
                </div>
              </div>
              <MessageList />
              <ChatInput />
            </>
          ) : (
            <div className="flex-1 flex flex-col items-center justify-center text-center p-8 space-y-4">
              <div className="p-4 bg-muted rounded-full">
                <LayoutDashboard className="h-8 w-8 text-muted-foreground" />
              </div>
              <div>
                <h3 className="text-lg font-semibold">Welcome to Nexus</h3>
                <p className="text-sm text-muted-foreground max-w-xs">
                  Select a channel from the sidebar to start collaborating with your team.
                </p>
              </div>
            </div>
          )}
        </main>
      </div>
    </div>
  )
}
