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
  const isBot = message.username === "github-bot"

  const editMutation = useMutation({
    mutationFn: async () => {
      const response = await api.patch(`/channels/messages/${message.id}`, {
        content: editContent,
        userId: user?.id,
      })
      return response.data
    },
    onSuccess: (data) => {
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
      await api.delete(`/channels/messages/${message.id}?userId=${user?.id}`)
    },
    onSuccess: () => {
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
        "group relative flex flex-col max-w-[85%] p-4 rounded-[1.25rem] transition-all duration-300 shadow-sm",
        isMe
          ? "ml-auto bg-[#06B6D4] text-white shadow-[#06B6D4]/10"
          : isBot 
            ? "bg-[#1E293B]/60 border border-[#334155]/50 backdrop-blur-sm text-slate-200"
            : "bg-[#1E293B] text-slate-200"
      )}
    >
      <div className="flex items-center justify-between gap-4 mb-2">
        <div className="flex items-center gap-2">
          <div className={cn(
              "h-5 w-5 rounded-md flex items-center justify-center text-[10px] font-bold uppercase",
              isBot ? "bg-[#06B6D4] text-white" : "bg-slate-700 text-slate-300"
          )}>
            {message.username.substring(0, 1)}
          </div>
          <span className={cn(
              "text-xs font-bold tracking-tight",
              isBot ? "text-[#06B6D4]" : "text-slate-100"
          )}>{message.username}</span>
          <span className="text-[10px] font-medium text-slate-500">
            {new Date(message.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
          </span>
          {isBot && (
              <span className="text-[9px] px-1.5 py-0.5 bg-[#06B6D4]/10 text-[#06B6D4] border border-[#06B6D4]/20 rounded uppercase font-bold tracking-widest">System</span>
          )}
        </div>

        {isMe && !isEditing && (
          <div className="relative">
            <button 
              onClick={(e) => {
                e.stopPropagation()
                setShowMenu(!showMenu)
              }}
              className="p-1 hover:bg-black/10 rounded transition-colors"
            >
              <MoreVertical className="h-3 w-3 text-slate-400" />
            </button>
            
            {showMenu && (
              <div className="absolute right-0 top-6 w-32 bg-slate-800 text-white border border-[#334155] shadow-xl rounded-xl p-1 z-[100] flex flex-col gap-1">
                <button
                  onClick={(e) => {
                    e.stopPropagation()
                    setIsEditing(true)
                    setShowMenu(false)
                  }}
                  className="w-full flex items-center gap-2 px-3 py-2 text-xs hover:bg-slate-700 rounded-lg text-left transition-colors"
                >
                  <Pencil className="h-3 w-3" /> Edit
                </button>
                <button
                  onClick={(e) => {
                    e.stopPropagation()
                    deleteMutation.mutate()
                  }}
                  className="w-full flex items-center gap-2 px-3 py-2 text-xs text-red-400 hover:bg-red-400/10 rounded-lg text-left transition-colors"
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
            className="h-10 text-sm bg-slate-900 border-[#334155] focus:border-[#06B6D4]"
            autoFocus
          />
          <div className="flex justify-end gap-1">
            <Button type="button" size="icon" variant="ghost" className="h-7 w-7 text-slate-400" onClick={() => setIsEditing(false)}>
              <X className="h-4 w-4" />
            </Button>
            <Button size="icon" variant="ghost" className="h-7 w-7 text-[#10B981]" type="submit" disabled={editMutation.isPending}>
              <Check className="h-4 w-4" />
            </Button>
          </div>
        </form>
      ) : (
        <div className={cn(
            "text-[13px] leading-relaxed whitespace-pre-wrap break-words font-medium",
            isBot ? "text-slate-300" : "text-slate-200"
        )}>
            {/* Simple Markdown-lite support for bolding */}
            {message.content.split('**').map((part, i) => 
                i % 2 === 1 ? <strong key={i} className="text-white font-bold">{part}</strong> : part
            )}
        </div>
      )}
    </div>
  )
}
