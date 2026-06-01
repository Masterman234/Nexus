import { useChatStore } from "@/store/useChatStore";
import { cn } from "@/lib/utils";
import { 
  Hash, 
  Activity, 
  Settings, 
  LayoutDashboard,
  ChevronLeft,
  ChevronRight,
  Plus
} from "lucide-react";
import { useState } from "react";

export function ChannelList() {
  const { channels, activeChannel, setActiveChannel } = useChatStore();
  const [isCollapsed, setIsCollapsed] = useState(false);

  return (
    <aside 
      className={cn(
        "flex flex-col bg-[#0F172A] border-r border-[#334155] transition-all duration-300 relative z-20",
        isCollapsed ? "w-16" : "w-64"
      )}
    >
      {/* Sidebar Toggle */}
      <button 
        onClick={() => setIsCollapsed(!isCollapsed)}
        className="absolute -right-3 top-20 h-6 w-6 rounded-full border border-[#334155] bg-[#1E293B] flex items-center justify-center text-slate-400 hover:text-white transition-colors"
      >
        {isCollapsed ? <ChevronRight className="h-3 w-3" /> : <ChevronLeft className="h-3 w-3" />}
      </button>

      {/* Navigation Groups */}
      <div className="flex-1 flex flex-col p-3 space-y-8 overflow-y-auto overflow-x-hidden text-slate-400 selection:bg-[#06B6D4]/30">
        
        {/* Workspace Section */}
        <div>
          {!isCollapsed && (
            <div className="px-3 mb-2 flex items-center justify-between">
              <span className="text-[10px] font-bold uppercase tracking-widest text-slate-500">Channels</span>
              <button className="text-slate-500 hover:text-white transition-colors">
                <Plus className="h-3.5 w-3.5" />
              </button>
            </div>
          )}
          <div className="space-y-0.5">
            {channels.map((channel) => (
              <button
                key={channel.id}
                onClick={() => setActiveChannel(channel)}
                className={cn(
                  "w-full flex items-center gap-3 px-3 py-2 rounded-lg transition-all duration-200 group",
                  activeChannel?.id === channel.id 
                    ? "bg-[#06B6D4] text-white shadow-lg shadow-[#06B6D4]/20" 
                    : "hover:bg-[#1E293B] hover:text-slate-200"
                )}
              >
                <Hash className={cn(
                  "h-4 w-4 shrink-0",
                  activeChannel?.id === channel.id ? "text-white" : "text-slate-500 group-hover:text-slate-300"
                )} />
                {!isCollapsed && (
                  <span className="text-sm font-semibold truncate">{channel.name}</span>
                )}
              </button>
            ))}
          </div>
        </div>

        {/* Global Navigation */}
        <div className="pt-4 border-t border-[#334155]/50">
          {!isCollapsed && (
            <span className="px-3 mb-2 block text-[10px] font-bold uppercase tracking-widest text-slate-500">Insights</span>
          )}
          <div className="space-y-0.5">
            <button className="w-full flex items-center gap-3 px-3 py-2 rounded-lg hover:bg-[#1E293B] hover:text-slate-200 transition-all duration-200 group">
              <Activity className="h-4 w-4 shrink-0 text-slate-500 group-hover:text-slate-300" />
              {!isCollapsed && <span className="text-sm font-semibold">Activity</span>}
            </button>
            <button className="w-full flex items-center gap-3 px-3 py-2 rounded-lg hover:bg-[#1E293B] hover:text-slate-200 transition-all duration-200 group">
              <LayoutDashboard className="h-4 w-4 shrink-0 text-slate-500 group-hover:text-slate-300" />
              {!isCollapsed && <span className="text-sm font-semibold">Intelligence</span>}
            </button>
          </div>
        </div>
      </div>

      {/* Sidebar Footer */}
      <div className="p-3 border-t border-[#334155]">
        <button className="w-full flex items-center gap-3 px-3 py-2 rounded-lg hover:bg-[#1E293B] hover:text-slate-200 transition-all duration-200 group text-slate-400">
          <Settings className="h-4 w-4 shrink-0 text-slate-500 group-hover:text-slate-300" />
          {!isCollapsed && <span className="text-sm font-semibold">Settings</span>}
        </button>
      </div>
    </aside>
  );
}
