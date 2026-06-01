import { useQuery } from "@tanstack/react-query"
import api from "@/lib/api"
import { useChatStore } from "@/store/useChatStore"
import { useEffect, useRef } from "react"
import { MessageItem } from "./MessageItem"

export function MessageList() {
  const { activeChannel, messages, setMessages } = useChatStore()
  const scrollRef = useRef<HTMLDivElement>(null)

  const { data: fetchedMessages } = useQuery({
    queryKey: ["messages", activeChannel?.id],
    queryFn: async () => {
      if (!activeChannel) return []
      const response = await api.get(`/channels/${activeChannel.id}/messages`)
      return response.data
    },
    enabled: !!activeChannel,
  })

  useEffect(() => {
    if (fetchedMessages && activeChannel) {
      setMessages(activeChannel.id, fetchedMessages)
    }
  }, [fetchedMessages, activeChannel, setMessages])

  // Auto-scroll to bottom when messages change
  useEffect(() => {
    if (scrollRef.current) {
        scrollRef.current.scrollTop = scrollRef.current.scrollHeight
    }
  }, [messages, activeChannel])

  if (!activeChannel) {
    return (
      <div className="flex-1 flex items-center justify-center text-muted-foreground">
        Select a channel to start chatting
      </div>
    )
  }

  const channelMessages = messages[activeChannel.id] || []

  return (
    <div ref={scrollRef} className="flex-1 overflow-y-auto p-4 space-y-4">
      {channelMessages.map((message) => (
        <MessageItem key={message.id} message={message} channelId={activeChannel.id} />
      ))}
      {channelMessages.length === 0 && (
        <p className="text-center text-xs text-muted-foreground py-8">
          No messages yet. Say hello!
        </p>
      )}
    </div>
  )
}
