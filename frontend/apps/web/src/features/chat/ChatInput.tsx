import { useState } from "react"
import { useMutation } from "@tanstack/react-query"
import api from "@/lib/api"
import { useAuthStore } from "@/store/useAuthStore"
import { useChatStore } from "@/store/useChatStore"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Send, Loader2 } from "lucide-react"

export function ChatInput() {
  const [content, setContent] = useState("")
  const { activeChannel, addMessage } = useChatStore()
  const { user } = useAuthStore()

  const mutation = useMutation({
    mutationFn: async (messageContent: string) => {
      if (!activeChannel || !user) return
      const response = await api.post(`/channels/${activeChannel.id}/messages`, {
        content: messageContent,
        userId: user.id,
      })
      return response.data
    },
    onSuccess: (data) => {
      setContent("")
      // Optimistically add message if not already there from SignalR
      if (activeChannel && data) {
        addMessage(activeChannel.id, data)
      }
    },
  })

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (!content.trim() || mutation.isPending) return
    mutation.mutate(content)
  }

  if (!activeChannel) return null

  return (
    <form onSubmit={handleSubmit} className="p-4 border-t bg-background flex gap-2">
      <Input
        placeholder={`Message #${activeChannel.name}`}
        value={content}
        onChange={(e) => setContent(e.target.value)}
        disabled={mutation.isPending}
        className="flex-1"
      />
      <Button type="submit" size="icon" disabled={!content.trim() || mutation.isPending}>
        {mutation.isPending ? (
          <Loader2 className="h-4 w-4 animate-spin" />
        ) : (
          <Send className="h-4 w-4" />
        )}
      </Button>
    </form>
  )
}
