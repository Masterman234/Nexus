import { useState } from "react"
import { useMutation } from "@tanstack/react-query"
import api from "@/lib/api"
import { useAuthStore } from "@/store/useAuthStore"
import { useChatStore, type Message } from "@/store/useChatStore"
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { MoreVertical, Pencil, Trash2, Check, X } from "lucide-react"

interface MessageItemProps {
  message: Message
  channelId: string
}

export function MessageItem({ message, channelId }: MessageItemProps) {
  const { user } = useAuthStore()
  const { updateMessage, deleteMessage } = useChatStore()
  const [isEditing, setIsEditing] = useState(false)
  const [editContent, setEditContent] = useState(message.content)
  const [showMenu, setShowMenu] = useState(false)

  const isMe = message.username === user?.username

  const editMutation = useMutation({
    mutationFn: async () => {
      console.log("Attempting to edit message:", message.id)
      const response = await api.patch(`/channels/messages/${message.id}`, {
        content: editContent,
        userId: user?.id,
      })
      return response.data
    },
    onSuccess: (data) => {
      console.log("Edit successful:", data)
      updateMessage(channelId, data)
      setIsEditing(false)
      setShowMenu(false)
    },
    onError: (err: any) => {
      console.error("Edit Failed:", err.response?.data || err.message)
      alert("Failed to edit message: " + (err.response?.data?.message || err.message))
    }
  })

  const deleteMutation = useMutation({
    mutationFn: async () => {
      console.log("Attempting to delete message:", message.id)
      await api.delete(`/channels/messages/${message.id}?userId=${user?.id}`)
    },
    onSuccess: () => {
      console.log("Delete successful")
      deleteMessage(channelId, message.id)
      setShowMenu(false)
    },
    onError: (err: any) => {
      console.error("Delete Failed:", err.response?.data || err.message)
      alert("Failed to delete message: " + (err.response?.data?.message || err.message))
    }
  })

  const handleEditSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (!editContent.trim() || editContent === message.content) {
      setIsEditing(false)
      return
    }
    editMutation.mutate()
  }

  return (
    <div
      className={cn(
        "group relative flex flex-col max-w-[80%] p-3 rounded-lg transition-all border border-transparent hover:border-muted-foreground/10",
        isMe
          ? "ml-auto bg-primary text-primary-foreground"
          : "bg-muted text-foreground"
      )}
    >
      <div className="flex items-center justify-between gap-4 mb-1">
        <div className="flex items-center gap-2">
          <span className="text-xs font-bold">{message.username}</span>
          <span className="text-[10px] opacity-70">
            {new Date(message.sentAt).toLocaleTimeString()}
          </span>
        </div>

        {isMe && !isEditing && (
          <div className="relative">
            <button 
              onClick={(e) => {
                e.stopPropagation()
                console.log("Menu button clicked")
                setShowMenu(!showMenu)
              }}
              className="p-1 hover:bg-black/10 rounded transition-colors"
            >
              <MoreVertical className="h-3 w-3" />
            </button>
            
            {showMenu && (
              <div className="absolute right-0 top-6 w-32 bg-card text-card-foreground border shadow-xl rounded-md p-1 z-[100] flex flex-col gap-1">
                <button
                  onClick={(e) => {
                    e.stopPropagation()
                    console.log("Edit clicked")
                    setIsEditing(true)
                    setShowMenu(false)
                  }}
                  className="w-full flex items-center gap-2 px-2 py-1.5 text-xs hover:bg-muted rounded text-left"
                >
                  <Pencil className="h-3 w-3" /> Edit
                </button>
                <button
                  onClick={(e) => {
                    e.stopPropagation()
                    console.log("Delete clicked")
                    deleteMutation.mutate()
                  }}
                  className="w-full flex items-center gap-2 px-2 py-1.5 text-xs text-destructive hover:bg-destructive/10 rounded text-left"
                >
                  <Trash2 className="h-3 w-3" /> Delete
                </button>
              </div>
            )}
          </div>
        )}
      </div>

      {isEditing ? (
        <form onSubmit={handleEditSubmit} className="flex flex-col gap-2">
          <Input
            value={editContent}
            onChange={(e) => setEditContent(e.target.value)}
            className="h-8 text-sm bg-background text-foreground border-primary-foreground/20"
            autoFocus
          />
          <div className="flex justify-end gap-1">
            <Button type="button" size="icon" variant="ghost" className="h-6 w-6 hover:bg-black/10" onClick={() => setIsEditing(false)}>
              <X className="h-3 w-3" />
            </Button>
            <Button size="icon" variant="ghost" className="h-6 w-6 text-green-500 hover:bg-green-500/10" type="submit" disabled={editMutation.isPending}>
              <Check className="h-3 w-3" />
            </Button>
          </div>
        </form>
      ) : (
        <p className="text-sm whitespace-pre-wrap break-words">{message.content}</p>
      )}
    </div>
  )
}
