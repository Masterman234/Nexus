import { useChatStore } from "@/store/useChatStore"
import { useSignalR } from "@/store/useSignalR"
import { cn } from "@/lib/utils"

export function ChannelList() {
  const { channels, activeChannel, setActiveChannel } = useChatStore()
  const { joinChannel, leaveChannel } = useSignalR()

  const handleChannelClick = async (channel: any) => {
    if (activeChannel) {
      await leaveChannel(activeChannel.id)
    }
    setActiveChannel(channel)
    await joinChannel(channel.id)
  }

  return (
    <div className="w-64 border-r bg-muted/50 flex flex-col">
      <div className="p-4 border-b">
        <h2 className="font-semibold">Channels</h2>
      </div>
      <div className="flex-1 overflow-y-auto p-2 space-y-1">
        {channels.map((channel) => (
          <button
            key={channel.id}
            onClick={() => handleChannelClick(channel)}
            className={cn(
              "w-full text-left px-3 py-2 rounded-md text-sm transition-colors",
              activeChannel?.id === channel.id
                ? "bg-primary text-primary-foreground"
                : "hover:bg-muted"
            )}
          >
            # {channel.name}
          </button>
        ))}
        {channels.length === 0 && (
          <p className="text-xs text-muted-foreground p-2">No channels found.</p>
        )}
      </div>
    </div>
  )
}
